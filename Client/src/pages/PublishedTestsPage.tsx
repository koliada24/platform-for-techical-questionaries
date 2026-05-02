import { useEffect, useState } from 'react';
import { Alert, Badge, Card, Container, Spinner, Table } from 'react-bootstrap';
import axios from 'axios';
import { publishedTestsApi, type PublishedTestListItemDto } from '../api/publishedTests';

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString();
}

export function PublishedTestsPage() {
  const [items, setItems] = useState<PublishedTestListItemDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setError(null);
    publishedTestsApi
      .listForTeacher()
      .then((data) => {
        if (!cancelled) setItems(data);
      })
      .catch((e) => {
        if (cancelled) return;
        let msg = 'Failed to load published tests.';
        if (axios.isAxiosError(e)) msg = e.response?.data?.error ?? msg;
        setError(msg);
        setItems([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h3 mb-0">Published Tests</h1>
      </div>

      {error && <Alert variant="danger">{error}</Alert>}

      {items === null ? (
        <div className="text-muted d-flex align-items-center gap-2">
          <Spinner animation="border" size="sm" /> Loading…
        </div>
      ) : items.length === 0 ? (
        <Card>
          <Card.Body className="text-muted">
            You haven&apos;t published any tests yet.
          </Card.Body>
        </Card>
      ) : (
        <Card>
          <Table responsive hover className="mb-0 align-middle text-center">
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Opened</th>
                <th>Closes</th>
                <th>Questions</th>
                <th>Time limit</th>
                <th>Classroom groups</th>
              </tr>
            </thead>
            <tbody>
              {items.map((t) => {
                const closed = new Date(t.closesAt).getTime() <= Date.now();
                return (
                  <tr key={`${t.testTemplateId}-${t.closesAt}`}>
                    <td>
                      <div className="fw-semibold">{t.name}</div>
                      {t.description && (
                        <div className="text-muted small">{t.description}</div>
                      )}
                    </td>
                    <td>
                      <Badge bg={closed ? 'secondary' : 'success'}>
                        {closed ? 'Closed' : 'Open'}
                      </Badge>
                    </td>
                    <td className="small">{formatDateTime(t.openedAt)}</td>
                    <td className="small">{formatDateTime(t.closesAt)}</td>
                    <td>{t.questionCount}</td>
                    <td>{t.timeLimitMinutes ? `${t.timeLimitMinutes} min` : 'Unlimited'}</td>
                    <td>{t.courseCount}</td>
                  </tr>
                );
              })}
            </tbody>
          </Table>
        </Card>
      )}
    </Container>
  );
}

