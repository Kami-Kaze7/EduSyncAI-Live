'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { courseApi } from '@/lib/api';
import { useState } from 'react';
import Link from 'next/link';
import { PlusIcon, AcademicCapIcon, ArrowLeftIcon } from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import type { Course } from '@/types';

export default function CoursesPage() {
    const [isCreating, setIsCreating] = useState(false);
    const [newCourse, setNewCourse] = useState({
        courseCode: '',
        courseName: '',
        description: '',
        creditHours: 3,
        lecturerId: 1, // TODO: Get from auth
    });

    const queryClient = useQueryClient();

    const { data: courses, isLoading } = useQuery({
        queryKey: ['courses'],
        queryFn: () => courseApi.getAll(),
    });

    const createMutation = useMutation({
        mutationFn: courseApi.create,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['courses'] });
            toast.success('Course created successfully!');
            setIsCreating(false);
            setNewCourse({
                courseCode: '',
                courseName: '',
                description: '',
                creditHours: 3,
                lecturerId: 1,
            });
        },
        onError: () => {
            toast.error('Failed to create course');
        },
    });

    const deleteMutation = useMutation({
        mutationFn: courseApi.delete,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['courses'] });
            toast.success('Course deleted successfully!');
        },
        onError: () => {
            toast.error('Failed to delete course');
        },
    });

    const handleCreate = (e: React.FormEvent) => {
        e.preventDefault();
        createMutation.mutate(newCourse);
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center space-x-4">
                            <Link href="/" className="text-gray-600 hover:text-gray-900">
                                <ArrowLeftIcon className="h-6 w-6" />
                            </Link>
                            <div className="flex items-center space-x-3">
                                <AcademicCapIcon className="h-8 w-8 text-indigo-600" />
                                <h1 className="text-2xl font-bold text-gray-900">Course Management</h1>
                            </div>
                        </div>
                        <button
                            onClick={() => setIsCreating(true)}
                            className="flex items-center space-x-2 bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 transition-colors"
                        >
                            <PlusIcon className="h-5 w-5" />
                            <span>New Course</span>
                        </button>
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                {/* Create Course Modal */}
                {isCreating && (
                    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                        <div className="bg-white rounded-xl p-8 max-w-2xl w-full mx-4">
                            <h2 className="text-2xl font-bold text-gray-900 mb-6">Create New Course</h2>
                            <form onSubmit={handleCreate} className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Course Code
                                    </label>
                                    <input
                                        type="text"
                                        required
                                        value={newCourse.courseCode}
                                        onChange={(e) => setNewCourse({ ...newCourse, courseCode: e.target.value })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                        placeholder="e.g., CS101"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Course Name
                                    </label>
                                    <input
                                        type="text"
                                        required
                                        value={newCourse.courseName}
                                        onChange={(e) => setNewCourse({ ...newCourse, courseName: e.target.value })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                        placeholder="e.g., Introduction to Programming"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Description
                                    </label>
                                    <textarea
                                        value={newCourse.description}
                                        onChange={(e) => setNewCourse({ ...newCourse, description: e.target.value })}
                                        rows={3}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                        placeholder="Course description..."
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Credit Hours
                                    </label>
                                    <input
                                        type="number"
                                        required
                                        min="1"
                                        max="6"
                                        value={newCourse.creditHours}
                                        onChange={(e) => setNewCourse({ ...newCourse, creditHours: parseInt(e.target.value) })}
                                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                    />
                                </div>
                                <div className="flex space-x-4 pt-4">
                                    <button
                                        type="submit"
                                        disabled={createMutation.isPending}
                                        className="flex-1 bg-indigo-600 text-white px-6 py-3 rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50"
                                    >
                                        {createMutation.isPending ? 'Creating...' : 'Create Course'}
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

                {/* Courses Grid */}
                {isLoading ? (
                    <div className="text-center py-12">
                        <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
                        <p className="mt-4 text-gray-600">Loading courses...</p>
                    </div>
                ) : courses && courses.length > 0 ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                        {courses.map((course: Course) => (
                            <div key={course.id} className="bg-white rounded-xl shadow-lg p-6 hover:shadow-xl transition-shadow">
                                <div className="flex items-start justify-between mb-4">
                                    <div>
                                        <h3 className="text-lg font-bold text-gray-900">{course.courseCode}</h3>
                                        <p className="text-sm text-gray-600">{course.creditHours} Credit Hours</p>
                                    </div>
                                    <button
                                        onClick={() => {
                                            if (confirm('Are you sure you want to delete this course?')) {
                                                deleteMutation.mutate(course.id);
                                            }
                                        }}
                                        className="text-red-600 hover:text-red-800 text-sm"
                                    >
                                        Delete
                                    </button>
                                </div>
                                <h4 className="text-xl font-semibold text-gray-800 mb-2">{course.courseName}</h4>
                                {course.description && (
                                    <p className="text-gray-600 text-sm mb-4 line-clamp-2">{course.description}</p>
                                )}
                                <div className="flex items-center justify-between pt-4 border-t border-gray-200">
                                    <span className="text-sm text-gray-500">
                                        Created {new Date(course.createdAt).toLocaleDateString()}
                                    </span>
                                    <Link
                                        href={`/courses/${course.id}`}
                                        className="text-indigo-600 hover:text-indigo-800 text-sm font-medium"
                                    >
                                        View Details →
                                    </Link>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="text-center py-12 bg-white rounded-xl shadow-lg">
                        <AcademicCapIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
                        <h3 className="text-xl font-semibold text-gray-900 mb-2">No courses yet</h3>
                        <p className="text-gray-600 mb-6">Get started by creating your first course</p>
                        <button
                            onClick={() => setIsCreating(true)}
                            className="inline-flex items-center space-x-2 bg-indigo-600 text-white px-6 py-3 rounded-lg hover:bg-indigo-700 transition-colors"
                        >
                            <PlusIcon className="h-5 w-5" />
                            <span>Create Course</span>
                        </button>
                    </div>
                )}
            </main>
        </div>
    );
}
