import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Card,
  Container,
  Form,
  InputGroup,
  Spinner,
  Stack,
} from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router-dom';
import axios from 'axios';
import {
  publishedTestsApi,
  type AttemptDetailForTeacherDto,
  type AttemptQuestionForTeacherDto,
  type SetManualMarkInput,
} from '../api/publishedTests';
import { CodeEditor } from '../components/CodeEditor';

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString();
}

function formatDuration(totalSeconds: number): string {
  if (!Number.isFinite(totalSeconds) || totalSeconds < 0) return '—';
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = Math.floor(totalSeconds % 60);
  const pad = (n: number) => String(n).padStart(2, '0');
  return h > 0 ? `${pad(h)}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
}

function studentLabel(d: AttemptDetailForTeacherDto): string {
  return d.studentName?.trim() || d.studentEmail || d.studentId;
}

type DraftMarks = Record<string, string>; // publishedQuestionId -> input value

function questionTypeLabel(t: AttemptQuestionForTeacherDto['type']): string {
  switch (t) {
    case 'SingleAnswer': return 'Single answer';
    case 'MultipleAnswers': return 'Multiple answers';
    case 'OpenAnswer': return 'Open answer';
    case 'Code': return 'Code';
    case 'Diagram': return 'Diagram';
  }
}

export function StudentAttemptDetailPage() {
  const { attemptId } = useParams<{ attemptId: string }>();
  const navigate = useNavigate();

  const [detail, setDetail] = useState<AttemptDetailForTeacherDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [drafts, setDrafts] = useState<DraftMarks>({});
  const [savingMarks, setSavingMarks] = useState(false);
  const [sending, setSending] = useState(false);

  useEffect(() => {
    if (!attemptId) return;
    let cancelled = false;
    publishedTestsApi
      .getAttemptDetail(attemptId)
      .then((d) => {
        if (cancelled) return;
        setDetail(d);
        const initial: DraftMarks = {};
        for (const q of d.questions) {
          if (!q.isAutoEvaluated) {
            initial[q.publishedQuestionId] = q.mark != null ? String(q.mark) : '';
          }
        }
        setDrafts(initial);
      })
      .catch((e) => {
        if (cancelled) return;
        let msg = 'Failed to load attempt.';
        if (axios.isAxiosError(e)) {
          if (e.response?.status === 404) msg = 'Attempt not found.';
          else msg = e.response?.data?.error ?? msg;
        }
        setError(msg);
      });
    return () => {
      cancelled = true;
    };
  }, [attemptId]);

  const manualQuestions = useMemo(
    () => (detail ? detail.questions.filter((q) => !q.isAutoEvaluated) : []),
    [detail],
  );

  const draftTotal = useMemo(() => {
    if (!detail) return 0;
    return detail.questions.reduce((sum, q) => {
      if (q.isAutoEvaluated) return sum + (q.mark ?? 0);
      const raw = drafts[q.publishedQuestionId] ?? '';
      const n = raw.trim() === '' ? null : Number(raw);
      return sum + (n != null && Number.isFinite(n) ? n : 0);
    }, 0);
  }, [detail, drafts]);

  const allManualFilled = useMemo(() => {
    return manualQuestions.every((q) => {
      const raw = (drafts[q.publishedQuestionId] ?? '').trim();
      if (raw === '') return false;
      const n = Number(raw);
      return Number.isFinite(n) && n >= 0 && n <= q.maxMark;
    });
  }, [manualQuestions, drafts]);

  const handleSaveMarks = async () => {
    if (!detail) return;
    setError(null);
    setInfo(null);

    const marks: SetManualMarkInput[] = manualQuestions.map((q) => {
      const raw = (drafts[q.publishedQuestionId] ?? '').trim();
      const n = raw === '' ? null : Number(raw);
      return {
        publishedQuestionId: q.publishedQuestionId,
        mark: n != null && Number.isFinite(n) ? n : null,
      };
    });

    // Validate locally before sending.
    for (const q of manualQuestions) {
      const raw = (drafts[q.publishedQuestionId] ?? '').trim();
      if (raw === '') continue;
      const n = Number(raw);
      if (!Number.isFinite(n) || n < 0 || n > q.maxMark) {
        setError(`Mark for question ${q.order + 1} must be between 0 and ${q.maxMark}.`);
        return;
      }
    }

    setSavingMarks(true);
    try {
      const updated = await publishedTestsApi.setManualMarks(detail.attemptId, marks);
      setDetail(updated);
      setInfo('Marks saved.');
    } catch (e) {
      let msg = 'Failed to save marks.';
      if (axios.isAxiosError(e)) msg = e.response?.data?.error ?? msg;
      setError(msg);
    } finally {
      setSavingMarks(false);
    }
  };

  const handleSendMark = async () => {
    if (!detail) return;
    setError(null);
    setInfo(null);
    setSending(true);
    try {
      const result = await publishedTestsApi.sendMark(detail.attemptId);
      setInfo(`Mark ${result.mark}/${result.maxMark} sent to Classroom.`);
      // Refresh to reflect markSent state.
      const refreshed = await publishedTestsApi.getAttemptDetail(detail.attemptId);
      setDetail(refreshed);
    } catch (e) {
      let msg = 'Failed to send mark to Classroom.';
      if (axios.isAxiosError(e)) {
        const data = e.response?.data;
        msg = data?.detail ?? data?.error ?? msg;
      }
      setError(msg);
    } finally {
      setSending(false);
    }
  };

  if (error && !detail) {
    return (
      <Container className="py-4">
        <Alert variant="danger">{error}</Alert>
        <Button variant="link" onClick={() => navigate(-1)}>← Back</Button>
      </Container>
    );
  }

  if (!detail) {
    return (
      <Container className="py-4">
        <div className="text-muted d-flex align-items-center gap-2">
          <Spinner animation="border" size="sm" /> Loading…
        </div>
      </Container>
    );
  }

  const backTarget = `/published-tests/${detail.testTemplateId}?closesAt=${encodeURIComponent(detail.closesAt)}`;

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-start gap-3 mb-4">
        <div>
          <h1 className="h3 mb-1">{detail.testName}</h1>
          <div className="text-muted">Attempt by {studentLabel(detail)}</div>
        </div>
        <Button
          variant="link"
          className="p-0 flex-shrink-0"
          onClick={() => navigate(backTarget)}
        >
          ← Back to attempts
        </Button>
      </div>

      {error && <Alert variant="danger" onClose={() => setError(null)} dismissible>{error}</Alert>}
      {info && <Alert variant="success" onClose={() => setInfo(null)} dismissible>{info}</Alert>}

      <section className="mb-4">
        <h5 className="mb-3">Student & attempt</h5>
        <Card>
          <Card.Body>
            <div className="d-flex align-items-center gap-3 mb-3">
              {detail.studentPictureUrl ? (
                <img
                  src={detail.studentPictureUrl}
                  alt=""
                  referrerPolicy="no-referrer"
                  width={48}
                  height={48}
                  style={{ borderRadius: '50%', objectFit: 'cover' }}
                />
              ) : (
                <div
                  className="bg-secondary text-white d-flex align-items-center justify-content-center"
                  style={{ width: 48, height: 48, borderRadius: '50%', fontSize: 20 }}
                >
                  {studentLabel(detail).charAt(0).toUpperCase()}
                </div>
              )}
              <div>
                <div className="fw-semibold">{studentLabel(detail)}</div>
                {detail.studentEmail && detail.studentName && (
                  <div className="text-muted small">{detail.studentEmail}</div>
                )}
              </div>
            </div>
            <dl className="row mb-0">
              <dt className="col-sm-4 col-md-3 text-muted fw-normal">Started</dt>
              <dd className="col-sm-8 col-md-9 mb-2">{formatDateTime(detail.startedAt)}</dd>

              <dt className="col-sm-4 col-md-3 text-muted fw-normal">Submitted</dt>
              <dd className="col-sm-8 col-md-9 mb-2">{formatDateTime(detail.submittedAt)}</dd>

              <dt className="col-sm-4 col-md-3 text-muted fw-normal">Duration</dt>
              <dd className="col-sm-8 col-md-9 mb-2">{formatDuration(detail.durationSeconds)}</dd>

              <dt className="col-sm-4 col-md-3 text-muted fw-normal">Total mark</dt>
              <dd className="col-sm-8 col-md-9 mb-2">
                <strong>{draftTotal}</strong> / {detail.maxMark}
              </dd>

              <dt className="col-sm-4 col-md-3 text-muted fw-normal">Status</dt>
              <dd className="col-sm-8 col-md-9 mb-0">
                {detail.markSent ? (
                  <Badge bg="success">Sent to Classroom</Badge>
                ) : detail.isFullyEvaluated ? (
                  <Badge bg="info">Ready to send</Badge>
                ) : (
                  <Badge bg="warning" text="dark">Needs grading</Badge>
                )}
              </dd>
            </dl>
          </Card.Body>
        </Card>
      </section>

      <section className="mb-4">
        <h5 className="mb-3">Answers</h5>
        <Stack gap={3}>
          {detail.questions.map((q) => (
            <Card key={q.publishedQuestionId}>
              <Card.Body>
                <div className="d-flex justify-content-between align-items-start gap-2 mb-2">
                  <div>
                    <div className="text-muted small">
                      Question {q.order + 1} · {questionTypeLabel(q.type)} · max {q.maxMark}
                    </div>
                    <div className="fw-semibold">{q.text}</div>
                  </div>
                  <div className="text-end">
                    {q.isAutoEvaluated ? (
                      <Badge bg="secondary">
                        Auto: {q.mark ?? 0}/{q.maxMark}
                      </Badge>
                    ) : (
                      <Badge bg={q.mark != null ? 'info' : 'warning'} text={q.mark != null ? undefined : 'dark'}>
                        {q.mark != null ? `Graded: ${q.mark}/${q.maxMark}` : 'Needs grading'}
                      </Badge>
                    )}
                  </div>
                </div>

                <AnswerView question={q} />

                {!q.isAutoEvaluated && (
                  <div className="mt-3" style={{ maxWidth: 260 }}>
                    <Form.Label className="small text-muted mb-1">Mark</Form.Label>
                    <InputGroup>
                      <Form.Control
                        type="number"
                        min={0}
                        max={q.maxMark}
                        value={drafts[q.publishedQuestionId] ?? ''}
                        onChange={(e) =>
                          setDrafts((prev) => ({
                            ...prev,
                            [q.publishedQuestionId]: e.target.value,
                          }))
                        }
                        disabled={detail.markSent}
                      />
                      <InputGroup.Text>/ {q.maxMark}</InputGroup.Text>
                    </InputGroup>
                  </div>
                )}
              </Card.Body>
            </Card>
          ))}
        </Stack>
      </section>

      <div className="d-flex justify-content-end gap-2">
        {!detail.markSent && manualQuestions.length > 0 && (
          <Button
            variant="outline-primary"
            onClick={handleSaveMarks}
            disabled={savingMarks || sending}
          >
            {savingMarks ? 'Saving…' : 'Save marks'}
          </Button>
        )}
        <Button
          variant="primary"
          onClick={handleSendMark}
          disabled={
            sending
            || savingMarks
            || detail.markSent
            || (manualQuestions.length > 0 && !allManualFilled)
          }
          title={
            detail.markSent
              ? 'Already sent'
              : !allManualFilled
                ? 'Grade all open-ended answers first'
                : undefined
          }
        >
          {sending ? 'Sending…' : detail.markSent ? 'Mark sent' : 'Send mark'}
        </Button>
      </div>
    </Container>
  );
}

function AnswerView({ question }: { question: AttemptQuestionForTeacherDto }) {
  switch (question.type) {
    case 'SingleAnswer': {
      return (
        <ul className="list-unstyled mb-0">
          {question.options.map((o) => {
            const isSelected = question.selectedOptionOrder === o.order;
            return (
              <li
                key={o.order}
                className={`d-flex align-items-center gap-2 ${o.isCorrect ? 'text-success' : ''}`}
              >
                <span style={{ width: 18 }}>
                  {isSelected ? '●' : '○'}
                </span>
                <span>{o.text}</span>
                {o.isCorrect && <Badge bg="success" className="ms-2">correct</Badge>}
                {isSelected && !o.isCorrect && <Badge bg="danger" className="ms-2">picked</Badge>}
              </li>
            );
          })}
        </ul>
      );
    }
    case 'MultipleAnswers': {
      const selected = new Set(question.selectedOptionOrders ?? []);
      return (
        <ul className="list-unstyled mb-0">
          {question.options.map((o) => {
            const isSelected = selected.has(o.order);
            return (
              <li
                key={o.order}
                className={`d-flex align-items-center gap-2 ${o.isCorrect ? 'text-success' : ''}`}
              >
                <span style={{ width: 18 }}>{isSelected ? '☑' : '☐'}</span>
                <span>{o.text}</span>
                {o.isCorrect && <Badge bg="success" className="ms-2">correct</Badge>}
                {isSelected && !o.isCorrect && <Badge bg="danger" className="ms-2">picked</Badge>}
              </li>
            );
          })}
        </ul>
      );
    }
    case 'OpenAnswer':
    case 'Diagram': {
      const text = question.answerText ?? '';
      if (!text.trim()) {
        return <div className="text-muted fst-italic">No answer provided.</div>;
      }
      return (
        <pre
          className="mb-0 p-2 bg-body-tertiary border rounded"
          style={{ whiteSpace: 'pre-wrap' }}
        >
          {text}
        </pre>
      );
    }
    case 'Code': {
      const text = question.answerText ?? '';
      if (!text.trim()) {
        return <div className="text-muted fst-italic">No answer provided.</div>;
      }
      return (
        <CodeEditor
          value={text}
          language={question.codeLanguage}
          readOnly
          height={Math.min(480, Math.max(180, text.split('\n').length * 20 + 40))}
        />
      );
    }
  }
}
