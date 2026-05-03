import { useEffect, useRef, useState } from 'react';
import Editor, { type OnMount } from '@monaco-editor/react';
import type { editor as MonacoEditor } from 'monaco-editor';
import type * as Monaco from 'monaco-editor';
import { useTheme } from '../theme/ThemeContext';
import { API_BASE_URL } from '../api/client';
import { attachCsharpLsp } from '../lsp/csharpLsp';

/**
 * Curated list of languages we expose in the test-template editor.
 * Each entry maps to a Monaco language id (https://github.com/microsoft/monaco-editor/tree/main/src/basic-languages).
 */
export const CODE_LANGUAGES: { value: string; label: string }[] = [
  { value: 'plaintext', label: 'Plain text' },
  { value: 'javascript', label: 'JavaScript' },
  { value: 'typescript', label: 'TypeScript' },
  { value: 'python', label: 'Python' },
  { value: 'java', label: 'Java' },
  { value: 'csharp', label: 'C#' },
  { value: 'cpp', label: 'C++' },
  { value: 'c', label: 'C' },
  { value: 'go', label: 'Go' },
  { value: 'rust', label: 'Rust' },
  { value: 'sql', label: 'SQL' },
  { value: 'html', label: 'HTML' },
  { value: 'css', label: 'CSS' },
  { value: 'json', label: 'JSON' },
  { value: 'xml', label: 'XML' },
  { value: 'markdown', label: 'Markdown' },
  { value: 'shell', label: 'Shell' },
];

const KNOWN_LANGUAGES = new Set(CODE_LANGUAGES.map((l) => l.value));

export function normalizeCodeLanguage(value: string | null | undefined): string {
  const v = (value ?? '').trim().toLowerCase();
  return KNOWN_LANGUAGES.has(v) ? v : 'plaintext';
}

interface CodeEditorProps {
  value: string;
  language: string | null | undefined;
  onChange?: (value: string) => void;
  readOnly?: boolean;
  height?: string | number;
  /**
   * When true and language === 'csharp', connects to the API's LSP WebSocket
   * to provide real Roslyn-backed IntelliSense (completion, hover,
   * signature help, diagnostics). Currently student-only.
   */
  enableLsp?: boolean;
}

export function CodeEditor({
  value,
  language,
  onChange,
  readOnly = false,
  height = 360,
  enableLsp = false,
}: CodeEditorProps) {
  const { theme } = useTheme();
  const editorRef = useRef<MonacoEditor.IStandaloneCodeEditor | null>(null);
  const [editor, setEditor] = useState<MonacoEditor.IStandaloneCodeEditor | null>(null);
  const [monacoNs, setMonacoNs] = useState<typeof Monaco | null>(null);

  const handleMount: OnMount = (ed, m) => {
    editorRef.current = ed;
    setEditor(ed);
    setMonacoNs(m);
  };

  // Keep editor.updateOptions in sync if readOnly flips after mount.
  useEffect(() => {
    editorRef.current?.updateOptions({ readOnly });
  }, [readOnly]);

  // Attach C# LSP when conditions are right. Re-runs (and tears down) when
  // language changes or LSP is disabled.
  useEffect(() => {
    if (!enableLsp || readOnly) return;
    if (!editor || !monacoNs) return;
    if (normalizeCodeLanguage(language) !== 'csharp') return;

    const wsUrl = API_BASE_URL.replace(/^http/, 'ws') + '/api/lsp/csharp';

    let cancelled = false;
    let dispose: (() => void) | null = null;

    attachCsharpLsp(monacoNs, editor, wsUrl)
      .then((res) => {
        if (cancelled) {
          res.dispose();
          return;
        }
        dispose = res.dispose;
      })
      .catch((err) => {
        // eslint-disable-next-line no-console
        console.warn('C# LSP unavailable:', err);
      });

    return () => {
      cancelled = true;
      dispose?.();
    };
  }, [enableLsp, readOnly, language, editor, monacoNs]);

  return (
    <div className="border rounded overflow-hidden">
      <Editor
        height={height}
        language={normalizeCodeLanguage(language)}
        value={value}
        theme={theme === 'dark' ? 'vs-dark' : 'light'}
        onMount={handleMount}
        onChange={(v) => onChange?.(v ?? '')}
        options={{
          readOnly,
          minimap: { enabled: false },
          fontSize: 14,
          scrollBeyondLastLine: false,
          automaticLayout: true,
          tabSize: 2,
          wordWrap: 'on',
          fixedOverflowWidgets: true,
        }}
      />
    </div>
  );
}
