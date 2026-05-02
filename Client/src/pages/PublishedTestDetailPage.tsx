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
  Table,
} from 'react-bootstrap';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import {
  publishedTestsApi,
  type PublishedTestDetailDto,
  type SubmittedAttemptSummaryDto,
} from '../api/publishedTests';

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString();
}

function formatDuration(totalSeconds: number): string {
  if (!Number.isFinite(totalSeconds) || totalSeconds < 0) return '—';
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = Math.floor(totalSeconds % 60);
  const pad = (n: number) => String(n).padStart(2, '0');
  return h > 0 ? `${pad(h)}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
}

function studentLabel(a: SubmittedAttemptSummaryDto): string {
  return a.studentName?.trim() || a.studentEmail || a.studentId;
}

export function PublishedTestDetailPage() {
  const { templateId } = useParams<{ templateId: string }>();
  const [searchParams] = useSearchParams();
  const closesAt = searchParams.get('closesAt');
  const navigate = useNavigate();

  const [detail, setDetail] = useState<PublishedTestDetailDto | null>(null);
  console.log('detail', detail);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (!templateId || !closesAt) {
      setError('Missing test reference.');
      return;
    }
    let cancelled = false;
    setError(null);
    publishedTestsApi
      .getTeacherDetail(templateId, closesAt)
      .then((data) => {
        if (!cancelled) setDetail(data);
      })
      .catch((e) => {
        if (cancelled) return;
        let msg = 'Failed to load published test.';
        if (axios.isAxiosError(e)) {
          if (e.response?.status === 404) msg = 'Published test not found.';
          else msg = e.response?.data?.error ?? msg;
        }
        setError(msg);
      });
    return () => {
      cancelled = true;
    };
  }, [templateId, closesAt]);

  const filtered = useMemo(() => {
    if (!detail) return [];
    const q = search.trim().toLowerCase();
    if (!q) return detail.submittedAttempts;
    return detail.submittedAttempts.filter((a) => {
      return (
        (a.studentName ?? '').toLowerCase().includes(q) ||
        (a.studentEmail ?? '').toLowerCase().includes(q)
      );
    });
  }, [detail, search]);

  return (
    <Container className="py-4">
      {error && <Alert variant="danger">{error}</Alert>}

      {!error && !detail ? (
        <div className="text-muted d-flex align-items-center gap-2">
          <Spinner animation="border" size="sm" /> Loading…
        </div>
      ) : detail ? (
        <>
          {/* Header with title + back link in the top-right corner */}
          <div className="d-flex justify-content-between align-items-start gap-3 mb-4">
            <div>
              <h1 className="h3 mb-1">{detail.name}</h1>
              {detail.description && (
                <div className="text-muted">{detail.description}</div>
              )}
            </div>
            <Button
              variant="link"
              className="p-0 flex-shrink-0"
              onClick={() => navigate('/published-tests')}
            >
              ← Back to published tests
            </Button>
          </div>

          {/* Section 1 — General details */}
          <section className="mb-4">
            <h5 className="mb-3">General details</h5>
            <Card>
              <Card.Body>
                <dl className="row mb-0">
                  <dt className="col-sm-4 col-md-3 text-muted fw-normal">Status</dt>
                  <dd className="col-sm-8 col-md-9 mb-2">
                    <Badge
                      bg={
                        new Date(detail.closesAt).getTime() <= Date.now()
                          ? 'secondary'
                          : 'success'
                      }
                    >
                      {new Date(detail.closesAt).getTime() <= Date.now() ? 'Closed' : 'Open'}
                    </Badge>
                  </dd>

                  <dt className="col-sm-4 col-md-3 text-muted fw-normal">Opened</dt>
                  <dd className="col-sm-8 col-md-9 mb-2">{formatDateTime(detail.openedAt)}</dd>

                  <dt className="col-sm-4 col-md-3 text-muted fw-normal">Closes</dt>
                  <dd className="col-sm-8 col-md-9 mb-2">{formatDateTime(detail.closesAt)}</dd>

                  <dt className="col-sm-4 col-md-3 text-muted fw-normal">Questions</dt>
                  <dd className="col-sm-8 col-md-9 mb-2">{detail.questionCount}</dd>

                  <dt className="col-sm-4 col-md-3 text-muted fw-normal">Time limit</dt>
                  <dd className="col-sm-8 col-md-9 mb-2">
                    {detail.timeLimitMinutes ? `${detail.timeLimitMinutes} min` : 'Unlimited'}
                  </dd>

                  <dt className="col-sm-4 col-md-3 text-muted fw-normal">Classroom groups</dt>
                  <dd className="col-sm-8 col-md-9 mb-0">{detail.courseCount}</dd>
                </dl>
              </Card.Body>
            </Card>
          </section>

          {/* Section 2 — Submitted attempts */}
          <section>
            <div className="d-flex justify-content-between align-items-center mb-3 gap-3 flex-wrap">
              <h5 className="mb-0">Submitted attempts</h5>

              <InputGroup style={{ maxWidth: 420 }}>
                <Form.Control
                  placeholder="Search by student name or email"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
                {search && (
                  <Button variant="outline-secondary" onClick={() => setSearch('')}>
                    Clear
                  </Button>
                )}
              </InputGroup>
            </div>

            {detail.submittedAttempts.length === 0 ? (
              <Card>
                <Card.Body className="text-muted">No students have submitted yet.</Card.Body>
              </Card>
            ) : filtered.length === 0 ? (
              <Card>
                <Card.Body className="text-muted">
                  No attempts match &ldquo;{search}&rdquo;.
                </Card.Body>
              </Card>
            ) : (
              <Card>
                <Table responsive hover className="mb-0 align-middle">
                  <thead>
                    <tr>
                      <th>Student</th>
                      <th>Started</th>
                      <th>Submitted</th>
                      <th>Duration</th>
                      <th>Mark</th>
                      <th>Evaluation</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filtered.map((a) => (
                      <tr key={a.id}>
                        <td>
                          <div className="d-flex align-items-center gap-2">
                            {a.studentPictureUrl ? (
                              <img
                                src={a.studentPictureUrl}
                                alt=""
                                referrerPolicy="no-referrer"
                                width={32}
                                height={32}
                                style={{ borderRadius: '50%', objectFit: 'cover' }}
                              />
                            ) : (
                              <div
                                className="bg-secondary text-white d-flex align-items-center justify-content-center"
                                style={{
                                  width: 32,
                                  height: 32,
                                  borderRadius: '50%',
                                  fontSize: 14,
                                }}
                              >
                                {studentLabel(a).charAt(0).toUpperCase()}
                              </div>
                            )}
                            <div>
                              <div className="fw-semibold">{studentLabel(a)}</div>
                              {a.studentEmail && a.studentName && (
                                <div className="text-muted small">{a.studentEmail}</div>
                              )}
                            </div>
                          </div>
                        </td>
                        <td className="small">{formatDateTime(a.startedAt)}</td>
                        <td className="small">{formatDateTime(a.submittedAt)}</td>
                        <td className="small">{formatDuration(a.durationSeconds)}</td>
                        <td className="small">{a.evaluatedMark}/{detail.maxMark}</td>
                        <td>
                          {a.isEvaluated ? (
                            <Badge bg="success">Evaluated</Badge>
                          ) : (
                            <Badge bg="warning" text="dark">
                              Not evaluated
                            </Badge>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
              </Card>
            )}
          </section>
        </>
      ) : null}
    </Container>
  );
}
