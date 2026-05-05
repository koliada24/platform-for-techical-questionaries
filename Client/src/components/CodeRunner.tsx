import { useState } from 'react';
import { Alert, Button, Spinner } from 'react-bootstrap';
import axios from 'axios';
import { codeRunApi, type CodeRunResultDto } from '../api/attempts';
import { normalizeCodeLanguage } from './CodeEditor';

interface CodeRunnerProps {
  code: string;
  language: string | null | undefined;
}

/**
 * "Run" button + output panel for code answers. Currently only C# is supported
 * by the API; for other languages the button is hidden.
 */
export function CodeRunner({ code, language }: CodeRunnerProps) {
  const lang = normalizeCodeLanguage(language);
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<CodeRunResultDto | null>(null);
  const [requestError, setRequestError] = useState<string | null>(null);

  if (lang !== 'csharp') return null;

  const handleRun = async () => {
    setRunning(true);
    setRequestError(null);
    setResult(null);
    try {
      const r = await codeRunApi.run('csharp', code);
      setResult(r);
    } catch (e) {
      let msg = 'Failed to run code.';
      if (axios.isAxiosError(e)) {
        msg = (e.response?.data as { error?: string } | undefined)?.error ?? msg;
      }
      setRequestError(msg);
    } finally {
      setRunning(false);
    }
  };

  return (
    <div className="mt-3">
      <div className="d-flex justify-content-between align-items-center mb-2">
        <Button
          size="sm"
          variant="success"
          onClick={handleRun}
          disabled={running || !code.trim()}
        >
          {running ? (
            <>
              <Spinner animation="border" size="sm" className="me-2" />
              Running…
            </>
          ) : (
            'Run code'
          )}
        </Button>
        {result && (
          <span className="text-muted small">
            {result.timedOut ? 'Timed out' : result.success ? 'OK' : 'Failed'} ·{' '}
            {result.durationMs} ms
          </span>
        )}
      </div>

      {requestError && <Alert variant="danger" className="mb-0">{requestError}</Alert>}

      {result && (
        <div className="border rounded">
          <div className="px-2 py-1 small text-muted bg-body-tertiary border-bottom">
            Output
          </div>
          {result.stdout && (
            <pre
              className="m-0 p-2"
              style={{
                whiteSpace: 'pre-wrap',
                fontSize: 13,
                maxHeight: 240,
                overflow: 'auto',
              }}
            >
              {result.stdout}
            </pre>
          )}
          {result.error && (
            <pre
              className="m-0 p-2 border-top text-danger"
              style={{
                whiteSpace: 'pre-wrap',
                fontSize: 13,
                maxHeight: 240,
                overflow: 'auto',
              }}
            >
              {result.error}
            </pre>
          )}
          {!result.stdout && !result.error && (
            <div className="p-2 text-muted small fst-italic">
              {result.success ? '(no output)' : '(no output, no error)'}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
