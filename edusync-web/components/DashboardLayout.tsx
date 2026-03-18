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

  const roleColors = {
    lecturer: { gradient: 'from-[#FF6B35] to-[#FFA07A]', light: 'bg-[#FFF3E0]', text: 'text-[#FF6B35]', ring: 'ring-[#FF6B35]' },
    student: { gradient: 'from-emerald-500 to-[#FF6B35]', light: 'bg-emerald-50', text: 'text-emerald-600', ring: 'ring-emerald-500' },
    admin: { gradient: 'from-amber-500 to-[#FF6B35]', light: 'bg-amber-50', text: 'text-amber-600', ring: 'ring-amber-500' },
  };

  const colors = roleColors[role];
  const roleLabel = role === 'lecturer' ? 'Lecturer' : role === 'student' ? 'Student' : 'Admin';
  const roleEmoji = role === 'lecturer' ? '👨‍🏫' : role === 'student' ? '👨‍🎓' : '🛡️';

  const today = new Date();
  const dateStr = today.toLocaleDateString('en-US', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' });

  return (
    <div className="h-screen bg-[#F5F6FA] flex overflow-hidden">
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
            <div className="w-8 h-8 rounded-lg bg-[#FF6B35] flex items-center justify-center text-white text-xs font-bold">
              E
            </div>
            <span className="text-base font-bold text-[#1A1A2E]">EduSync AI</span>
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
                    ? `bg-[#FFF3E0] text-[#FF6B35]`
                    : 'text-[#6B7280] hover:bg-gray-50 hover:text-[#1A1A2E]'
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
              <h1 className="text-lg font-bold text-[#1A1A2E]">{roleLabel} Dashboard</h1>
              <p className="text-xs text-[#9CA3AF]">{dateStr}</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {/* User info */}
            <div className="hidden sm:block text-right">
              <p className="text-sm font-semibold text-[#1A1A2E]">{userName}</p>
              <p className="text-xs text-[#9CA3AF]">{roleLabel}</p>
            </div>
            {profileImage ? (
              <img src={profileImage} alt={userName} className="w-9 h-9 rounded-full object-cover border border-gray-200" />
            ) : (
              <div className={`w-9 h-9 rounded-full bg-gradient-to-br ${colors.gradient} flex items-center justify-center text-white text-sm font-bold`}>
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

        {/* Welcome Banner */}
        <div className="px-4 lg:px-8 pt-6">
          <div className={`bg-gradient-to-r ${colors.gradient} rounded-2xl p-6 lg:p-8 flex items-center justify-between overflow-hidden relative`}>
            <div className="relative z-10">
              <h2 className="text-2xl lg:text-3xl font-extrabold text-white mb-1">
                Hi, {userName.split(' ')[0]}! 👋
              </h2>
              <p className="text-white/80 text-sm">
                Ready to start your day? Here&apos;s your {roleLabel.toLowerCase()} overview.
              </p>
            </div>
            {profileImage ? (
              <img src={profileImage} alt={userName} className="w-24 h-24 lg:w-28 lg:h-28 rounded-full object-cover hidden sm:block border-4 border-white shadow-xl z-20" />
            ) : (
              <div className="text-6xl lg:text-7xl hidden sm:block opacity-80 z-20">{roleEmoji}</div>
            )}
            {/* Decorative circles */}
            <div className="absolute top-[-20px] right-[-20px] w-32 h-32 bg-white/10 rounded-full" />
            <div className="absolute bottom-[-30px] right-[60px] w-20 h-20 bg-white/10 rounded-full" />
          </div>
        </div>

        {/* Page Content */}
        <div className="flex-1 px-4 lg:px-8 py-6 overflow-y-auto">
          {children}
        </div>
      </div>
    </div>
  );
}
