import { lazy, Suspense, useEffect, useMemo, useRef } from 'react';
import { Spinner } from 'react-bootstrap';
import { useTheme } from '../theme/ThemeContext';

// Excalidraw is heavy (~1 MB gzipped) and pulls in its own CSS — load it lazily
// so unrelated screens (login, home, template list) don't pay the cost.
const ExcalidrawLazy = lazy(async () => {
  await import('@excalidraw/excalidraw/index.css');
  const mod = await import('@excalidraw/excalidraw');
  return { default: mod.Excalidraw };
});

// We keep the Excalidraw types loose on purpose — its public types live under
// versioned subpath exports that change between minor releases. We only need a
// handful of shapes here, so we describe them structurally.
type ExcalidrawElement = { id: string; isDeleted?: boolean; [k: string]: unknown };
type AppState = { viewBackgroundColor?: string; [k: string]: unknown };
type BinaryFiles = Record<string, unknown>;
interface ExcalidrawApi {
  getAppState: () => AppState;
}

/**
 * Wire-format we persist as the diagram answer. The whole object is serialized
 * to JSON and stored in the existing `Text` column on DiagramAnswer{InProgress,Submitted}.
 *
 * - `scene` is what the student resumes from when re-opening the attempt and
 *   what the teacher's read-only viewer renders.
 * - `svg` is a baked snapshot suitable for cheap previews / future PDF export.
 */
export interface DiagramPayload {
  scene: {
    elements: ExcalidrawElement[];
    appState?: Partial<AppState>;
    files?: BinaryFiles;
  };
  svg?: string;
}

export function parseDiagramPayload(raw: string | null | undefined): DiagramPayload | null {
  if (!raw) return null;
  const trimmed = raw.trim();
  if (!trimmed) return null;
  try {
    const v = JSON.parse(trimmed) as Partial<DiagramPayload> | null;
    if (v && v.scene && Array.isArray(v.scene.elements)) {
      return {
        scene: {
          elements: v.scene.elements,
          appState: v.scene.appState,
          files: v.scene.files,
        },
        svg: v.svg,
      };
    }
  } catch {
    /* fall through */
  }
  return null;
}

interface DiagramEditorProps {
  /** JSON string in the shape of `DiagramPayload`. Empty string = blank canvas. */
  value: string;
  /** Called with the serialized `DiagramPayload` JSON. Debounced internally. */
  onChange?: (value: string) => void;
  readOnly?: boolean;
  height?: number | string;
}

/**
 * Excalidraw-backed diagram editor with the same `value/onChange/readOnly`
 * contract as `CodeEditor`. The parent owns the persisted JSON string.
 *
 * Notes:
 * - We intentionally do NOT push `value` back into the live editor when the
 *   parent state changes — that would create a feedback loop with our own
 *   `onChange`. We seed Excalidraw once via `initialData`. In read-only mode
 *   the parent recreates the component (via `key={…}`) when the answer
 *   actually changes, which is how the teacher viewer re-renders.
 * - SVG snapshots are produced inside the same debounce as `onChange` so we
 *   only pay for export when the scene has actually settled.
 */
export function DiagramEditor({ value, onChange, readOnly = false, height = 480 }: DiagramEditorProps) {
  const { theme } = useTheme();

  // Seed once on mount. Subsequent prop changes are ignored (see note above).
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const initial = useMemo(() => parseDiagramPayload(value), []);

  const apiRef = useRef<ExcalidrawApi | null>(null);

  const debounceRef = useRef<number | null>(null);
  // Track the last serialized payload so we don't fire onChange when Excalidraw
  // emits change events that don't actually mutate the scene (selection,
  // pointer movement, etc.).
  const lastSentRef = useRef<string | null>(null);

  useEffect(() => {
    return () => {
      if (debounceRef.current != null) window.clearTimeout(debounceRef.current);
    };
  }, []);

  const handleChange = (
    elements: readonly ExcalidrawElement[],
    appState: AppState,
    files: BinaryFiles,
  ) => {
    if (readOnly) return;
    if (debounceRef.current != null) window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(async () => {
      let svg: string | undefined;
      try {
        const mod = await import('@excalidraw/excalidraw');
        const node = await mod.exportToSvg({
          // exportToSvg ignores soft-deleted elements but accepts the live array.
          elements: elements as never,
          appState: { ...appState, exportBackground: false } as never,
          files: files as never,
          exportPadding: 8,
        });
        svg = node.outerHTML;
      } catch {
        /* SVG snapshot is best-effort. */
      }
      const payload: DiagramPayload = {
        scene: {
          elements: elements.filter((e) => !e.isDeleted).map((e) => ({ ...e })),
          appState: { viewBackgroundColor: appState.viewBackgroundColor },
          files,
        },
        svg,
      };
      const serialized = JSON.stringify(payload);
      if (serialized === lastSentRef.current) return;
      lastSentRef.current = serialized;
      onChange?.(serialized);
    }, 700);
  };

  return (
    <div
      className="border rounded overflow-hidden"
      style={{ height: typeof height === 'number' ? `${height}px` : height }}
    >
      <Suspense
        fallback={
          <div className="d-flex align-items-center justify-content-center h-100">
            <Spinner animation="border" />
          </div>
        }
      >
        <ExcalidrawLazy
          excalidrawAPI={(api: ExcalidrawApi) => {
            apiRef.current = api;
          }}
          initialData={
            initial
              ? {
                  elements: initial.scene.elements as never,
                  appState: initial.scene.appState as never,
                  files: initial.scene.files as never,
                  scrollToContent: true,
                }
              : undefined
          }
          theme={theme === 'dark' ? 'dark' : 'light'}
          viewModeEnabled={readOnly}
          onChange={handleChange as never}
          UIOptions={{
            canvasActions: {
              changeViewBackgroundColor: !readOnly,
              clearCanvas: !readOnly,
              export: false,
              loadScene: false,
              saveToActiveFile: false,
              toggleTheme: false,
            },
          }}
        />
      </Suspense>
    </div>
  );
}
