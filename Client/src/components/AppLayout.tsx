import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar, SIDEBAR_WIDTH } from './Sidebar';
import { TopNavbar } from './TopNavbar';

export function AppLayout() {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <div className="bg-light" style={{ minHeight: '100vh' }}>
      <Sidebar collapsed={collapsed} />
      <div
        className="d-flex flex-column"
        style={{
          marginLeft: collapsed ? 0 : SIDEBAR_WIDTH,
          minHeight: '100vh',
          transition: 'margin-left 0.2s ease',
        }}
      >
        <TopNavbar collapsed={collapsed} onToggleSidebar={() => setCollapsed((c) => !c)} />
        <main className="flex-grow-1">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
