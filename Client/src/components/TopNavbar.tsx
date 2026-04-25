import { forwardRef, type MouseEvent, type ReactNode } from 'react';
import { Dropdown, Image, Navbar } from 'react-bootstrap';
import { useAuth } from '../auth/AuthContext';

function Avatar({ src, name }: { src: string | null; name: string }) {
  const initials = name
    .split(/[\s@.]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('');

  if (src) {
    return (
      <Image
        src={src}
        roundedCircle
        width={32}
        height={32}
        alt={name}
        referrerPolicy="no-referrer"
      />
    );
  }
  return (
    <div
      className="rounded-circle bg-secondary text-white d-inline-flex align-items-center justify-content-center"
      style={{ width: 32, height: 32, fontSize: 13, fontWeight: 600 }}
      aria-label={name}
    >
      {initials || '?'}
    </div>
  );
}

interface ToggleProps {
  onClick?: (e: MouseEvent) => void;
  children?: ReactNode;
}

const ProfileToggle = forwardRef<HTMLDivElement, ToggleProps>(
  ({ onClick, children }, ref) => (
    <div
      ref={ref}
      role="button"
      tabIndex={0}
      onClick={(e) => onClick?.(e)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick?.(e as unknown as MouseEvent);
        }
      }}
      className="d-flex align-items-center gap-2"
      style={{ cursor: 'pointer', userSelect: 'none' }}
    >
      {children}
    </div>
  ),
);
ProfileToggle.displayName = 'ProfileToggle';

export function TopNavbar() {
  const { user, logout } = useAuth();
  if (!user) return null;

  const display = user.fullName ?? user.email;

  return (
    <Navbar bg="white" className="border-bottom px-3 py-2">
      <Navbar.Collapse className="justify-content-end">
        <Dropdown align="end">
          <Dropdown.Toggle as={ProfileToggle} id="user-menu">
            <span className="d-none d-sm-inline text-body">{user.email}</span>
            <Avatar src={user.pictureUrl} name={display} />
          </Dropdown.Toggle>
          <Dropdown.Menu>
            <Dropdown.Header className="text-truncate" style={{ maxWidth: 240 }}>
              {display}
            </Dropdown.Header>
            <Dropdown.Divider />
            <Dropdown.Item onClick={logout}>Logout</Dropdown.Item>
          </Dropdown.Menu>
        </Dropdown>
      </Navbar.Collapse>
    </Navbar>
  );
}
