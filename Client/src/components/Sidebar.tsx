import { NavLink } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export const SIDEBAR_WIDTH = 240;

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `nav-link text-white rounded px-3 py-2 ${isActive ? 'bg-secondary' : ''}`;

interface SidebarProps {
  collapsed?: boolean;
}

export function Sidebar({ collapsed = false }: SidebarProps) {
  const { user } = useAuth();

  return (
    <aside
      className="bg-dark text-white d-flex flex-column"
      style={{
        width: SIDEBAR_WIDTH,
        position: 'fixed',
        top: 0,
        left: 0,
        bottom: 0,
        zIndex: 1040,
        transform: collapsed ? `translateX(-${SIDEBAR_WIDTH}px)` : 'translateX(0)',
        transition: 'transform 0.2s ease',
        overflowY: 'auto',
      }}
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
        <NavLink to="/test-templates" className={linkClass}>
          Templates Management
        </NavLink>
      </nav>
    </aside>
  );
}