import { Alert, Button, Card, Container } from 'react-bootstrap';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { loginWithGoogle } = useAuth();

  const params = new URLSearchParams(window.location.search);
  const errorCode = params.get('error');
  const expected = params.get('expected');

  let errorMessage: string | null = null;
  if (errorCode === 'google') {
    errorMessage = 'Google sign-in failed. Please try again.';
  } else if (errorCode === 'role-mismatch') {
    errorMessage = `This Google account is registered as a ${expected}. Teachers should sign in here; students should use the test link they received.`;
  }

  return (
    <Container style={{ maxWidth: 460 }} className="py-5">
      <Card>
        <Card.Body>
          <Card.Title className="mb-2">Teacher sign in</Card.Title>
          <Card.Text className="text-muted mb-4">
            Use your Google account to manage tests. Students join via a test link.
          </Card.Text>

          {errorMessage && <Alert variant="danger">{errorMessage}</Alert>}

          <Button variant="primary" className="w-100" onClick={() => loginWithGoogle('Teacher')}>
            Continue with Google
          </Button>
        </Card.Body>
      </Card>
    </Container>
  );
}
