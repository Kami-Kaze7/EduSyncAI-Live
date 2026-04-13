'use client';

import Link from 'next/link';
import { useState, useEffect } from 'react';
import axios from 'axios';

export default function LandingPage() {
  const [scrolled, setScrolled] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 20);
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  return (
    <div className="min-h-screen bg-[#FAFAFA] text-[#1A1A2E] overflow-x-hidden" style={{ fontFamily: "'Inter', sans-serif" }}>

      {/* Navbar */}
      <nav className={`fixed top-0 w-full z-50 transition-all duration-300 ${scrolled ? 'bg-white/95 backdrop-blur-md shadow-sm' : 'bg-transparent'}`}>
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-9 h-9 rounded-lg bg-[#FF6B35] flex items-center justify-center text-white text-sm font-bold shadow-md">
              E
            </div>
            <span className="text-lg font-bold text-[#1A1A2E]">
              EduSync AI
            </span>
          </div>
          <div className="hidden md:flex items-center gap-8">
            <a href="#features" className="text-sm text-[#6B7280] hover:text-[#1A1A2E] transition-colors font-medium">Features</a>
            <a href="#services" className="text-sm text-[#6B7280] hover:text-[#1A1A2E] transition-colors font-medium">Services</a>
            <a href="#courses" className="text-sm text-[#6B7280] hover:text-[#1A1A2E] transition-colors font-medium">Courses</a>
            <a href="#portals" className="text-sm text-[#6B7280] hover:text-[#1A1A2E] transition-colors font-medium">Portals</a>
            <a href="#about" className="text-sm text-[#6B7280] hover:text-[#1A1A2E] transition-colors font-medium">About</a>
          </div>
          <div className="hidden md:flex items-center gap-3">
            <a href="#portals" className="px-5 py-2.5 bg-[#1A1A2E] text-white text-sm font-semibold rounded-lg hover:bg-[#2d2d4e] transition-colors shadow-md">
              Get Started
            </a>
          </div>
          {/* Mobile menu button */}
          <button
            className="md:hidden w-10 h-10 flex items-center justify-center rounded-lg hover:bg-gray-100 transition-colors"
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
          >
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
              {mobileMenuOpen ? (
                <path d="M5 5L15 15M15 5L5 15" stroke="#1A1A2E" strokeWidth="2" strokeLinecap="round" />
              ) : (
                <>
                  <path d="M3 5H17M3 10H17M3 15H17" stroke="#1A1A2E" strokeWidth="2" strokeLinecap="round" />
                </>
              )}
            </svg>
          </button>
        </div>
        {/* Mobile menu */}
        {mobileMenuOpen && (
          <div className="md:hidden bg-white border-t border-gray-100 px-6 py-4 space-y-3">
            <a href="#features" className="block text-sm text-[#6B7280] hover:text-[#1A1A2E] font-medium" onClick={() => setMobileMenuOpen(false)}>Features</a>
            <a href="#services" className="block text-sm text-[#6B7280] hover:text-[#1A1A2E] font-medium" onClick={() => setMobileMenuOpen(false)}>Services</a>
            <a href="#portals" className="block text-sm text-[#6B7280] hover:text-[#1A1A2E] font-medium" onClick={() => setMobileMenuOpen(false)}>Portals</a>
            <a href="#about" className="block text-sm text-[#6B7280] hover:text-[#1A1A2E] font-medium" onClick={() => setMobileMenuOpen(false)}>About</a>
            <a href="#portals" className="block px-5 py-2.5 bg-[#1A1A2E] text-white text-sm font-semibold rounded-lg text-center" onClick={() => setMobileMenuOpen(false)}>Get Started</a>
          </div>
        )}
      </nav>

      {/* Hero Section */}
      <section className="relative min-h-screen flex items-center px-6 pt-20 overflow-hidden">
        {/* Decorative shapes */}
        <div className="absolute top-20 right-20 w-32 h-32 bg-[#FFE4D6] rounded-full opacity-60 blur-sm" />
        <div className="absolute top-40 right-60 w-16 h-16 bg-[#E8F5E9] rounded-lg rotate-12 opacity-70" />
        <div className="absolute bottom-32 left-10 w-20 h-20 bg-[#FFF3E0] rounded-full opacity-50" />
        <div className="absolute top-32 left-[30%] w-12 h-12 bg-[#F3E5F5] rounded-lg rotate-45 opacity-60" />
        <div className="absolute bottom-40 right-[35%] w-24 h-24 bg-[#E3F2FD] rounded-full opacity-40" />

        <div className="max-w-7xl mx-auto w-full grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
          {/* Left content */}
          <div className="relative z-10">
            <div className="inline-flex items-center gap-2 bg-[#FFF3E0] rounded-full px-4 py-1.5 mb-6">
              <span className="text-[#FF6B35] text-sm font-semibold">1,200+</span>
              <span className="text-[#6B7280] text-sm">Active Users</span>
            </div>
            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-extrabold leading-[1.1] mb-6 text-[#1A1A2E]">
              Building smart
              <br />
              <span className="text-[#1A1A2E]">education and</span>
              <br />
              <span className="bg-gradient-to-r from-[#FF6B35] to-[#FFA07A] bg-clip-text text-transparent">experience.</span>
            </h1>
            <p className="text-base text-[#6B7280] max-w-lg mb-8 leading-relaxed">
              A unified platform for lecturers, students, and administrators.
              Real-time classes, AI-powered attendance, interactive whiteboards,
              and intelligent course management — all in one place.
            </p>
            <div className="flex flex-wrap items-center gap-4">
              <a href="#portals" className="group inline-flex items-center gap-2 px-7 py-3.5 bg-[#1A1A2E] text-white text-sm font-semibold rounded-xl hover:bg-[#2d2d4e] transition-all duration-300 shadow-lg hover:shadow-xl">
                Get Started
                <svg className="w-4 h-4 transition-transform group-hover:translate-x-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" /></svg>
              </a>
              <a href="#features" className="inline-flex items-center gap-2 px-7 py-3.5 bg-white border border-gray-200 text-[#1A1A2E] text-sm font-semibold rounded-xl hover:border-gray-300 hover:shadow-md transition-all duration-300">
                <svg className="w-4 h-4 text-[#FF6B35]" fill="currentColor" viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
                Watch Demo
              </a>
            </div>
            <div className="flex items-center gap-2 mt-8">
              <div className="flex items-center gap-1">
                {[1,2,3,4,5].map(i => (
                  <svg key={i} className="w-4 h-4 text-[#FFB74D]" fill="currentColor" viewBox="0 0 20 20"><path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/></svg>
                ))}
              </div>
              <span className="text-sm font-semibold text-[#1A1A2E]">5.0 Rated</span>
              <span className="text-xs text-[#9CA3AF]">• Trusted by 50+ Institutions</span>
            </div>
          </div>

          {/* Right side — Floating cards */}
          <div className="relative hidden lg:block h-[500px]">
            {/* Main image placeholder */}
            <div className="absolute top-8 left-8 right-0 bottom-8 bg-gradient-to-br from-[#FF6B35]/10 to-[#FFA07A]/10 rounded-3xl overflow-hidden border border-[#FF6B35]/10">
              <div className="absolute inset-0 flex items-center justify-center">
                <div className="text-center">
                  <div className="text-7xl mb-4">🎓</div>
                  <p className="text-[#FF6B35] font-semibold text-lg">Smart Learning</p>
                  <p className="text-[#9CA3AF] text-sm">Powered by AI</p>
                </div>
              </div>
            </div>

            {/* Floating stat card — top left */}
            <div className="absolute top-0 left-0 bg-white rounded-2xl shadow-xl p-4 z-20 landing-float" style={{ animationDelay: '0s' }}>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-[#E3F2FD] flex items-center justify-center text-lg">📊</div>
                <div>
                  <p className="text-lg font-bold text-[#1A1A2E]">47.5k</p>
                  <p className="text-xs text-[#9CA3AF]">Sessions Completed</p>
                </div>
              </div>
            </div>

            {/* Floating card — mid right */}
            <div className="absolute top-24 right-0 bg-white rounded-2xl shadow-xl p-4 z-20 landing-float" style={{ animationDelay: '1s' }}>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-[#E8F5E9] flex items-center justify-center">
                  <svg className="w-5 h-5 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" /></svg>
                </div>
                <div>
                  <p className="text-sm font-semibold text-[#1A1A2E]">AI Attendance</p>
                  <p className="text-xs text-[#9CA3AF]">Facial Recognition</p>
                </div>
              </div>
            </div>

            {/* Floating card — bottom left */}
            <div className="absolute bottom-0 left-4 bg-white rounded-2xl shadow-xl p-4 z-20 landing-float" style={{ animationDelay: '2s' }}>
              <p className="text-xs text-[#9CA3AF] mb-1">Daily Active Users</p>
              <p className="text-2xl font-extrabold text-[#1A1A2E]">800<span className="text-[#FF6B35]">+</span></p>
            </div>

            {/* Floating card — bottom right */}
            <div className="absolute bottom-16 right-4 bg-[#1A1A2E] rounded-2xl shadow-xl p-4 z-20 landing-float" style={{ animationDelay: '0.5s' }}>
              <div className="flex items-center gap-2">
                <span className="text-white text-sm font-semibold">Satisfaction</span>
                <span className="text-[#FF6B35] text-xl font-extrabold">25%</span>
              </div>
              <p className="text-white/50 text-xs">↑ increased this month</p>
            </div>
          </div>
        </div>
      </section>

      {/* Trust Bar — Scrolling Logos */}
      <section className="relative z-10 py-12 border-t border-gray-100 bg-white">
        <div className="max-w-7xl mx-auto px-6">
          <p className="text-center text-xs text-[#9CA3AF] uppercase tracking-widest font-semibold mb-8">
            Trusted by top educational institutions worldwide
          </p>
          <div className="flex items-center justify-center gap-12 flex-wrap opacity-40">
            {['🏛️ University of Lagos', '🏫 Covenant Uni', '🎓 FUTA', '🏛️ OAU', '🏫 LASU', '🎓 UNILAG'].map((name, i) => (
              <span key={i} className="text-lg font-bold text-[#1A1A2E] whitespace-nowrap">{name}</span>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section — "Why we are trusted" */}
      <section id="features" className="relative z-10 py-24 px-6 bg-white">
        <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
          {/* Left — Visual */}
          <div className="relative">
            <div className="bg-gradient-to-br from-[#FFF3E0] to-[#FFE4D6] rounded-3xl p-8 aspect-square max-w-md mx-auto flex items-center justify-center">
              <div className="text-center">
                <div className="text-8xl mb-6">👨‍🏫</div>
                <p className="text-[#1A1A2E] font-bold text-xl">Live Teaching</p>
                <p className="text-[#6B7280] text-sm mt-2">Real-time interaction</p>
              </div>
            </div>
            {/* Floating social proof card */}
            <div className="absolute bottom-4 left-0 bg-white rounded-xl shadow-lg p-3 flex items-center gap-3 landing-float" style={{ animationDelay: '1.5s' }}>
              <div className="w-8 h-8 rounded-full bg-[#FF6B35] flex items-center justify-center text-white text-xs font-bold">📱</div>
              <div>
                <p className="text-xs font-semibold text-[#1A1A2E]">Mobile Ready</p>
                <p className="text-[10px] text-[#9CA3AF]">Access anywhere</p>
              </div>
            </div>
          </div>

          {/* Right — Content */}
          <div>
            <h2 className="text-3xl sm:text-4xl font-extrabold text-[#1A1A2E] mb-4 leading-tight">
              See why we are trusted
              <br />the world over.
            </h2>
            <p className="text-[#6B7280] mb-8 leading-relaxed max-w-md">
              EduSync AI combines cutting-edge AI technology with intuitive design
              to transform classroom experiences. From attendance to course management,
              everything works seamlessly together.
            </p>
            {/* Stats row */}
            <div className="flex gap-8 mb-8">
              {[
                { value: '5K%', label: 'Happy Users' },
                { value: '756+', label: 'Sessions Done' },
                { value: '100%', label: 'Satisfaction' },
              ].map((stat, i) => (
                <div key={i} className="text-center">
                  <p className="text-2xl sm:text-3xl font-extrabold text-[#1A1A2E]">{stat.value}</p>
                  <p className="text-xs text-[#9CA3AF] mt-1">{stat.label}</p>
                </div>
              ))}
            </div>
            <a href="#services" className="group inline-flex items-center gap-2 px-6 py-3 bg-[#1A1A2E] text-white text-sm font-semibold rounded-xl hover:bg-[#2d2d4e] transition-all shadow-md">
              Learn More
              <svg className="w-4 h-4 transition-transform group-hover:translate-x-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" /></svg>
            </a>
          </div>
        </div>
      </section>

      {/* Services Section */}
      <section id="services" className="relative z-10 py-24 px-6 bg-[#FDF5EF]">
        <div className="max-w-7xl mx-auto">
          <div className="mb-16">
            <p className="text-xs text-[#FF6B35] uppercase tracking-widest font-semibold mb-3">WHAT WE DO</p>
            <h2 className="text-3xl sm:text-4xl font-extrabold text-[#1A1A2E] leading-tight">
              Our included
              <br />
              <span className="italic font-serif">services.</span>
            </h2>
            <p className="text-[#6B7280] mt-4 max-w-md">
              Everything you need for modern education, all built into one seamless platform.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {[
              { icon: '📊', title: 'Advanced Analytics', desc: 'Comprehensive attendance reports with charts, trends, and exportable data for each course and session.' },
              { icon: '🎨', title: 'Interactive Whiteboard', desc: 'Draw, annotate, import documents, and save whiteboard sessions. Share with students instantly.' },
              { icon: '🤖', title: 'AI Face Recognition', desc: 'Automated attendance using facial recognition. No more manual roll calls — just show up and get marked.' },
              { icon: '📡', title: 'Live Streaming', desc: 'Real-time camera streaming and screen sharing with picture-in-picture for engaging virtual lectures.' },
              { icon: '🎬', title: 'Screen Recording', desc: 'Record lectures, whiteboard sessions, and presentations. Students can review them anytime.' },
              { icon: '🎓', title: 'Course Management', desc: 'Create courses, manage curricula, enroll students, and track academic progress with ease.' },
            ].map((service, i) => (
              <div key={i} className="group bg-white rounded-2xl p-8 hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-[#FF6B35]/20">
                <div className="w-12 h-12 rounded-xl bg-[#FFF3E0] flex items-center justify-center text-2xl mb-5 group-hover:bg-[#FF6B35] group-hover:text-white transition-colors duration-300 group-hover:scale-110 group-hover:shadow-lg">
                  {service.icon}
                </div>
                <h3 className="text-lg font-bold text-[#1A1A2E] mb-3">{service.title}</h3>
                <p className="text-sm text-[#6B7280] leading-relaxed">{service.desc}</p>
                <div className="mt-5 inline-flex items-center gap-1 text-[#FF6B35] text-sm font-semibold opacity-0 group-hover:opacity-100 transition-opacity">
                  Learn more
                  <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" /></svg>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Portal Section */}
      <section id="portals" className="relative z-10 py-24 px-6 bg-white">
        <div className="max-w-5xl mx-auto">
          <div className="text-center mb-16">
            <p className="text-xs text-[#FF6B35] uppercase tracking-widest font-semibold mb-3">
              Access Your Dashboard
            </p>
            <h2 className="text-3xl sm:text-4xl font-extrabold text-[#1A1A2E] mb-4">
              Choose Your Portal
            </h2>
            <p className="text-[#6B7280] max-w-xl mx-auto">
              Select your role to log in and access your personalized dashboard.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Lecturer Portal */}
            <Link href="/login" className="group block">
              <div className="relative bg-white border-2 border-gray-100 rounded-2xl p-10 text-center hover:border-[#FF6B35]/40 hover:shadow-2xl transition-all duration-500 hover:scale-[1.03]">
                <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-[#FF6B35] to-[#FFA07A] flex items-center justify-center text-4xl shadow-lg shadow-[#FF6B35]/20 group-hover:shadow-xl group-hover:shadow-[#FF6B35]/30 transition-all duration-300">
                  👨‍🏫
                </div>
                <h3 className="text-2xl font-bold mb-3 text-[#1A1A2E]">Lecturer</h3>
                <p className="text-[#6B7280] text-sm mb-6 leading-relaxed">
                  Manage courses, conduct live sessions, use interactive whiteboards, and track attendance.
                </p>
                <div className="inline-flex items-center gap-2 text-[#FF6B35] font-semibold group-hover:gap-3 transition-all text-sm">
                  Open Dashboard <span className="text-lg">→</span>
                </div>
              </div>
            </Link>

            {/* Student Portal */}
            <Link href="/student/login" className="group block">
              <div className="relative bg-white border-2 border-gray-100 rounded-2xl p-10 text-center hover:border-blue-300/40 hover:shadow-2xl transition-all duration-500 hover:scale-[1.03]">
                <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-emerald-500 to-emerald-400 flex items-center justify-center text-4xl shadow-lg shadow-emerald-500/20 group-hover:shadow-xl group-hover:shadow-emerald-500/30 transition-all duration-300">
                  👨‍🎓
                </div>
                <h3 className="text-2xl font-bold mb-3 text-[#1A1A2E]">Student</h3>
                <p className="text-[#6B7280] text-sm mb-6 leading-relaxed">
                  Browse courses, join live classes, view materials, check attendance records, and more.
                </p>
                <div className="inline-flex items-center gap-2 text-blue-600 font-semibold group-hover:gap-3 transition-all text-sm">
                  Open Dashboard <span className="text-lg">→</span>
                </div>
              </div>
            </Link>

            {/* Admin Portal */}
            <Link href="/admin/login" className="group block">
              <div className="relative bg-white border-2 border-gray-100 rounded-2xl p-10 text-center hover:border-blue-300/40 hover:shadow-2xl transition-all duration-500 hover:scale-[1.03]">
                <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-amber-500 to-amber-400 flex items-center justify-center text-4xl shadow-lg shadow-amber-500/20 group-hover:shadow-xl group-hover:shadow-amber-500/30 transition-all duration-300">
                  🛡️
                </div>
                <h3 className="text-2xl font-bold mb-3 text-[#1A1A2E]">Administrator</h3>
                <p className="text-[#6B7280] text-sm mb-6 leading-relaxed">
                  Manage lecturers, students, system settings, and oversee the entire platform.
                </p>
                <div className="inline-flex items-center gap-2 text-blue-600 font-semibold group-hover:gap-3 transition-all text-sm">
                  Open Dashboard <span className="text-lg">→</span>
                </div>
              </div>
            </Link>
          </div>
        </div>
      </section>

      {/* Featured Courses Section */}
      <FeaturedCoursesSection />

      {/* About Section */}
      <section id="about" className="relative z-10 py-24 px-6 bg-[#FAFAFA]">
        <div className="max-w-4xl mx-auto">
          <div className="bg-white border border-gray-100 rounded-3xl p-12 md:p-16 shadow-sm">
            <div className="text-center">
              <div className="w-16 h-16 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-[#FF6B35] to-[#FFA07A] flex items-center justify-center text-2xl font-bold text-white shadow-lg shadow-[#FF6B35]/20">
                E
              </div>
              <h2 className="text-3xl font-extrabold mb-6 text-[#1A1A2E]">About EduSync AI</h2>
              <p className="text-[#6B7280] leading-relaxed mb-8 max-w-2xl mx-auto">
                EduSync AI is a comprehensive educational platform designed to bridge the gap
                between traditional classroom management and modern AI-powered solutions.
                Our platform enables lecturers to conduct live sessions, manage course materials,
                and track student attendance using cutting-edge facial recognition technology —
                all from a single, unified interface.
              </p>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mt-12">
                {[
                  { icon: '🎓', label: 'Real-time Classes' },
                  { icon: '🤖', label: 'AI Attendance' },
                  { icon: '🖊️', label: 'Whiteboards' },
                  { icon: '📱', label: 'Multi-Platform' },
                ].map((s, i) => (
                  <div key={i} className="text-center p-4 rounded-xl bg-[#FDF5EF] hover:bg-[#FFE4D6] transition-colors">
                    <p className="text-3xl mb-2">{s.icon}</p>
                    <p className="text-[#6B7280] text-xs font-medium">{s.label}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="relative z-10 bg-[#1A1A2E] text-white py-16 px-6">
        <div className="max-w-7xl mx-auto">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-12 mb-12">
            <div className="md:col-span-2">
              <div className="flex items-center gap-2 mb-4">
                <div className="w-9 h-9 rounded-lg bg-[#FF6B35] flex items-center justify-center text-white text-sm font-bold">
                  E
                </div>
                <span className="text-lg font-bold">EduSync AI</span>
              </div>
              <p className="text-white/50 text-sm leading-relaxed max-w-sm">
                Smart Education, Seamless Sync. A comprehensive platform for modern classroom management powered by artificial intelligence.
              </p>
            </div>
            <div>
              <h4 className="font-semibold mb-4 text-sm">Quick Links</h4>
              <div className="space-y-2">
                <a href="#features" className="block text-sm text-white/50 hover:text-white transition-colors">Features</a>
                <a href="#services" className="block text-sm text-white/50 hover:text-white transition-colors">Services</a>
                <a href="#portals" className="block text-sm text-white/50 hover:text-white transition-colors">Portals</a>
                <a href="#about" className="block text-sm text-white/50 hover:text-white transition-colors">About</a>
              </div>
            </div>
            <div>
              <h4 className="font-semibold mb-4 text-sm">Access</h4>
              <div className="space-y-2">
                <Link href="/login" className="block text-sm text-white/50 hover:text-white transition-colors">Lecturer Login</Link>
                <Link href="/student/login" className="block text-sm text-white/50 hover:text-white transition-colors">Student Login</Link>
                <Link href="/admin/login" className="block text-sm text-white/50 hover:text-white transition-colors">Admin Login</Link>
              </div>
            </div>
          </div>
          <div className="border-t border-white/10 pt-8 flex flex-col md:flex-row items-center justify-between gap-4">
            <p className="text-xs text-white/30">
              © {new Date().getFullYear()} EduSync AI. All rights reserved.
            </p>
            <div className="flex items-center gap-4">
              <a href="#" className="text-white/30 hover:text-white/60 transition-colors text-xs">Privacy Policy</a>
              <a href="#" className="text-white/30 hover:text-white/60 transition-colors text-xs">Terms of Service</a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}

// ═══════════════════════════════════════════
// Featured Courses Section
// ═══════════════════════════════════════════
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5152/api';

function FeaturedCoursesSection() {
    const [courses, setCourses] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        axios.get(`${API_BASE_URL}/CourseVideos/featured`)
            .then(res => setCourses(res.data || []))
            .catch(() => {})
            .finally(() => setLoading(false));
    }, []);

    if (loading) return null;
    if (courses.length === 0) return null;

    return (
        <section id="courses" className="relative z-10 py-24 px-6 bg-white">
            <div className="max-w-7xl mx-auto">
                <div className="text-center mb-16">
                    <div className="inline-flex items-center gap-2 px-4 py-2 bg-[#FFF3ED] rounded-full mb-6">
                        <span className="text-sm">🎓</span>
                        <span className="text-sm font-semibold text-[#FF6B35]">Featured Courses</span>
                    </div>
                    <h2 className="text-4xl font-extrabold text-[#1A1A2E] mb-4">Explore Our Top Courses</h2>
                    <p className="text-[#6B7280] max-w-2xl mx-auto">Handpicked courses from our best instructors, ready for you to start learning today.</p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                    {courses.map((course: any) => (
                        <Link
                            key={`${course.facultyName}-${course.courseName}`}
                            href={`/course-info/${encodeURIComponent(course.courseName)}`}
                            className="group block"
                        >
                            <div className="bg-white border-2 border-gray-100 rounded-2xl overflow-hidden hover:border-[#FF6B35]/30 hover:shadow-2xl transition-all duration-500 hover:scale-[1.02]">
                                {/* Thumbnail / Gradient */}
                                <div className="h-44 relative overflow-hidden">
                                    {course.thumbnailUrl ? (
                                        <img src={course.thumbnailUrl} alt={course.courseName} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
                                    ) : (
                                        <div className="w-full h-full bg-gradient-to-br from-[#FF6B35] via-[#FF8F5E] to-[#FFA07A] flex items-center justify-center">
                                            <span className="text-6xl opacity-30 group-hover:opacity-50 transition-opacity">📚</span>
                                        </div>
                                    )}
                                    <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
                                    <div className="absolute bottom-3 left-3 right-3 flex items-end justify-between">
                                        <span className="bg-black/50 backdrop-blur-sm text-white text-xs font-bold px-2.5 py-1 rounded-lg">
                                            📹 {course.videoCount} video{course.videoCount !== 1 ? 's' : ''}
                                        </span>
                                        {course.price > 0 && (
                                            <span className="bg-emerald-500 text-white text-xs font-bold px-3 py-1 rounded-lg shadow">
                                                ₦{course.price.toLocaleString()}
                                            </span>
                                        )}
                                    </div>
                                </div>
                                {/* Info */}
                                <div className="p-5">
                                    <h3 className="text-lg font-bold text-[#1A1A2E] group-hover:text-[#FF6B35] transition-colors mb-1">{course.courseName}</h3>
                                    <div className="flex items-center gap-2 mb-2">
                                        <span className="text-xs text-[#6B7280] bg-gray-100 px-2 py-0.5 rounded-full">{course.facultyName}</span>
                                        <span className="text-xs text-[#6B7280]">{course.departmentName}</span>
                                    </div>
                                    {course.description && (
                                        <p className="text-sm text-[#6B7280] line-clamp-2 leading-relaxed">{course.description}</p>
                                    )}
                                    <div className="mt-4 inline-flex items-center gap-1 text-[#FF6B35] font-semibold text-sm group-hover:gap-2 transition-all">
                                        View Course <span className="text-base">→</span>
                                    </div>
                                </div>
                            </div>
                        </Link>
                    ))}
                </div>
            </div>
        </section>
    );
}
