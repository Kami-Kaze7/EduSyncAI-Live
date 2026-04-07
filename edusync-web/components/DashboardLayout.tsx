'use client';

import { useState } from 'react';
import Link from 'next/link';

interface NavItem {
  id: string;
  label: string;
  icon: string;
  href?: string;
}

interface DashboardLayoutProps {
  role: 'lecturer' | 'student' | 'admin';
  userName: string;
  navItems: NavItem[];
  activeNav: string;
  onNavChange: (id: string) => void;
  onLogout: () => void;
  children: React.ReactNode;
  profileImage?: string;
}

export default function DashboardLayout({
  role,
  userName,
  navItems,
  activeNav,
  onNavChange,
  onLogout,
  children,
  profileImage,
}: DashboardLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const roleLabel = role === 'lecturer' ? 'Lecturer' : role === 'student' ? 'Student' : 'Admin';
  const roleEmoji = role === 'lecturer' ? '👨‍🏫' : role === 'student' ? '👨‍🎓' : '🛡️';

  const today = new Date();
  const dateStr = today.toLocaleDateString('en-US', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' });

  return (
    <div className="h-screen bg-gray-50 flex overflow-hidden">
      {/* Mobile overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black/40 z-40 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside className={`fixed lg:static inset-y-0 left-0 z-50 w-[240px] bg-white border-r border-gray-100 flex flex-col transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`}>
        {/* Logo */}
        <div className="px-6 py-5 border-b border-gray-100">
          <Link href="/" className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-blue-600 flex items-center justify-center text-white text-xs font-bold">
              E
            </div>
            <span className="text-base font-bold text-gray-900">EduSync AI</span>
          </Link>
        </div>

        {/* Nav Items */}
        <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
          {navItems.map((item) => {
            const isActive = activeNav === item.id;
            return (
              <button
                key={item.id}
                onClick={() => {
                  onNavChange(item.id);
                  setSidebarOpen(false);
                }}
                className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 ${
                  isActive
                    ? `bg-blue-50 text-blue-600`
                    : 'text-gray-500 hover:bg-gray-50 hover:text-gray-900'
                }`}
              >
                <span className="text-lg">{item.icon}</span>
                <span>{item.label}</span>
              </button>
            );
          })}
        </nav>

        {/* Sidebar Footer */}
        <div className="px-4 py-4 border-t border-gray-100">
          <button
            onClick={onLogout}
            className="w-full flex items-center gap-3 px-4 py-2.5 rounded-xl text-sm font-medium text-red-500 hover:bg-red-50 transition-colors"
          >
            <span className="text-lg">🚪</span>
            <span>Logout</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top Header */}
        <header className="bg-white border-b border-gray-100 px-4 lg:px-8 py-3 flex items-center justify-between sticky top-0 z-30">
          <div className="flex items-center gap-4">
            {/* Mobile menu button */}
            <button
              className="lg:hidden w-9 h-9 flex items-center justify-center rounded-lg hover:bg-gray-100 transition-colors"
              onClick={() => setSidebarOpen(true)}
            >
              <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                <path d="M3 5H17M3 10H17M3 15H17" stroke="#1A1A2E" strokeWidth="2" strokeLinecap="round" />
              </svg>
            </button>
            <div>
              <h1 className="text-lg font-bold text-gray-900">{roleLabel} Dashboard</h1>
              <p className="text-xs text-gray-500">{dateStr}</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {/* User info */}
            <div className="hidden sm:block text-right">
              <p className="text-sm font-semibold text-gray-900">{userName}</p>
              <p className="text-xs text-gray-500">{roleLabel}</p>
            </div>
            {profileImage ? (
              <img src={profileImage} alt={userName} className="w-9 h-9 rounded-full object-cover border border-gray-200" />
            ) : (
              <div className={`w-9 h-9 rounded-full bg-blue-600 flex items-center justify-center text-white text-sm font-bold`}>
                {userName.charAt(0).toUpperCase()}
              </div>
            )}
            <button
              onClick={onLogout}
              className="w-9 h-9 flex items-center justify-center rounded-lg text-red-400 hover:bg-red-50 hover:text-red-600 transition-colors"
              title="Logout"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
                <polyline points="16 17 21 12 16 7" />
                <line x1="21" y1="12" x2="9" y2="12" />
              </svg>
            </button>
          </div>
        </header>


        {/* Page Content */}
        <div className="flex-1 px-4 lg:px-8 py-6 overflow-y-auto">
          {children}
        </div>
      </div>
    </div>
  );
}
