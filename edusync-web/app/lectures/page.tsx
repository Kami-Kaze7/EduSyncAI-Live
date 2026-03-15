'use client';

import Link from 'next/link';
import { DocumentTextIcon, ArrowLeftIcon } from '@heroicons/react/24/outline';

export default function LecturesIndexPage() {
    return (
        <div className="min-h-screen bg-gradient-to-br from-purple-50 to-pink-100">
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
                    <div className="flex items-center space-x-4">
                        <Link href="/" className="text-gray-600 hover:text-gray-900">
                            <ArrowLeftIcon className="h-6 w-6" />
                        </Link>
                        <div className="flex items-center space-x-3">
                            <DocumentTextIcon className="h-8 w-8 text-purple-600" />
                            <h1 className="text-2xl font-bold text-gray-900">Lecture Preparation</h1>
                        </div>
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
                <div className="text-center bg-white rounded-xl shadow-lg p-12">
                    <DocumentTextIcon className="h-16 w-16 text-purple-400 mx-auto mb-4" />
                    <h2 className="text-2xl font-bold text-gray-900 mb-4">Prepare Your Lectures</h2>
                    <p className="text-gray-600 mb-6 max-w-2xl mx-auto">
                        To prepare for a lecture, go to the Schedule page and click "Prepare Lecture" on any scheduled session.
                        You'll be able to add notes and upload materials for that specific lecture.
                    </p>
                    <Link
                        href="/schedule"
                        className="inline-flex items-center space-x-2 bg-purple-600 text-white px-6 py-3 rounded-lg hover:bg-purple-700 transition-colors"
                    >
                        <span>Go to Schedule</span>
                        <span>→</span>
                    </Link>
                </div>
            </main>
        </div>
    );
}
