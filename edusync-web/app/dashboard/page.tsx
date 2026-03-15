'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { AcademicCapIcon, CalendarIcon, DocumentTextIcon, ArrowRightOnRectangleIcon, PhotoIcon, ClipboardDocumentCheckIcon } from '@heroicons/react/24/outline';
import AuthGuard from '@/components/AuthGuard';
import { useAuthStore } from '@/lib/store';
import toast from 'react-hot-toast';

export default function Home() {
  const router = useRouter();
  const { lecturer, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    toast.success('Logged out successfully');
    router.push('/login');
  };
  return (
    <AuthGuard>
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
        {/* Header */}
        <header className="bg-white shadow-sm">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <AcademicCapIcon className="h-8 w-8 text-indigo-600" />
                <div>
                  <h1 className="text-2xl font-bold text-gray-900">EduSync AI</h1>
                  {lecturer && (
                    <p className="text-sm text-gray-600">Welcome, {lecturer.fullName}</p>
                  )}
                </div>
              </div>
              <button
                onClick={handleLogout}
                className="flex items-center space-x-2 text-gray-600 hover:text-gray-900 transition-colors"
              >
                <ArrowRightOnRectangleIcon className="h-5 w-5" />
                <span>Logout</span>
              </button>
            </div>
          </div>
        </header>

        {/* Main Content */}
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center mb-12">
            <h2 className="text-4xl font-bold text-gray-900 mb-4">
              Welcome to Your Dashboard
            </h2>
            <p className="text-lg text-gray-600">
              Manage your courses, schedules, and lecture materials all in one place
            </p>
          </div>

          {/* Feature Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
            {/* Course Management */}
            <Link href="/courses">
              <div className="bg-white rounded-xl shadow-lg p-8 hover:shadow-xl transition-shadow cursor-pointer border-2 border-transparent hover:border-indigo-500">
                <div className="flex items-center justify-center w-16 h-16 bg-indigo-100 rounded-full mb-6 mx-auto">
                  <AcademicCapIcon className="h-8 w-8 text-indigo-600" />
                </div>
                <h3 className="text-xl font-semibold text-gray-900 text-center mb-3">
                  Course Management
                </h3>
                <p className="text-gray-600 text-center">
                  Create, edit, and manage your courses. Enroll students and track progress.
                </p>
              </div>
            </Link>

            {/* Lecture Schedule */}
            <Link href="/schedule">
              <div className="bg-white rounded-xl shadow-lg p-8 hover:shadow-xl transition-shadow cursor-pointer border-2 border-transparent hover:border-green-500">
                <div className="flex items-center justify-center w-16 h-16 bg-green-100 rounded-full mb-6 mx-auto">
                  <CalendarIcon className="h-8 w-8 text-green-600" />
                </div>
                <h3 className="text-xl font-semibold text-gray-900 text-center mb-3">
                  Lecture Schedule
                </h3>
                <p className="text-gray-600 text-center">
                  View and manage your lecture schedule. Create new sessions and track attendance.
                </p>
              </div>
            </Link>

            {/* Lecture Preparation */}
            <Link href="/lectures">
              <div className="bg-white rounded-xl shadow-lg p-8 hover:shadow-xl transition-shadow cursor-pointer border-2 border-transparent hover:border-purple-500 h-full">
                <div className="flex items-center justify-center w-16 h-16 bg-purple-100 rounded-full mb-6 mx-auto">
                  <DocumentTextIcon className="h-8 w-8 text-purple-600" />
                </div>
                <h3 className="text-xl font-semibold text-gray-900 text-center mb-3">
                  Lecture Preparation
                </h3>
                <p className="text-gray-600 text-center">
                  Upload materials, create notes, and prepare for your lectures.
                </p>
              </div>
            </Link>

            {/* Saved Whiteboards Gallery */}
            <Link href="/whiteboards">
              <div className="bg-white rounded-xl shadow-lg p-8 hover:shadow-xl transition-shadow cursor-pointer border-2 border-transparent hover:border-blue-500 h-full">
                <div className="flex items-center justify-center w-16 h-16 bg-blue-100 rounded-full mb-6 mx-auto">
                  <PhotoIcon className="h-8 w-8 text-blue-600" />
                </div>
                <h3 className="text-xl font-semibold text-gray-900 text-center mb-3">
                  Whiteboards Gallery
                </h3>
                <p className="text-gray-600 text-center">
                  View and manage all saved whiteboard drawings from your sessions.
                </p>
              </div>
            </Link>

            {/* Attendance Records */}
            <Link href="/attendance">
              <div className="bg-white rounded-xl shadow-lg p-8 hover:shadow-xl transition-shadow cursor-pointer border-2 border-transparent hover:border-orange-500 h-full">
                <div className="flex items-center justify-center w-16 h-16 bg-orange-100 rounded-full mb-6 mx-auto">
                  <ClipboardDocumentCheckIcon className="h-8 w-8 text-orange-600" />
                </div>
                <h3 className="text-xl font-semibold text-gray-900 text-center mb-3">
                  Attendance Records
                </h3>
                <p className="text-gray-600 text-center">
                  View and verify student attendance across all your course sessions.
                </p>
              </div>
            </Link>
          </div>

          {/* Quick Stats */}
          <div className="mt-16 bg-white rounded-xl shadow-lg p-8">
            <h3 className="text-2xl font-bold text-gray-900 mb-6">Quick Stats</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="text-center p-6 bg-indigo-50 rounded-lg">
                <p className="text-4xl font-bold text-indigo-600 mb-2">-</p>
                <p className="text-gray-600">Total Courses</p>
              </div>
              <div className="text-center p-6 bg-green-50 rounded-lg">
                <p className="text-4xl font-bold text-green-600 mb-2">-</p>
                <p className="text-gray-600">Upcoming Sessions</p>
              </div>
              <div className="text-center p-6 bg-purple-50 rounded-lg">
                <p className="text-4xl font-bold text-purple-600 mb-2">-</p>
                <p className="text-gray-600">Total Students</p>
              </div>
            </div>
          </div>
        </main>
      </div>
    </AuthGuard>
  );
}
