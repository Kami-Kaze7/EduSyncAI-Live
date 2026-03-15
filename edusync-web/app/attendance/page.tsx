'use client';

import { useQuery } from '@tanstack/react-query';
import { sessionApi, attendanceApi } from '@/lib/api';
import { useState } from 'react';
import Link from 'next/link';
import { ArrowLeftIcon, ClipboardDocumentCheckIcon, UserGroupIcon, CalendarIcon } from '@heroicons/react/24/outline';

import { useAuthStore } from '@/lib/store';

export default function AttendanceOverviewPage() {
    const { lecturer } = useAuthStore();
    const { data: sessions, isLoading } = useQuery({
        queryKey: ['all-sessions', lecturer?.id],
        queryFn: () => sessionApi.getAll({ lecturerId: lecturer?.id }),
        enabled: !!lecturer?.id,
    });

    if (isLoading) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
                    <div className="flex items-center space-x-4">
                        <Link href="/" className="text-gray-600 hover:text-gray-900">
                            <ArrowLeftIcon className="h-6 w-6" />
                        </Link>
                        <div className="flex items-center space-x-3">
                            <ClipboardDocumentCheckIcon className="h-8 w-8 text-indigo-600" />
                            <h1 className="text-2xl font-bold text-gray-900">Attendance Records</h1>
                        </div>
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
                    <div className="p-6 border-b border-gray-100">
                        <h2 className="text-xl font-semibold text-gray-900">All Session Attendance</h2>
                        <p className="text-sm text-gray-500 mt-1">View detailed attendance lists for all your recorded sessions.</p>
                    </div>

                    <div className="overflow-x-auto">
                        <table className="min-w-full divide-y divide-gray-200">
                            <thead className="bg-gray-50">
                                <tr>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Course</th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Topic</th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Present</th>
                                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
                                </tr>
                            </thead>
                            <tbody className="bg-white divide-y divide-gray-200">
                                {sessions && sessions.length > 0 ? (
                                    sessions.map((session) => (
                                        <tr key={session.id} className="hover:bg-gray-50 transition-colors">
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                <div className="text-sm font-bold text-gray-900">{session.course?.courseCode}</div>
                                                <div className="text-xs text-gray-500 font-medium">
                                                    {session.course?.courseName || session.course?.courseTitle || 'Unknown Course'}
                                                </div>
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                <div className="text-sm text-gray-900">{session.topic || 'Untitled Session'}</div>
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                <div className="flex items-center text-sm text-gray-500">
                                                    <CalendarIcon className="h-4 w-4 mr-1.5" />
                                                    {new Date(session.scheduledDate).toLocaleDateString()}
                                                </div>
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                <div className="flex items-center text-sm font-semibold text-indigo-600">
                                                    <UserGroupIcon className="h-4 w-4 mr-1.5" />
                                                    {session.attendanceCount || 0}
                                                </div>
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                                <Link
                                                    href={`/lectures/${session.id}`}
                                                    className="text-indigo-600 hover:text-indigo-900 bg-indigo-50 px-3 py-1.5 rounded-md transition-colors"
                                                >
                                                    View List
                                                </Link>
                                            </td>
                                        </tr>
                                    ))
                                ) : (
                                    <tr>
                                        <td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                                            No sessions found. Start a session in the desktop app to record attendance.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </main>
        </div>
    );
}
