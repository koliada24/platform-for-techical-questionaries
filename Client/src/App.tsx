import { useEffect } from 'react';
import { Spinner } from 'react-bootstrap';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { LoginPage } from './pages/LoginPage';
import { HomePage } from './pages/HomePage';
import { TestTemplatesManagementPage } from './pages/TestTemplatesManagementPage';
import { TestTemplateEditorPage } from './pages/TestTemplateEditorPage';
import { PublishedTestsPage } from './pages/PublishedTestsPage';
import { PublishedTestDetailPage } from './pages/PublishedTestDetailPage';
import { StudentAttemptDetailPage } from './pages/StudentAttemptDetailPage';
import { StudentTestPage } from './pages/StudentTestPage';
import { AttemptPage } from './pages/AttemptPage';
import { AttemptSubmittedPage } from './pages/AttemptSubmittedPage';
import { AppLayout } from './components/AppLayout';
import { ThemeProvider } from './theme/ThemeContext';

function Shell() {
  const { user, loading } = useAuth();

  // Strip OAuth-callback query params from URL once authenticated.
  useEffect(() => {
    if (user && window.location.search) {
      window.history.replaceState({}, '', window.location.pathname);
    }
  }, [user]);

  if (loading) {
    return (
      <div className="d-flex vh-100 align-items-center justify-content-center">
        <Spinner animation="border" />
      </div>
    );
  }

  // Public student test route — accessible without an authenticated session;
  // the page itself triggers the student Google login flow when needed.
  return (
    <Routes>
      <Route path="/tests/:id" element={<StudentTestPage />} />
      <Route path="/attempts/:id" element={<AttemptPage />} />
      <Route path="/attempts/:id/submitted" element={<AttemptSubmittedPage />} />
      {user ? (
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/test-templates" element={<TestTemplatesManagementPage />} />
          <Route path="/test-templates/new" element={<TestTemplateEditorPage />} />
          <Route path="/test-templates/:id/edit" element={<TestTemplateEditorPage />} />
          <Route path="/published-tests" element={<PublishedTestsPage />} />
          <Route path="/published-tests/attempts/:attemptId" element={<StudentAttemptDetailPage />} />
          <Route path="/published-tests/:templateId" element={<PublishedTestDetailPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      ) : (
        <Route path="*" element={<LoginPage />} />
      )}
    </Routes>
  );
}

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <BrowserRouter>
          <Shell />
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;

