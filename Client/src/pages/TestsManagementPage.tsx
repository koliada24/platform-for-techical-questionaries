import { Card, Container } from 'react-bootstrap';

export function TestsManagementPage() {
  return (
    <Container className="py-4">
      <h1 className="h3 mb-4">Tests management</h1>
      <Card>
        <Card.Body>
          <Card.Text className="text-muted mb-0">
            No tests yet. Test creation and listing will be available here.
          </Card.Text>
        </Card.Body>
      </Card>
    </Container>
  );
}
