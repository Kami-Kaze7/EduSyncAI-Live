'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sessionApi, courseApi } from '@/lib/api';
import { useState } from 'react';
import Link from 'next/link';
import { PlusIcon, CalendarIcon, ArrowLeftIcon } from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import type { ClassSession, Course } from '@/types';

export default function SchedulePage() {
    const [isCreating, setIsCreating] = useState(false);
    const [newSession, setNewSession] = useState({
        courseId: 0,
        scheduledDate: '',
        topic: '',
        location: '',
        durationMinutes: 60,
        status: 'Scheduled' as const,
    });

    const queryClient = useQueryClient();

    const { data: sessions, isLoading } = useQuery({
        queryKey: ['sessions'],
        queryFn: () => sessionApi.getAll(),
    });

    const { data: courses } = useQuery({
        queryKey: ['courses'],
        queryFn: () => courseApi.getAll(),
    });

    const createMutation = useMutation({
        mutationFn: sessionApi.create,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['sessions'] });
            toast.success('Session scheduled successfully!');
            setIsCreating(false);
            setNewSession({
                courseId: 0,
                scheduledDate: '',
                topic: '',
                location: '',
                durationMinutes: 60,
                status: 'Scheduled',
            });
        },
        onError: () => {
            toast.error('Failed to create session');
        },
    });

    const deleteMutation = useMutation({
        mutationFn: sessionApi.delete,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['sessions'] });
            toast.success('Session deleted successfully!');
        },
        onError: () => {
            toast.error('Failed to delete session');
        },
    });

    const handleCreate = (e: React.FormEvent) => {
        e.preventDefault();
        if (newSession.courseId === 0) {
            toast.error('Please select a course');
            return;
        }
        createMutation.mutate(newSession);
    };

    const getCourseName = (courseId: number) => {
        const course = courses?.find((c: Course) => c.id === courseId);
        return course ? `${course.courseCode} - ${course.courseName}` : 'Unknown Course';
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleString('en-US', {
            weekday: 'short',
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-green-50 to-emerald-100">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center space-x-4">
                            <Link href="/" className="text-gray-600 hover:text-gray-900">
                                <ArrowLeftIcon className="h-6 w-6" />
                            </Link>
                            <div className="flex items-center space-x-3">
                                <CalendarIcon className="h-8 w-8 text-green-600" />
                                <h1 className="text-2xl font-bold text-gray-900">Lecture Schedule</h1>
                            </div>
                        </div>
                        <button
                            onClick={() => setIsCreating(true)}
                            className="flex items-center space-x-2 bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 transition-colors"
                        >
                            <PlusIcon className="h-5 w-5" />
                            <span>New Session</span>
                        </button>
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                {/* Create Session Modal */}
                {isCreating && (
                    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                        <div className="bg-white rounded-xl p-8 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
                            <h2 className="text-2xl font-bold text-gray-900 mb-6">Schedule New Session</h2>
                            <form onSubmit={handleCreate} className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Course *
                                    </label>
                                    <select
                                        required
                                        value={newSession.courseId}
                                        onChange={(e) => setNewSession({ ...newSession, courseId: parseInt(e.target.value) })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                                    >
                                        <option value={0}>Select a course...</option>
                                        {courses?.map((course: Course) => (
                                            <option key={course.id} value={course.id}>
                                                {course.courseCode} - {course.courseName}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Date & Time *
                                    </label>
                                    <input
                                        type="datetime-local"
                                        required
                                        value={newSession.scheduledDate}
                                        onChange={(e) => setNewSession({ ...newSession, scheduledDate: e.target.value })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Topic
                                    </label>
                                    <input
                                        type="text"
                                        value={newSession.topic}
                                        onChange={(e) => setNewSession({ ...newSession, topic: e.target.value })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                                        placeholder="e.g., Introduction to Variables"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Location
                                    </label>
                                    <input
                                        type="text"
                                        value={newSession.location}
                                        onChange={(e) => setNewSession({ ...newSession, location: e.target.value })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                                        placeholder="e.g., Room 101, Building A"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Duration (minutes)
                                    </label>
                                    <input
                                        type="number"
                                        required
                                        min="15"
                                        max="240"
                                        step="15"
                                        value={newSession.durationMinutes}
                                        onChange={(e) => setNewSession({ ...newSession, durationMinutes: parseInt(e.target.value) })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                                    />
                                </div>
                                <div className="flex space-x-4 pt-4">
                                    <button
                                        type="submit"
                                        disabled={createMutation.isPending}
                                        className="flex-1 bg-green-600 text-white px-6 py-3 rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50"
                                    >
                                        {createMutation.isPending ? 'Scheduling...' : 'Schedule Session'}
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setIsCreating(false)}
                                        className="flex-1 bg-gray-200 text-gray-700 px-6 py-3 rounded-lg hover:bg-gray-300 transition-colors"
                                    >
                                        Cancel
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                )}

                {/* Sessions List */}
                {isLoading ? (
                    <div className="text-center py-12">
                        <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-green-600"></div>
                        <p className="mt-4 text-gray-600">Loading schedule...</p>
                    </div>
                ) : sessions && sessions.length > 0 ? (
                    <div className="space-y-4">
                        {sessions.map((session: ClassSession) => (
                            <div key={session.id} className="bg-white rounded-xl shadow-lg p-6 hover:shadow-xl transition-shadow">
                                <div className="flex items-start justify-between">
                                    <div className="flex-1">
                                        <div className="flex items-center space-x-3 mb-2">
                                            <span className={`px-3 py-1 rounded-full text-sm font-medium ${session.status === 'Scheduled' ? 'bg-blue-100 text-blue-800' :
                                                    session.status === 'Completed' ? 'bg-green-100 text-green-800' :
                                                        'bg-red-100 text-red-800'
                                                }`}>
                                                {session.status}
                                            </span>
                                            <span className="text-sm text-gray-600">{session.durationMinutes} minutes</span>
                                        </div>
                                        <h3 className="text-xl font-bold text-gray-900 mb-2">
                                            {getCourseName(session.courseId)}
                                        </h3>
                                        {session.topic && (
                                            <p className="text-gray-700 mb-2">Topic: {session.topic}</p>
                                        )}
                                        <div className="flex items-center space-x-4 text-sm text-gray-600">
                                            <span>📅 {formatDate(session.scheduledDate)}</span>
                                            {session.location && <span>📍 {session.location}</span>}
                                        </div>
                                    </div>
                                    <div className="flex flex-col space-y-2">
                                        <Link
                                            href={`/lectures/${session.id}`}
                                            className="text-green-600 hover:text-green-800 text-sm font-medium"
                                        >
                                            Prepare Lecture →
                                        </Link>
                                        <button
                                            onClick={() => {
                                                if (confirm('Are you sure you want to delete this session?')) {
                                                    deleteMutation.mutate(session.id);
                                                }
                                            }}
                                            className="text-red-600 hover:text-red-800 text-sm"
                                        >
                                            Delete
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="text-center py-12 bg-white rounded-xl shadow-lg">
                        <CalendarIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
                        <h3 className="text-xl font-semibold text-gray-900 mb-2">No sessions scheduled</h3>
                        <p className="text-gray-600 mb-6">Get started by scheduling your first lecture session</p>
                        <button
                            onClick={() => setIsCreating(true)}
                            className="inline-flex items-center space-x-2 bg-green-600 text-white px-6 py-3 rounded-lg hover:bg-green-700 transition-colors"
                        >
                            <PlusIcon className="h-5 w-5" />
                            <span>Schedule Session</span>
                        </button>
                    </div>
                )}
            </main>
        </div>
    );
}
