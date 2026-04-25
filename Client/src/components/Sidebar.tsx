import { NavLink } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

const SIDEBAR_WIDTH = 240;

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `nav-link text-white rounded px-3 py-2 ${isActive ? 'bg-secondary' : ''}`;

export function Sidebar() {
  const { user } = useAuth();

  return (
    <aside
      className="bg-dark text-white d-flex flex-column"
      style={{ width: SIDEBAR_WIDTH, minHeight: '100vh', position: 'sticky', top: 0 }}
    >
      <div className="px-3 py-3 border-bottom border-secondary">
        <div className="fw-bold">Tech Questionnaires</div>
        {user && (
          <small className="text-white-50 d-block text-truncate">
            {user.fullName ?? user.email}
          </small>
        )}
      </div>

      <nav className="nav flex-column p-2 flex-grow-1">
        <NavLink to="/" end className={linkClass}>
          Home
        </NavLink>
        <NavLink to="/tests" className={linkClass}>
          Tests management
        </NavLink>
      </nav>
    </aside>
  );
}
