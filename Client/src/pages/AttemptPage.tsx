import { useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Container,
  Form,
  Modal,
  Spinner,
  Stack,
} from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router-dom';
import axios from 'axios';
import { useAuth } from '../auth/AuthContext';
import { ThemeToggle } from '../components/ThemeToggle';
import {
  attemptsApi,
  type AttemptForStudentDto,
  type AttemptQuestionForStudentDto,
} from '../api/attempts';

function formatTimeLeft(ms: number): string {
  if (ms <= 0) return '00:00';
  const total = Math.floor(ms / 1000);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  const pad = (n: number) => String(n).padStart(2, '0');
  return h > 0 ? `${pad(h)}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
}

const idxStorageKey = (attemptId: string) => `attempt-progress:${attemptId}:idx`;

export function AttemptPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user, loading: authLoading, loginWithGoogle } = useAuth();

  const [attempt, setAttempt] = useState<AttemptForStudentDto | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loadStatus, setLoadStatus] = useState<number | null>(null);
  const [fetching, setFetching] = useState(false);

  const [currentIdx, setCurrentIdx] = useState(0);
  const [singlePicks, setSinglePicks] = useState<Record<string, number | null>>({});
  const [multiPicks, setMultiPicks] = useState<Record<string, number[]>>({});
  const [textPicks, setTextPicks] = useState<Record<string, string>>({});
  const [answeredIds, setAnsweredIds] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [clearing, setClearing] = useState(false);

  const [showFinish, setShowFinish] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isStudent = user?.role === 'Student';
  const returnUrl = id ? `/attempts/${id}` : '/';

  // Load attempt
  useEffect(() => {
    if (authLoading || !user || !isStudent || !id) return;
    let cancelled = false;
    setFetching(true);
    setLoadError(null);
    setLoadStatus(null);
    attemptsApi
      .get(id)
      .then((data) => {
        if (cancelled) return;
        setAttempt(data);

        // Hydrate previously-saved answers from the server.
        const single: Record<string, number | null> = {};
        const multi: Record<string, number[]> = {};
        const text: Record<string, string> = {};
        const answered = new Set<string>();
        for (const q of data.questions) {
          const s = q.savedAnswer;
          if (!s) continue;
          answered.add(q.id);
          switch (s.type) {
            case 'SingleAnswer':
              single[q.id] = s.selectedOptionOrder ?? null;
              break;
            case 'MultipleAnswers':
              multi[q.id] = s.selectedOptionOrders ?? [];
              break;
            case 'OpenAnswer':
            case 'Code':
            case 'Diagram':
              text[q.id] = s.text ?? '';
              break;
          }
        }
        setSinglePicks(single);
        setMultiPicks(multi);
        setTextPicks(text);
        setAnsweredIds(answered);

        const stored = window.localStorage.getItem(idxStorageKey(data.id));
        const parsed = stored == null ? NaN : Number(stored);
        if (Number.isFinite(parsed) && parsed >= 0 && parsed < data.questions.length) {
          setCurrentIdx(parsed);
        } else {
          setCurrentIdx(0);
        }
      })
      .catch((e) => {
        if (cancelled) return;
        if (axios.isAxiosError(e)) {
          setLoadStatus(e.response?.status ?? null);
          setLoadError(
            (e.response?.data as { error?: string } | undefined)?.error ?? 'Failed to load attempt.',
          );
        } else {
          setLoadError('Failed to load attempt.');
        }
      })
      .finally(() => {
        if (!cancelled) setFetching(false);
      });
    return () => {
      cancelled = true;
    };
  }, [authLoading, user, isStudent, id]);

  // Persist current question index
  useEffect(() => {
    if (!attempt) return;
    window.localStorage.setItem(idxStorageKey(attempt.id), String(currentIdx));
  }, [attempt, currentIdx]);

  // Timer
  const deadlineMs = useMemo(() => {
    if (!attempt || attempt.timeLimitMinutes == null) return null;
    return new Date(attempt.startedAt).getTime() + attempt.timeLimitMinutes * 60_000;
  }, [attempt]);

  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    if (!attempt) return;
    const t = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(t);
  }, [attempt]);

  const timeLeftMs = deadlineMs == null ? null : deadlineMs - now;

  const handleAnswer = async () => {
    if (!attempt) return;
    const q = attempt.questions[currentIdx];
    if (!q) return;
    setSaving(true);
    setSaveError(null);
    try {
      switch (q.type) {
        case 'SingleAnswer':
          await attemptsApi.saveSingleAnswer(attempt.id, q.id, singlePicks[q.id] ?? null);
          break;
        case 'MultipleAnswers':
          await attemptsApi.saveMultipleAnswers(attempt.id, q.id, multiPicks[q.id] ?? []);
          break;
        case 'OpenAnswer':
          await attemptsApi.saveTextAnswer(attempt.id, q.id, textPicks[q.id] ?? '');
          break;
        case 'Code':
          await attemptsApi.saveCodeAnswer(attempt.id, q.id, textPicks[q.id] ?? '');
          break;
        case 'Diagram':
          await attemptsApi.saveDiagramAnswer(attempt.id, q.id, textPicks[q.id] ?? '');
          break;
      }
      setAnsweredIds((prev) => {
        if (prev.has(q.id)) return prev;
        const next = new Set(prev);
        next.add(q.id);
        return next;
      });
      setCurrentIdx((i) => (i + 1) % attempt.questions.length);
    } catch (e) {
      let msg = 'Failed to save answer.';
      if (axios.isAxiosError(e)) {
        msg = (e.response?.data as { error?: string } | undefined)?.error ?? msg;
      }
      setSaveError(msg);
    } finally {
      setSaving(false);
    }
  };

  const handleClear = async () => {
    if (!attempt) return;
    const q = attempt.questions[currentIdx];
    if (!q) return;
    setClearing(true);
    setSaveError(null);
    try {
      await attemptsApi.clearAnswer(attempt.id, q.id);
      setSinglePicks((prev) => {
        if (!(q.id in prev)) return prev;
        const next = { ...prev };
        delete next[q.id];
        return next;
      });
      setMultiPicks((prev) => {
        if (!(q.id in prev)) return prev;
        const next = { ...prev };
        delete next[q.id];
        return next;
      });
      setTextPicks((prev) => {
        if (!(q.id in prev)) return prev;
        const next = { ...prev };
        delete next[q.id];
        return next;
      });
      setAnsweredIds((prev) => {
        if (!prev.has(q.id)) return prev;
        const next = new Set(prev);
        next.delete(q.id);
        return next;
      });
    } catch (e) {
      let msg = 'Failed to clear answer.';
      if (axios.isAxiosError(e)) {
        msg = (e.response?.data as { error?: string } | undefined)?.error ?? msg;
      }
      setSaveError(msg);
    } finally {
      setClearing(false);
    }
  };

  // Early-return states ---------------------------------------------
  if (!id) {
    return (
      <Container className="py-5">
        <Alert variant="danger">Missing attempt id.</Alert>
      </Container>
    );
  }

  if (authLoading) {
    return (
      <Container className="py-5 d-flex justify-content-center">
        <Spinner animation="border" />
      </Container>
    );
  }

  if (!user) {
    return (
      <Container className="py-5" style={{ maxWidth: 560 }}>
        <Card>
          <Card.Body>
            <Card.Title>Sign in to continue</Card.Title>
            <Button variant="primary" onClick={() => loginWithGoogle('Student', returnUrl)}>
              Sign in with Google
            </Button>
          </Card.Body>
        </Card>
      </Container>
    );
  }

  if (!isStudent) {
    return (
      <Container className="py-5" style={{ maxWidth: 560 }}>
        <Alert variant="warning">
          This page is for students. You are signed in as <strong>{user.role}</strong>.
        </Alert>
      </Container>
    );
  }

  if (fetching) {
    return (
      <Container className="py-5 d-flex justify-content-center">
        <Spinner animation="border" />
      </Container>
    );
  }

  if (loadError) {
    return (
      <Container className="py-5" style={{ maxWidth: 560 }}>
        <Alert variant="danger">
          {loadStatus === 404 ? 'Attempt not found.' : loadError}
        </Alert>
        <Button variant="outline-secondary" onClick={() => navigate('/')}>
          Back
        </Button>
      </Container>
    );
  }

  if (!attempt) return null;

  const question = attempt.questions[currentIdx];

  return (
    <Container className="py-4" style={{ maxWidth: 880 }}>
      {/* Header: name + timer */}
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h4 className="m-0">{attempt.name}</h4>
        <div className="d-flex align-items-center gap-3">
          {timeLeftMs != null && (
            <div className={`fs-5 fw-bold ${timeLeftMs <= 60_000 ? 'text-danger' : ''}`}>
              {formatTimeLeft(timeLeftMs)}
            </div>
          )}
          <ThemeToggle />
        </div>
      </div>

      {/* Question navigator */}
      <div className="d-flex flex-wrap gap-2 mb-4">
        {attempt.questions.map((q, i) => {
          const answered = answeredIds.has(q.id);
          const variant = i === currentIdx
            ? 'primary'
            : answered
              ? 'secondary'
              : 'outline-secondary';
          return (
            <Button
              key={q.id}
              variant={variant}
              size="sm"
              style={
                answered && i !== currentIdx
                  ? { width: 40, height: 40, backgroundColor: '#adb5bd', borderColor: '#adb5bd' }
                  : { width: 40, height: 40 }
              }
              onClick={() => setCurrentIdx(i)}
              title={`Question ${i + 1}`}
            >
              {i + 1}
            </Button>
          );
        })}
      </div>

      {/* Current question */}
      {question && (
        <Card className="mb-4">
          <Card.Body>
            <div className="text-muted small mb-1">
              Question {currentIdx + 1} of {attempt.questions.length}
            </div>
            <Card.Title as="h5" className="mb-3">
              {question.text}
            </Card.Title>

            <QuestionInput
              question={question}
              singlePicks={singlePicks}
              setSinglePicks={setSinglePicks}
              multiPicks={multiPicks}
              setMultiPicks={setMultiPicks}
              textPicks={textPicks}
              setTextPicks={setTextPicks}
            />

            {saveError && (
              <Alert variant="danger" className="mt-3 mb-0">
                {saveError}
              </Alert>
            )}

            <div className="d-flex justify-content-end gap-2 mt-4">
              <Button
                variant="outline-secondary"
                onClick={handleClear}
                disabled={saving || clearing || !answeredIds.has(question.id)}
              >
                {clearing ? 'Clearing…' : 'Clear answer'}
              </Button>
              <Button
                variant="primary"
                onClick={handleAnswer}
                disabled={saving || clearing || question.type === 'Code' || question.type === 'Diagram'}
              >
                {saving ? 'Saving…' : 'Next question'}
              </Button>
            </div>
          </Card.Body>
        </Card>
      )}

      <div className="d-flex justify-content-end">
        <Button variant="danger" onClick={() => setShowFinish(true)}>
          Finish
        </Button>
      </div>

      <Modal show={showFinish} onHide={() => (submitting ? null : setShowFinish(false))} centered>
        <Modal.Header closeButton={!submitting}>
          <Modal.Title>Finish test?</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Are you sure you want to finish and submit your answers? You won&apos;t be able to
          change them afterwards.
          {submitError && (
            <Alert variant="danger" className="mt-3 mb-0">
              {submitError}
            </Alert>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button
            variant="outline-secondary"
            onClick={() => setShowFinish(false)}
            disabled={submitting}
          >
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={submitting}
            onClick={async () => {
              if (!attempt) return;
              setSubmitting(true);
              setSubmitError(null);
              try {
                await attemptsApi.submit(attempt.id);
                window.localStorage.removeItem(idxStorageKey(attempt.id));
                navigate(`/attempts/${attempt.id}/submitted`, { replace: true });
              } catch (e) {
                let msg = 'Failed to submit attempt.';
                if (axios.isAxiosError(e)) {
                  msg = (e.response?.data as { error?: string } | undefined)?.error ?? msg;
                }
                setSubmitError(msg);
              } finally {
                setSubmitting(false);
              }
            }}
          >
            {submitting ? 'Submitting…' : 'Finish'}
          </Button>
        </Modal.Footer>
      </Modal>
    </Container>
  );
}

// ---- Helpers ------------------------------------------------------

interface QuestionInputProps {
  question: AttemptQuestionForStudentDto;
  singlePicks: Record<string, number | null>;
  setSinglePicks: React.Dispatch<React.SetStateAction<Record<string, number | null>>>;
  multiPicks: Record<string, number[]>;
  setMultiPicks: React.Dispatch<React.SetStateAction<Record<string, number[]>>>;
  textPicks: Record<string, string>;
  setTextPicks: React.Dispatch<React.SetStateAction<Record<string, string>>>;
}

function QuestionInput(props: QuestionInputProps) {
  const { question, singlePicks, setSinglePicks, multiPicks, setMultiPicks, textPicks, setTextPicks } =
    props;

  // We need a stable ref so the textarea onChange isn't stale-closed.
  const textRef = useRef(textPicks);
  textRef.current = textPicks;

  switch (question.type) {
    case 'SingleAnswer': {
      const selected = singlePicks[question.id] ?? null;
      return (
        <Stack gap={2}>
          {question.options.map((opt, idx) => (
            <Form.Check
              key={`${question.id}-${idx}`}
              type="radio"
              id={`q-${question.id}-opt-${idx}`}
              name={`q-${question.id}`}
              label={opt.text}
              checked={selected === idx}
              onChange={() =>
                setSinglePicks((prev) => ({ ...prev, [question.id]: idx }))
              }
            />
          ))}
        </Stack>
      );
    }
    case 'MultipleAnswers': {
      const selected = new Set(multiPicks[question.id] ?? []);
      return (
        <Stack gap={2}>
          {question.options.map((opt, idx) => (
            <Form.Check
              key={`${question.id}-${idx}`}
              type="checkbox"
              id={`q-${question.id}-opt-${idx}`}
              label={opt.text}
              checked={selected.has(idx)}
              onChange={(e) => {
                setMultiPicks((prev) => {
                  const next = new Set(prev[question.id] ?? []);
                  if (e.target.checked) next.add(idx);
                  else next.delete(idx);
                  return { ...prev, [question.id]: Array.from(next).sort((a, b) => a - b) };
                });
              }}
            />
          ))}
        </Stack>
      );
    }
    case 'OpenAnswer': {
      return (
        <Form.Control
          as="textarea"
          rows={5}
          value={textPicks[question.id] ?? ''}
          onChange={(e) =>
            setTextPicks((prev) => ({ ...prev, [question.id]: e.target.value }))
          }
          placeholder="Type your answer here…"
        />
      );
    }
    case 'Code':
      return (
        <Alert variant="secondary" className="mb-0">
          Code editor will be available soon.
        </Alert>
      );
    case 'Diagram':
      return (
        <Alert variant="secondary" className="mb-0">
          Diagram editor will be available soon.
        </Alert>
      );
    default:
      return null;
  }
}
