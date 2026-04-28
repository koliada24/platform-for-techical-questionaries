import { useEffect, useState } from 'react';
import { Alert, Button, Form, Modal, Spinner, Stack } from 'react-bootstrap';
import axios from 'axios';
import { classroomApi, testTemplatesApi } from '../api/testTemplates';
import type { ClassroomCourseDto, TestTemplateSummaryDto } from '../types/testTemplates';

interface Props {
  show: boolean;
  testTemplate: TestTemplateSummaryDto | null;
  onHide: () => void;
  onPublished: () => void;
}

export function PublishTestTemplateModal({ show, testTemplate, onHide, onPublished }: Props) {
  const [courses, setCourses] = useState<ClassroomCourseDto[] | null>(null);
  const [loadingCourses, setLoadingCourses] = useState(false);
  const [coursesError, setCoursesError] = useState<string | null>(null);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [closesAt, setClosesAt] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (!show) return;
    setSelected(new Set());
    setSubmitError(null);
    setCoursesError(null);
    // default close time: 1 week from now, local datetime-local format
    const d = new Date();
    d.setDate(d.getDate() + 7);
    const pad = (n: number) => String(n).padStart(2, '0');
    setClosesAt(
      `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`,
    );

    setLoadingCourses(true);
    classroomApi
      .courses()
      .then(setCourses)
      .catch((e) => {
        let msg = 'Failed to load Google Classroom courses.';
        if (axios.isAxiosError(e)) {
          msg = e.response?.data?.error ?? msg;
        }
        setCoursesError(msg);
        setCourses([]);
      })
      .finally(() => setLoadingCourses(false));
  }, [show]);

  const toggle = (id: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handlePublish = async () => {
    if (!testTemplate) return;
    setSubmitError(null);
    if (selected.size === 0) {
      setSubmitError('Select at least one course.');
      return;
    }
    if (!closesAt) {
      setSubmitError('Pick a closing date and time.');
      return;
    }
    const closesIso = new Date(closesAt).toISOString();
    if (new Date(closesIso).getTime() < Date.now()) {
      setSubmitError('Closing date must be in the future.');
      return;
    }
    setSubmitting(true);
    try {
      await testTemplatesApi.publish(testTemplate.id, {
        courseIds: Array.from(selected),
        closesAt: closesIso,
      });
      onPublished();
      onHide();
    } catch (e) {
      let msg = 'Failed to publish test.';
      if (axios.isAxiosError(e)) msg = e.response?.data?.error ?? msg;
      setSubmitError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal show={show} onHide={submitting ? undefined : onHide} size="lg">
      <Modal.Header closeButton={!submitting}>
        <Modal.Title>Publish test{testTemplate ? `: ${testTemplate.name}` : ''}</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        {submitError && <Alert variant="danger">{submitError}</Alert>}

        <Form.Group className="mb-3">
          <Form.Label>Closes at</Form.Label>
          <Form.Control
            type="datetime-local"
            value={closesAt}
            onChange={(e) => setClosesAt(e.target.value)}
          />
        </Form.Group>

        <Form.Label>Choose Google Classroom groups</Form.Label>
        {loadingCourses && (
          <div className="d-flex align-items-center gap-2 text-muted">
            <Spinner animation="border" size="sm" /> Loading courses…
          </div>
        )}
        {coursesError && <Alert variant="warning">{coursesError}</Alert>}
        {!loadingCourses && courses && courses.length === 0 && !coursesError && (
          <Alert variant="info">No active Google Classroom courses for this account.</Alert>
        )}
        {courses && courses.length > 0 && (
          <Stack gap={2}>
            {courses.map((c) => (
              <Form.Check
                key={c.id}
                type="checkbox"
                id={`course-${c.id}`}
                label={
                  <span>
                    <strong>{c.name}</strong>
                    {c.section ? <span className="text-muted"> — {c.section}</span> : null}
                  </span>
                }
                checked={selected.has(c.id)}
                onChange={() => toggle(c.id)}
              />
            ))}
          </Stack>
        )}
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={onHide} disabled={submitting}>
          Cancel
        </Button>
        <Button
          variant="primary"
          onClick={handlePublish}
          disabled={submitting || loadingCourses || !testTemplate}
        >
          {submitting ? 'Publishing…' : 'Publish'}
        </Button>
      </Modal.Footer>
    </Modal>
  );
}
