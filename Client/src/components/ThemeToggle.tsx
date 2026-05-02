import { Form } from 'react-bootstrap';
import { useTheme } from '../theme/ThemeContext';

export function ThemeToggle({ className }: { className?: string }) {
  const { theme, toggleTheme } = useTheme();
  const isDark = theme === 'dark';
  return (
    <Form.Check
      type="switch"
      id="theme-switch"
      className={className}
      checked={isDark}
      onChange={toggleTheme}
      label={<span aria-hidden="true">{isDark ? '\u{1F319}' : '\u2600\uFE0F'}</span>}
      title={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
    />
  );
}
