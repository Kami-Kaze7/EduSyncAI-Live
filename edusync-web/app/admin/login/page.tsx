'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { adminApi } from '@/lib/adminApi';
import toast from 'react-hot-toast';
import Link from 'next/link';

export default function AdminLogin() {
    const router = useRouter();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);

        try {
            const response = await adminApi.login(username, password);
            localStorage.setItem('adminToken', response.token);
            localStorage.setItem('adminUser', JSON.stringify(response.user));
            toast.success('Login successful!');
            router.push('/admin/dashboard');
        } catch (error: any) {
            toast.error(error.response?.data?.error || 'Login failed');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex">
            {/* Left — Form Side */}
            <div className="flex-1 flex items-center justify-center px-6 py-12 bg-white">
                <div className="w-full max-w-md">
                    {/* Logo */}
                    <div className="mb-10">
                        <Link href="/" className="inline-flex items-center gap-2 group">
                            <div className="w-9 h-9 rounded-lg bg-[#FF6B35] flex items-center justify-center text-white text-sm font-bold shadow-md">
                                E
                            </div>
                            <span className="text-lg font-bold text-[#1A1A2E] group-hover:text-[#FF6B35] transition-colors">EduSync AI</span>
                        </Link>
                    </div>

                    {/* Heading */}
                    <h1 className="text-3xl font-extrabold text-[#1A1A2E] mb-2">Admin Sign In</h1>
                    <p className="text-[#9CA3AF] mb-8">Access the admin panel to manage users &amp; system settings.</p>

                    {/* Form */}
                    <form onSubmit={handleSubmit} className="space-y-5">
                        <div>
                            <label className="block text-sm font-medium text-[#374151] mb-1.5">
                                Username
                            </label>
                            <input
                                type="text"
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                className="w-full px-4 py-3 bg-[#F9FAFB] border border-gray-200 rounded-xl focus:ring-2 focus:ring-[#FF6B35]/30 focus:border-[#FF6B35] transition-all outline-none text-[#1A1A2E] placeholder-[#9CA3AF]"
                                placeholder="Enter username"
                                required
                                disabled={loading}
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-[#374151] mb-1.5">
                                Password
                            </label>
                            <input
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                className="w-full px-4 py-3 bg-[#F9FAFB] border border-gray-200 rounded-xl focus:ring-2 focus:ring-[#FF6B35]/30 focus:border-[#FF6B35] transition-all outline-none text-[#1A1A2E] placeholder-[#9CA3AF]"
                                placeholder="Enter password"
                                required
                                disabled={loading}
                            />
                        </div>

                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full bg-gradient-to-r from-[#FF6B35] to-[#FFA07A] text-white py-3.5 px-4 rounded-xl hover:shadow-lg hover:shadow-[#FF6B35]/25 focus:outline-none focus:ring-2 focus:ring-[#FF6B35]/50 transition-all disabled:opacity-50 disabled:cursor-not-allowed font-semibold text-sm"
                        >
                            {loading ? (
                                <span className="flex items-center justify-center">
                                    <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Signing in...
                                </span>
                            ) : (
                                'SIGN IN'
                            )}
                        </button>
                    </form>

                    {/* Default credentials */}
                    <div className="mt-6 p-4 bg-[#FFF3E0] rounded-xl border border-[#FFE4D6]">
                        <p className="text-sm text-[#6B7280]">
                            <span className="font-semibold text-[#1A1A2E]">Default credentials:</span> admin / admin123
                        </p>
                    </div>

                    {/* Footer link */}
                    <p className="text-center text-sm text-[#9CA3AF] mt-8">
                        Not an admin?{' '}
                        <Link href="/login" className="text-[#FF6B35] font-semibold hover:underline">Lecturer Login</Link>
                        {' '}or{' '}
                        <Link href="/student/login" className="text-[#FF6B35] font-semibold hover:underline">Student Login</Link>
                    </p>
                </div>
            </div>

            {/* Right — Illustration Side */}
            <div className="hidden lg:flex flex-1 bg-gradient-to-br from-amber-500 to-[#FF6B35] items-center justify-center relative overflow-hidden">
                {/* Decorative shapes */}
                <div className="absolute top-24 left-16 w-20 h-20 bg-white/10 rounded-full" />
                <div className="absolute bottom-20 right-12 w-28 h-28 bg-white/10 rounded-full" />
                <div className="absolute top-1/3 right-16 w-12 h-12 bg-white/15 rounded-lg rotate-45" />
                <div className="absolute bottom-32 left-10 w-16 h-16 bg-white/10 rounded-lg rotate-12" />

                <div className="text-center relative z-10 px-12">
                    <div className="text-8xl mb-8">🛡️</div>
                    <h2 className="text-3xl font-extrabold text-white mb-4">Admin Panel</h2>
                    <p className="text-white/80 text-sm leading-relaxed max-w-sm mx-auto">
                        Manage lecturers, students, system settings, and oversee the entire EduSync AI platform.
                    </p>
                    <div className="flex items-center justify-center gap-6 mt-10">
                        {['👥 Users', '⚙️ Settings', '📊 Reports'].map((item, i) => (
                            <div key={i} className="bg-white/15 backdrop-blur-sm rounded-xl px-4 py-2.5 text-white text-xs font-medium">
                                {item}
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}
