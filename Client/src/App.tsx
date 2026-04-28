import { useEffect } from 'react';
import { Spinner } from 'react-bootstrap';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { LoginPage } from './pages/LoginPage';
import { HomePage } from './pages/HomePage';
import { TestTemplatesManagementPage } from './pages/TestTemplatesManagementPage';
import { TestTemplateEditorPage } from './pages/TestTemplateEditorPage';
import { AppLayout } from './components/AppLayout';

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

  if (!user) return <LoginPage />;

  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/test-templates" element={<TestTemplatesManagementPage />} />
        <Route path="/test-templates/new" element={<TestTemplateEditorPage />} />
        <Route path="/test-templates/:id/edit" element={<TestTemplateEditorPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Shell />
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;

