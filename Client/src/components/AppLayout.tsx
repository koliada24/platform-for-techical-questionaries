import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { TopNavbar } from './TopNavbar';

export function AppLayout() {
  return (
    <div className="d-flex" style={{ minHeight: '100vh' }}>
      <Sidebar />
      <div className="flex-grow-1 d-flex flex-column bg-light">
        <TopNavbar />
        <main className="flex-grow-1">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
