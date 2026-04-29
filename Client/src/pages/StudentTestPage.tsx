import { useEffect, useState } from 'react';
import { Alert, Button, Card, Container, Spinner, Stack } from 'react-bootstrap';
import { useParams } from 'react-router-dom';
import axios from 'axios';
import { useAuth } from '../auth/AuthContext';
import { publishedTestsApi, type PublishedTestInfoDto } from '../api/publishedTests';

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString();
}

export function StudentTestPage() {
  const { id } = useParams<{ id: string }>();
  const { user, loading: authLoading, loginWithGoogle, logout } = useAuth();

  const [info, setInfo] = useState<PublishedTestInfoDto | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loadStatus, setLoadStatus] = useState<number | null>(null);
  const [fetching, setFetching] = useState(false);

  const returnUrl = id ? `/tests/${id}` : '/';
  const isStudent = user?.role === 'Student';

  useEffect(() => {
    if (authLoading || !user || !isStudent || !id) return;
    let cancelled = false;
    setFetching(true);
    setLoadError(null);
    setLoadStatus(null);
    publishedTestsApi
      .getInfo(id)
      .then((data) => {
        if (!cancelled) setInfo(data);
      })
      .catch((e) => {
        if (cancelled) return;
        if (axios.isAxiosError(e)) {
          setLoadStatus(e.response?.status ?? null);
          setLoadError(
            (e.response?.data as { error?: string } | undefined)?.error ?? 'Failed to load test.',
          );
        } else {
          setLoadError('Failed to load test.');
        }
      })
      .finally(() => {
        if (!cancelled) setFetching(false);
      });
    return () => {
      cancelled = true;
    };
  }, [authLoading, user, isStudent, id]);

  if (!id) {
    return (
      <Container className="py-5">
        <Alert variant="danger">Missing test id.</Alert>
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

  // Not signed in — prompt student login
  if (!user) {
    return (
      <Container className="py-5" style={{ maxWidth: 560 }}>
        <Card>
          <Card.Body>
            <Card.Title>Sign in to take this test</Card.Title>
            <Card.Text className="text-muted">
              You need to sign in with your student Google account to continue.
            </Card.Text>
            <Button variant="primary" onClick={() => loginWithGoogle('Student', returnUrl)}>
              Sign in with Google
            </Button>
          </Card.Body>
        </Card>
      </Container>
    );
  }

  // Wrong role
  if (!isStudent) {
    return (
      <Container className="py-5" style={{ maxWidth: 560 }}>
        <Alert variant="warning">
          This link is for students. You are signed in as <strong>{user.role}</strong>.
        </Alert>
        <Button variant="outline-secondary" onClick={() => logout()}>
          Sign out
        </Button>
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
          {loadStatus === 404 ? 'Test not found.' : loadError}
        </Alert>
      </Container>
    );
  }

  if (!info) return null;

  const closed = new Date(info.closesAt).getTime() < Date.now();

  return (
    <Container className="py-5" style={{ maxWidth: 640 }}>
      <div className="d-flex justify-content-end align-items-center gap-2 mb-3 small text-muted">
        <span>
          Signed in as{' '}
          <strong>{user.email}</strong>
        </span>
      </div>
      <Card>
        <Card.Body>
          <Card.Title as="h3">{info.name}</Card.Title>
          {info.description && (
            <Card.Text className="text-muted">{info.description}</Card.Text>
          )}

          <Stack gap={2} className="my-3">
            <div>
              <strong>Questions:</strong> {info.questionCount}
            </div>
            <div>
              <strong>Time limit:</strong>{' '}
              {info.timeLimitMinutes != null ? `${info.timeLimitMinutes} min` : 'No limit'}
            </div>
            <div>
              <strong>Closes at:</strong> {formatDateTime(info.closesAt)}
            </div>
          </Stack>

          {closed && (
            <Alert variant="warning" className="mb-3">
              This test is closed.
            </Alert>
          )}

          <Button variant="primary" disabled={closed}>
            Start
          </Button>
        </Card.Body>
      </Card>
    </Container>
  );
}
