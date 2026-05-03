// Hand-rolled LSP-over-WebSocket client + Monaco provider registration.
//
// Why not monaco-languageclient? Its peer-dep chain (@codingame/monaco-vscode-api,
// vscode-languageclient, etc.) is fragile and pulls in a lot. For our limited
// surface — completion / hover / signature help / diagnostics — a small custom
// bridge is simpler, has zero extra dependencies, and is easy to extend.
//
// Wire format on the WebSocket: one JSON-RPC message per WS text frame.
// The server (Api/Controllers/LspController.cs) translates that to/from the
// Content-Length-framed bytes that the language server speaks on stdio, and
// sends a single non-LSP "hello" frame first that contains the workspace and
// document URIs the LSP will recognize.

import type * as Monaco from 'monaco-editor';

// ---- JSON-RPC over WebSocket ---------------------------------------

type RpcId = number;

interface RpcRequest {
  jsonrpc: '2.0';
  id: RpcId;
  method: string;
  params?: unknown;
}

interface RpcNotification {
  jsonrpc: '2.0';
  method: string;
  params?: unknown;
}

interface RpcResponse {
  jsonrpc: '2.0';
  id: RpcId;
  result?: unknown;
  error?: { code: number; message: string };
}

interface LspHello {
  workspaceUri: string;
  documentUri: string;
}

type Pending = {
  resolve: (v: unknown) => void;
  reject: (e: unknown) => void;
};

export class LspConnection {
  private ws: WebSocket;
  private nextId = 1;
  private pending = new Map<RpcId, Pending>();
  private notifHandlers = new Map<string, (params: unknown) => void>();
  private helloPromise: Promise<LspHello>;
  private gotHello = false;
  private closed = false;

  constructor(url: string) {
    this.ws = new WebSocket(url);
    this.helloPromise = new Promise<LspHello>((resolveHello, rejectHello) => {
      this.ws.onmessage = (ev) => {
        const data = typeof ev.data === 'string' ? ev.data : '';
        if (!data) return;
        if (!this.gotHello) {
          try {
            const parsed = JSON.parse(data) as Partial<LspHello>;
            if (
              parsed &&
              typeof parsed.workspaceUri === 'string' &&
              typeof parsed.documentUri === 'string'
            ) {
              this.gotHello = true;
              resolveHello({
                workspaceUri: parsed.workspaceUri,
                documentUri: parsed.documentUri,
              });
              return;
            }
          } catch {
            /* fall through to LSP-message handling */
          }
        }
        this.handleMessage(data);
      };
      this.ws.onerror = () => rejectHello(new Error('LSP WebSocket error'));
      this.ws.onclose = () => {
        if (!this.gotHello) rejectHello(new Error('LSP closed before init'));
        this.closed = true;
        for (const p of this.pending.values()) {
          p.reject(new Error('LSP connection closed'));
        }
        this.pending.clear();
      };
    });
  }

  awaitHello(): Promise<LspHello> {
    return this.helloPromise;
  }

  request<T = unknown>(method: string, params?: unknown): Promise<T> {
    if (this.closed) return Promise.reject(new Error('LSP connection closed'));
    const id = this.nextId++;
    const req: RpcRequest = { jsonrpc: '2.0', id, method, params };
    return new Promise<T>((resolve, reject) => {
      this.pending.set(id, {
        resolve: resolve as (v: unknown) => void,
        reject,
      });
      try {
        this.ws.send(JSON.stringify(req));
      } catch (e) {
        this.pending.delete(id);
        reject(e);
      }
    });
  }

  notify(method: string, params?: unknown): void {
    if (this.closed) return;
    const n: RpcNotification = { jsonrpc: '2.0', method, params };
    try {
      this.ws.send(JSON.stringify(n));
    } catch {
      /* ignored */
    }
  }

  on(method: string, handler: (params: unknown) => void): void {
    this.notifHandlers.set(method, handler);
  }

  close(): void {
    if (this.closed) return;
    this.closed = true;
    try {
      this.ws.close();
    } catch {
      /* ignored */
    }
  }

  private handleMessage(data: string): void {
    let parsed: unknown;
    try {
      parsed = JSON.parse(data);
    } catch {
      return;
    }
    if (!parsed || typeof parsed !== 'object') return;

    const msg = parsed as Partial<RpcResponse> & Partial<RpcNotification>;
    if (typeof msg.method === 'string') {
      // Notification (we ignore server-initiated requests for now).
      const handler = this.notifHandlers.get(msg.method);
      handler?.(msg.params);
      return;
    }
    if (typeof msg.id === 'number') {
      const p = this.pending.get(msg.id);
      if (!p) return;
      this.pending.delete(msg.id);
      const r = msg as RpcResponse;
      if (r.error) p.reject(new Error(r.error.message));
      else p.resolve(r.result);
    }
  }
}

// ---- Monaco integration --------------------------------------------

interface LspPosition {
  line: number;
  character: number;
}
interface LspRange {
  start: LspPosition;
  end: LspPosition;
}
interface LspDiagnostic {
  range: LspRange;
  severity?: number;
  message: string;
  source?: string;
}
interface LspCompletionItem {
  label: string | { label: string };
  kind?: number;
  insertText?: string;
  insertTextFormat?: number; // 1 = plaintext, 2 = snippet
  documentation?: string | { kind: string; value: string };
  detail?: string;
  sortText?: string;
  filterText?: string;
}
interface LspMarkupContent {
  kind: string;
  value: string;
}
interface LspHover {
  contents: string | LspMarkupContent | Array<string | LspMarkupContent>;
}
interface LspSignatureInformation {
  label: string;
  documentation?: string | LspMarkupContent;
  parameters?: { label: string; documentation?: string | LspMarkupContent }[];
}
interface LspSignatureHelp {
  signatures: LspSignatureInformation[];
  activeSignature?: number;
  activeParameter?: number;
}

export interface AttachLspResult {
  /** Detach providers, send shutdown/exit, close the WebSocket. */
  dispose: () => void;
  /** The model that LSP is bound to (URI matches what the server expects). */
  model: Monaco.editor.ITextModel;
}

/**
 * Connect to the LSP WebSocket, swap the editor's model to one whose URI
 * matches the path the language server expects, perform the LSP handshake,
 * and register Monaco providers backed by the LSP. Returns a teardown fn.
 */
export async function attachCsharpLsp(
  monaco: typeof Monaco,
  editor: Monaco.editor.IStandaloneCodeEditor,
  wsUrl: string,
): Promise<AttachLspResult> {
  const conn = new LspConnection(wsUrl);
  const hello = await conn.awaitHello();

  // Swap the editor's model to one whose URI matches the LSP's expected path.
  const oldModel = editor.getModel();
  const initialText = oldModel?.getValue() ?? '';
  const uri = monaco.Uri.parse(hello.documentUri);
  let model = monaco.editor.getModel(uri);
  if (!model) {
    model = monaco.editor.createModel(initialText, 'csharp', uri);
  } else if (model.getValue() !== initialText) {
    model.setValue(initialText);
  }
  editor.setModel(model);

  await conn.request('initialize', {
    processId: null,
    clientInfo: { name: 'qapp-monaco' },
    rootUri: hello.workspaceUri,
    workspaceFolders: [{ uri: hello.workspaceUri, name: 'workspace' }],
    capabilities: {
      textDocument: {
        synchronization: { dynamicRegistration: false, didSave: false },
        completion: {
          completionItem: {
            snippetSupport: true,
            documentationFormat: ['markdown', 'plaintext'],
          },
        },
        hover: { contentFormat: ['markdown', 'plaintext'] },
        signatureHelp: {
          signatureInformation: { documentationFormat: ['markdown', 'plaintext'] },
        },
        publishDiagnostics: {},
      },
    },
  });
  conn.notify('initialized', {});

  let docVersion = 1;
  conn.notify('textDocument/didOpen', {
    textDocument: {
      uri: hello.documentUri,
      languageId: 'csharp',
      version: docVersion,
      text: model.getValue(),
    },
  });

  const changeListener = model.onDidChangeContent(() => {
    docVersion++;
    conn.notify('textDocument/didChange', {
      textDocument: { uri: hello.documentUri, version: docVersion },
      contentChanges: [{ text: model!.getValue() }],
    });
  });

  // Diagnostics → Monaco markers
  conn.on('textDocument/publishDiagnostics', (params: unknown) => {
    const p = params as { uri?: string; diagnostics?: LspDiagnostic[] } | null;
    if (!p || p.uri !== hello.documentUri) return;
    const markers: Monaco.editor.IMarkerData[] = (p.diagnostics ?? []).map((d) => ({
      severity: lspToMonacoSeverity(monaco, d.severity),
      message: d.message,
      startLineNumber: d.range.start.line + 1,
      startColumn: d.range.start.character + 1,
      endLineNumber: d.range.end.line + 1,
      endColumn: d.range.end.character + 1,
      source: d.source,
    }));
    monaco.editor.setModelMarkers(model!, 'csharp-lsp', markers);
  });

  // Completion
  const completionProvider = monaco.languages.registerCompletionItemProvider('csharp', {
    triggerCharacters: ['.', ' ', '(', ',', ':', '<', '@', '"'],
    provideCompletionItems: async (m, pos) => {
      if (m !== model) return null;
      let result: unknown;
      try {
        result = await conn.request('textDocument/completion', {
          textDocument: { uri: hello.documentUri },
          position: { line: pos.lineNumber - 1, character: pos.column - 1 },
        });
      } catch {
        return null;
      }

      const items: LspCompletionItem[] = Array.isArray(result)
        ? (result as LspCompletionItem[])
        : ((result as { items?: LspCompletionItem[] } | null)?.items ?? []);

      const word = m.getWordUntilPosition(pos);
      const range = new monaco.Range(
        pos.lineNumber,
        word.startColumn,
        pos.lineNumber,
        word.endColumn,
      );

      return {
        suggestions: items.map((it) => {
          const label = typeof it.label === 'string' ? it.label : it.label.label;
          const isSnippet = it.insertTextFormat === 2;
          return {
            label,
            kind: lspToMonacoKind(monaco, it.kind),
            insertText: it.insertText ?? label,
            insertTextRules: isSnippet
              ? monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet
              : undefined,
            documentation:
              typeof it.documentation === 'string'
                ? it.documentation
                : it.documentation?.value,
            detail: it.detail,
            sortText: it.sortText,
            filterText: it.filterText,
            range,
          } satisfies Monaco.languages.CompletionItem;
        }),
      };
    },
  });

  // Hover
  const hoverProvider = monaco.languages.registerHoverProvider('csharp', {
    provideHover: async (m, pos) => {
      if (m !== model) return null;
      let result: unknown;
      try {
        result = await conn.request('textDocument/hover', {
          textDocument: { uri: hello.documentUri },
          position: { line: pos.lineNumber - 1, character: pos.column - 1 },
        });
      } catch {
        return null;
      }
      const r = result as LspHover | null;
      if (!r?.contents) return null;
      const text = flattenHoverContents(r.contents);
      if (!text) return null;
      return { contents: [{ value: text }] };
    },
  });

  // Signature help
  const sigHelpProvider = monaco.languages.registerSignatureHelpProvider('csharp', {
    signatureHelpTriggerCharacters: ['(', ','],
    provideSignatureHelp: async (m, pos) => {
      if (m !== model) return null;
      let result: unknown;
      try {
        result = await conn.request('textDocument/signatureHelp', {
          textDocument: { uri: hello.documentUri },
          position: { line: pos.lineNumber - 1, character: pos.column - 1 },
        });
      } catch {
        return null;
      }
      const r = result as LspSignatureHelp | null;
      if (!r?.signatures?.length) return null;
      return {
        value: {
          signatures: r.signatures.map((s) => ({
            label: s.label,
            documentation:
              typeof s.documentation === 'string'
                ? s.documentation
                : s.documentation?.value,
            parameters: (s.parameters ?? []).map((par) => ({
              label: par.label,
              documentation:
                typeof par.documentation === 'string'
                  ? par.documentation
                  : par.documentation?.value,
            })),
          })),
          activeSignature: r.activeSignature ?? 0,
          activeParameter: r.activeParameter ?? 0,
        },
        dispose: () => {},
      };
    },
  });

  const dispose = () => {
    changeListener.dispose();
    completionProvider.dispose();
    hoverProvider.dispose();
    sigHelpProvider.dispose();
    try {
      monaco.editor.setModelMarkers(model!, 'csharp-lsp', []);
    } catch {
      /* model may be disposed */
    }
    try {
      conn.notify('shutdown', null);
      conn.notify('exit');
    } catch {
      /* ignored */
    }
    conn.close();
  };

  return { dispose, model };
}

function flattenHoverContents(contents: LspHover['contents']): string {
  if (typeof contents === 'string') return contents;
  if (Array.isArray(contents)) {
    return contents
      .map((c) => (typeof c === 'string' ? c : c.value))
      .filter(Boolean)
      .join('\n\n');
  }
  return contents.value;
}

function lspToMonacoSeverity(monaco: typeof Monaco, s?: number): Monaco.MarkerSeverity {
  switch (s) {
    case 1:
      return monaco.MarkerSeverity.Error;
    case 2:
      return monaco.MarkerSeverity.Warning;
    case 3:
      return monaco.MarkerSeverity.Info;
    default:
      return monaco.MarkerSeverity.Hint;
  }
}

function lspToMonacoKind(
  monaco: typeof Monaco,
  k?: number,
): Monaco.languages.CompletionItemKind {
  const M = monaco.languages.CompletionItemKind;
  switch (k) {
    case 1: return M.Text;
    case 2: return M.Method;
    case 3: return M.Function;
    case 4: return M.Constructor;
    case 5: return M.Field;
    case 6: return M.Variable;
    case 7: return M.Class;
    case 8: return M.Interface;
    case 9: return M.Module;
    case 10: return M.Property;
    case 11: return M.Unit;
    case 12: return M.Value;
    case 13: return M.Enum;
    case 14: return M.Keyword;
    case 15: return M.Snippet;
    case 16: return M.Color;
    case 17: return M.File;
    case 18: return M.Reference;
    case 19: return M.Folder;
    case 20: return M.EnumMember;
    case 21: return M.Constant;
    case 22: return M.Struct;
    case 23: return M.Event;
    case 24: return M.Operator;
    case 25: return M.TypeParameter;
    default:
      return M.Text;
  }
}
