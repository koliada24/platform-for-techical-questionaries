import { Container, Card } from 'react-bootstrap';
import { useAuth } from '../auth/AuthContext';

export function HomePage() {
  const { user } = useAuth();
  if (!user) return null;

  return (
    <Container className="py-4">
      <h1 className="h3 mb-4">
        Welcome, {user.fullName ?? user.email}
      </h1>
      <Card>
        <Card.Body>
          <Card.Text>
            You are signed in as a <strong>{user.role}</strong>.{' '}
            {user.hasGoogleLink
              ? 'Your Google account is linked.'
              : 'Your account is not linked to Google yet.'}
          </Card.Text>
        </Card.Body>
      </Card>
    </Container>
  );
}
