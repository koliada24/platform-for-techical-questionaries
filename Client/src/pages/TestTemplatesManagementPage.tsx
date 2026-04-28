import { useCallback, useEffect, useState } from 'react';
import { Alert, Badge, Button, Card, Container, Spinner, Stack } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { testTemplatesApi } from '../api/testTemplates';
import type { TestTemplateSummaryDto } from '../types/testTemplates';
import { PencilIcon, PlayIcon, PlusIcon, TrashIcon } from '../components/icons';
import { ConfirmDeleteModal } from '../components/ConfirmDeleteModal';
import { PublishTestTemplateModal } from '../components/PublishTestTemplateModal';

export function TestTemplatesManagementPage() {
  const navigate = useNavigate();
  const [templates, setTemplates] = useState<TestTemplateSummaryDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<TestTemplateSummaryDto | null>(null);
  const [publishTarget, setPublishTarget] = useState<TestTemplateSummaryDto | null>(null);

  const reload = useCallback(async () => {
    setError(null);
    try {
      const data = await testTemplatesApi.list();
      setTemplates(data);
    } catch (e) {
      let msg = 'Failed to load tests.';
      if (axios.isAxiosError(e)) msg = e.response?.data?.error ?? msg;
      setError(msg);
      setTemplates([]);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h3 mb-0">Tests management</h1>
        <Button variant="primary" onClick={() => navigate('/test-templates/new')}>
          <PlusIcon /> Create test
        </Button>
      </div>

      {error && <Alert variant="danger">{error}</Alert>}

      {templates === null ? (
        <div className="text-muted d-flex align-items-center gap-2">
          <Spinner animation="border" size="sm" /> Loading…
        </div>
      ) : templates.length === 0 ? (
        <Card>
          <Card.Body className="text-muted">
            No tests yet. Click <strong>Create test</strong> to add your first one.
          </Card.Body>
        </Card>
      ) : (
        <Stack gap={3}>
          {templates.map((t) => (
            <Card key={t.id}>
              <Card.Body className="d-flex justify-content-between align-items-start gap-3">
                <div className="flex-grow-1">
                  <Card.Title className="mb-1">{t.name}</Card.Title>
                  {t.description && (
                    <Card.Text className="text-muted mb-2">{t.description}</Card.Text>
                  )}
                  <div className="small text-muted">
                    <Badge bg="secondary" className="me-2">
                      {t.questionCount} question{t.questionCount === 1 ? '' : 's'}
                    </Badge>
                    {t.timeLimitMinutes ? (
                      <Badge bg="info">{t.timeLimitMinutes} min</Badge>
                    ) : (
                      <Badge bg="light" text="dark">
                        Unlimited time
                      </Badge>
                    )}
                  </div>
                </div>
                <div className="d-flex gap-2">
                  <Button
                    variant="success"
                    size="sm"
                    onClick={() => setPublishTarget(t)}
                    title="Publish to Google Classroom"
                  >
                    <PlayIcon /> Start test
                  </Button>
                  <Button
                    variant="outline-secondary"
                    size="sm"
                    onClick={() => navigate(`/test-templates/${t.id}/edit`)}
                    title="Edit"
                  >
                    <PencilIcon />
                  </Button>
                  <Button
                    variant="outline-danger"
                    size="sm"
                    onClick={() => setDeleteTarget(t)}
                    title="Delete"
                  >
                    <TrashIcon />
                  </Button>
                </div>
              </Card.Body>
            </Card>
          ))}
        </Stack>
      )}

      <ConfirmDeleteModal
        show={!!deleteTarget}
        title="Delete test?"
        message={
          deleteTarget
            ? `Are you sure you want to delete "${deleteTarget.name}"? This cannot be undone.`
            : ''
        }
        onConfirm={async () => {
          if (!deleteTarget) return;
          await testTemplatesApi.remove(deleteTarget.id);
          await reload();
        }}
        onHide={() => setDeleteTarget(null)}
      />

      <PublishTestTemplateModal
        show={!!publishTarget}
        testTemplate={publishTarget}
        onHide={() => setPublishTarget(null)}
        onPublished={() => void reload()}
      />
    </Container>
  );
}
