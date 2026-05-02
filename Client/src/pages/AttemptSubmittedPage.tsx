import { Card, Container } from 'react-bootstrap';

export function AttemptSubmittedPage() {
  return (
    <Container className="py-5 d-flex justify-content-center">
      <Card style={{ maxWidth: 560 }} className="text-center">
        <Card.Body className="p-4">
          <Card.Title as="h3" className="mb-3">
            Thanks for taking the test!
          </Card.Title>
          <Card.Text className="text-muted">
            Your answers have been submitted. Your teacher will review them and your grade will be
            posted to Google Classroom.
          </Card.Text>
          <Card.Text className="text-muted small mb-0">
            You can now close this tab.
          </Card.Text>
        </Card.Body>
      </Card>
    </Container>
  );
}
