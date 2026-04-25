import { Button, Container, Navbar, Card, Badge } from 'react-bootstrap';
import { useAuth } from '../auth/AuthContext';

export function HomePage() {
  const { user, logout } = useAuth();
  if (!user) return null;

  return (
    <>
      <Navbar bg="light" className="px-3">
        <Navbar.Brand>Technical Questionnaires</Navbar.Brand>
        <Navbar.Collapse className="justify-content-end">
          <Navbar.Text className="me-3">
            {user.fullName ?? user.email} <Badge bg="info">{user.role}</Badge>
          </Navbar.Text>
          <Button size="sm" variant="outline-secondary" onClick={logout}>Logout</Button>
        </Navbar.Collapse>
      </Navbar>
      <Container className="py-4">
        <Card>
          <Card.Body>
            <Card.Title>Welcome, {user.fullName ?? user.email}!</Card.Title>
            <Card.Text>
              You are signed in as a <strong>{user.role}</strong>.{' '}
              {user.hasGoogleLink
                ? 'Your Google account is linked.'
                : 'Your account is not linked to Google yet.'}
            </Card.Text>
          </Card.Body>
        </Card>
      </Container>
    </>
  );
}
