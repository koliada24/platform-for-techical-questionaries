import { useEffect, useRef } from 'react';
import Editor, { type OnMount } from '@monaco-editor/react';
import type { editor } from 'monaco-editor';
import { useTheme } from '../theme/ThemeContext';

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
}

export function CodeEditor({
  value,
  language,
  onChange,
  readOnly = false,
  height = 360,
}: CodeEditorProps) {
  const { theme } = useTheme();
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);

  const handleMount: OnMount = (ed) => {
    editorRef.current = ed;
  };

  // Keep editor.updateOptions in sync if readOnly flips after mount.
  useEffect(() => {
    editorRef.current?.updateOptions({ readOnly });
  }, [readOnly]);

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
