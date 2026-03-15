'use client';

import Link from 'next/link';
import { useState, useEffect } from 'react';

export default function LandingPage() {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 20);
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  return (
    <div className="min-h-screen bg-[#0a0a1a] text-white overflow-x-hidden">
      {/* Animated Background Gradient */}
      <div className="fixed inset-0 z-0">
        <div className="absolute top-[-50%] left-[-20%] w-[80%] h-[80%] rounded-full bg-[radial-gradient(circle,rgba(99,102,241,0.15),transparent_60%)] animate-pulse" style={{ animationDuration: '8s' }} />
        <div className="absolute bottom-[-30%] right-[-20%] w-[70%] h-[70%] rounded-full bg-[radial-gradient(circle,rgba(168,85,247,0.12),transparent_60%)] animate-pulse" style={{ animationDuration: '10s' }} />
        <div className="absolute top-[20%] right-[10%] w-[40%] h-[40%] rounded-full bg-[radial-gradient(circle,rgba(59,130,246,0.08),transparent_60%)] animate-pulse" style={{ animationDuration: '6s' }} />
      </div>

      {/* Navbar */}
      <nav className={`fixed top-0 w-full z-50 transition-all duration-300 ${scrolled ? 'bg-[#0a0a1a]/90 backdrop-blur-xl shadow-lg shadow-black/20 border-b border-white/5' : ''}`}>
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-lg font-bold shadow-lg shadow-indigo-500/30">
              E
            </div>
            <span className="text-xl font-bold bg-gradient-to-r from-white to-white/70 bg-clip-text text-transparent">
              EduSync AI
            </span>
          </div>
          <div className="hidden md:flex items-center gap-8">
            <a href="#features" className="text-sm text-white/60 hover:text-white transition-colors">Features</a>
            <a href="#portals" className="text-sm text-white/60 hover:text-white transition-colors">Portals</a>
            <a href="#about" className="text-sm text-white/60 hover:text-white transition-colors">About</a>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="relative z-10 min-h-screen flex items-center justify-center px-6">
        <div className="max-w-5xl mx-auto text-center">
          <div className="inline-flex items-center gap-2 bg-white/5 border border-white/10 rounded-full px-4 py-2 mb-8 text-sm text-white/70">
            <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse" />
            AI-Powered Classroom Management
          </div>
          <h1 className="text-5xl sm:text-6xl lg:text-7xl font-bold leading-tight mb-6">
            <span className="bg-gradient-to-r from-white via-white to-white/60 bg-clip-text text-transparent">
              Smart Education,
            </span>
            <br />
            <span className="bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent">
              Seamless Sync
            </span>
          </h1>
          <p className="text-lg sm:text-xl text-white/50 max-w-2xl mx-auto mb-12 leading-relaxed">
            A unified platform for lecturers, students, and administrators. 
            Real-time classes, AI-powered attendance, interactive whiteboards, 
            and intelligent course management — all in one place.
          </p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <a href="#portals" className="group relative px-8 py-4 bg-gradient-to-r from-indigo-600 to-purple-600 rounded-xl text-white font-semibold shadow-lg shadow-indigo-500/25 hover:shadow-xl hover:shadow-indigo-500/40 transition-all duration-300 hover:scale-105">
              Get Started
              <span className="ml-2 inline-block transition-transform group-hover:translate-x-1">→</span>
            </a>
            <a href="#features" className="px-8 py-4 bg-white/5 border border-white/10 rounded-xl text-white/80 font-semibold hover:bg-white/10 hover:border-white/20 transition-all duration-300">
              Learn More
            </a>
          </div>
        </div>

        {/* Scroll indicator */}
        <div className="absolute bottom-10 left-1/2 -translate-x-1/2 flex flex-col items-center gap-2 text-white/30">
          <span className="text-xs">Scroll down</span>
          <div className="w-6 h-10 rounded-full border-2 border-white/20 flex items-start justify-center p-1">
            <div className="w-1.5 h-3 rounded-full bg-white/40 animate-bounce" style={{ animationDuration: '2s' }} />
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section id="features" className="relative z-10 py-24 px-6">
        <div className="max-w-7xl mx-auto">
          <div className="text-center mb-16">
            <p className="text-indigo-400 text-sm font-semibold tracking-wider uppercase mb-3">
              Powerful Features
            </p>
            <h2 className="text-3xl sm:text-4xl font-bold mb-4">
              Everything You Need for Modern Education
            </h2>
            <p className="text-white/50 max-w-xl mx-auto">
              EduSync AI combines cutting-edge AI with intuitive design to transform classroom experiences.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[
              { icon: '🎓', title: 'Course Management', desc: 'Create courses, manage curricula, enroll students, and track academic progress with ease.', color: 'from-indigo-500/20 to-indigo-600/5', border: 'border-indigo-500/20' },
              { icon: '👁️', title: 'AI Face Recognition', desc: 'Automated attendance using facial recognition. No more manual roll calls — just show up and get marked.', color: 'from-purple-500/20 to-purple-600/5', border: 'border-purple-500/20' },
              { icon: '📡', title: 'Live Streaming', desc: 'Real-time camera streaming and screen sharing with picture-in-picture for engaging virtual lectures.', color: 'from-blue-500/20 to-blue-600/5', border: 'border-blue-500/20' },
              { icon: '🖊️', title: 'Interactive Whiteboard', desc: 'Draw, annotate, import documents, and save whiteboard sessions. Share with students instantly.', color: 'from-green-500/20 to-green-600/5', border: 'border-green-500/20' },
              { icon: '🎬', title: 'Screen Recording', desc: 'Record lectures, whiteboard sessions, and presentations. Students can review them anytime.', color: 'from-pink-500/20 to-pink-600/5', border: 'border-pink-500/20' },
              { icon: '📊', title: 'Attendance Analytics', desc: 'Comprehensive attendance reports with charts, trends, and exportable data for each course and session.', color: 'from-orange-500/20 to-orange-600/5', border: 'border-orange-500/20' },
            ].map((f, i) => (
              <div key={i} className={`group bg-gradient-to-br ${f.color} border ${f.border} rounded-2xl p-8 hover:scale-[1.02] transition-all duration-300 hover:shadow-lg hover:shadow-black/20`}>
                <div className="text-4xl mb-4">{f.icon}</div>
                <h3 className="text-xl font-semibold mb-3">{f.title}</h3>
                <p className="text-white/50 leading-relaxed text-sm">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Portal Section */}
      <section id="portals" className="relative z-10 py-24 px-6">
        <div className="max-w-5xl mx-auto">
          <div className="text-center mb-16">
            <p className="text-purple-400 text-sm font-semibold tracking-wider uppercase mb-3">
              Access Your Dashboard
            </p>
            <h2 className="text-3xl sm:text-4xl font-bold mb-4">
              Choose Your Portal
            </h2>
            <p className="text-white/50 max-w-xl mx-auto">
              Select your role to log in and access your personalized dashboard.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Lecturer Portal */}
            <Link href="/login" className="group block">
              <div className="relative bg-gradient-to-br from-indigo-600/10 to-indigo-900/10 border border-indigo-500/20 rounded-2xl p-10 text-center hover:border-indigo-500/50 hover:shadow-2xl hover:shadow-indigo-500/10 transition-all duration-500 hover:scale-105">
                <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-indigo-500 to-indigo-700 flex items-center justify-center text-4xl shadow-lg shadow-indigo-500/30 group-hover:shadow-xl group-hover:shadow-indigo-500/50 transition-all duration-300">
                  👨‍🏫
                </div>
                <h3 className="text-2xl font-bold mb-3 text-white">Lecturer</h3>
                <p className="text-white/50 text-sm mb-6 leading-relaxed">
                  Manage courses, conduct live sessions, use interactive whiteboards, and track attendance.
                </p>
                <div className="inline-flex items-center gap-2 text-indigo-400 font-semibold group-hover:gap-3 transition-all text-sm">
                  Open Dashboard <span className="text-lg">→</span>
                </div>
              </div>
            </Link>

            {/* Student Portal */}
            <Link href="/student/login" className="group block">
              <div className="relative bg-gradient-to-br from-emerald-600/10 to-emerald-900/10 border border-emerald-500/20 rounded-2xl p-10 text-center hover:border-emerald-500/50 hover:shadow-2xl hover:shadow-emerald-500/10 transition-all duration-500 hover:scale-105">
                <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-emerald-500 to-emerald-700 flex items-center justify-center text-4xl shadow-lg shadow-emerald-500/30 group-hover:shadow-xl group-hover:shadow-emerald-500/50 transition-all duration-300">
                  👨‍🎓
                </div>
                <h3 className="text-2xl font-bold mb-3 text-white">Student</h3>
                <p className="text-white/50 text-sm mb-6 leading-relaxed">
                  Browse courses, join live classes, view materials, check attendance records, and more.
                </p>
                <div className="inline-flex items-center gap-2 text-emerald-400 font-semibold group-hover:gap-3 transition-all text-sm">
                  Open Dashboard <span className="text-lg">→</span>
                </div>
              </div>
            </Link>

            {/* Admin Portal */}
            <Link href="/admin/login" className="group block">
              <div className="relative bg-gradient-to-br from-amber-600/10 to-amber-900/10 border border-amber-500/20 rounded-2xl p-10 text-center hover:border-amber-500/50 hover:shadow-2xl hover:shadow-amber-500/10 transition-all duration-500 hover:scale-105">
                <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-amber-500 to-amber-700 flex items-center justify-center text-4xl shadow-lg shadow-amber-500/30 group-hover:shadow-xl group-hover:shadow-amber-500/50 transition-all duration-300">
                  🛡️
                </div>
                <h3 className="text-2xl font-bold mb-3 text-white">Administrator</h3>
                <p className="text-white/50 text-sm mb-6 leading-relaxed">
                  Manage lecturers, students, system settings, and oversee the entire platform.
                </p>
                <div className="inline-flex items-center gap-2 text-amber-400 font-semibold group-hover:gap-3 transition-all text-sm">
                  Open Dashboard <span className="text-lg">→</span>
                </div>
              </div>
            </Link>
          </div>
        </div>
      </section>

      {/* About Section */}
      <section id="about" className="relative z-10 py-24 px-6">
        <div className="max-w-4xl mx-auto">
          <div className="bg-gradient-to-br from-white/5 to-white/[0.02] border border-white/10 rounded-3xl p-12 md:p-16">
            <div className="text-center">
              <div className="w-16 h-16 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-2xl font-bold shadow-lg shadow-indigo-500/30">
                E
              </div>
              <h2 className="text-3xl font-bold mb-6">About EduSync AI</h2>
              <p className="text-white/50 leading-relaxed mb-8 max-w-2xl mx-auto">
                EduSync AI is a comprehensive educational platform designed to bridge the gap 
                between traditional classroom management and modern AI-powered solutions. 
                Our platform enables lecturers to conduct live sessions, manage course materials, 
                and track student attendance using cutting-edge facial recognition technology — 
                all from a single, unified interface.
              </p>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mt-12">
                {[
                  { num: '🎓', label: 'Real-time Classes' },
                  { num: '🤖', label: 'AI Attendance' },
                  { num: '🖊️', label: 'Whiteboards' },
                  { num: '📱', label: 'Multi-Platform' },
                ].map((s, i) => (
                  <div key={i} className="text-center">
                    <p className="text-3xl mb-2">{s.num}</p>
                    <p className="text-white/40 text-xs font-medium">{s.label}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="relative z-10 border-t border-white/5 py-12 px-6">
        <div className="max-w-7xl mx-auto flex flex-col md:flex-row items-center justify-between gap-6">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-sm font-bold shadow-md shadow-indigo-500/20">
              E
            </div>
            <span className="text-sm text-white/50">
              EduSync AI — Smart Education, Seamless Sync
            </span>
          </div>
          <p className="text-xs text-white/30">
            © {new Date().getFullYear()} EduSync AI. All rights reserved.
          </p>
        </div>
      </footer>
    </div>
  );
}
