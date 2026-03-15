'use client';
import { API_BASE_URL, API_SERVER_URL } from '@/lib/config';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { ArrowLeftIcon, PhotoIcon, ArrowDownTrayIcon, ArrowPathIcon } from '@heroicons/react/24/outline';
import { materialsApi } from '@/lib/api';
import { useAuthStore } from '@/lib/store';
import toast from 'react-hot-toast';

export default function LecturerWhiteboardsPage() {
    const router = useRouter();
    const { lecturer } = useAuthStore();
    const [whiteboards, setWhiteboards] = useState<any[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const fetchWhiteboards = async () => {
        if (!lecturer?.id) return;
        setIsLoading(true);
        try {
            const data = await materialsApi.getByLecturer(lecturer.id);
            // Filter only images/whiteboards if needed, but the endpoint should mostly return those
            setWhiteboards(data || []);
        } catch (error) {
            console.error("Failed to fetch whiteboards:", error);
            toast.error("Failed to load whiteboards");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        if (!lecturer) {
            router.push('/login');
            return;
        }
        fetchWhiteboards();
    }, [lecturer, router]);

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm sticky top-0 z-10">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex items-center justify-between">
                    <div className="flex items-center space-x-4">
                        <Link href="/" className="p-2 hover:bg-gray-100 rounded-full transition-colors text-gray-600">
                            <ArrowLeftIcon className="h-6 w-6" />
                        </Link>
                        <div>
                            <h1 className="text-2xl font-bold text-gray-900">Saved Whiteboards</h1>
                            <p className="text-sm text-gray-600">All drawings captured during your sessions</p>
                        </div>
                    </div>
                    <button
                        onClick={fetchWhiteboards}
                        className="p-2 text-indigo-600 hover:bg-indigo-50 rounded-lg transition-all flex items-center font-medium"
                    >
                        <ArrowPathIcon className={`h-5 w-5 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
                        Sync Drawings
                    </button>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
                {isLoading ? (
                    <div className="flex flex-col items-center justify-center py-24">
                        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mb-4"></div>
                        <p className="text-gray-500 font-medium">Loading your drawings...</p>
                    </div>
                ) : whiteboards.length > 0 ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                        {whiteboards.map((wb: any) => (
                            <div key={wb.id} className="group bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden hover:shadow-xl transition-all duration-300 transform hover:-translate-y-1">
                                <div className="aspect-video relative bg-slate-100 flex items-center justify-center overflow-hidden border-b border-gray-100">
                                    {wb.fileType?.match(/\.(mp4|webm|avi|mov)$/i) ? (
                                        <video
                                            controls
                                            preload="metadata"
                                            className="w-full h-full object-contain bg-black"
                                            src={`${API_BASE_URL}/materials/${wb.id}/download`}
                                        >
                                            Your browser does not support the video tag.
                                        </video>
                                    ) : (
                                        <img
                                            src={`${API_BASE_URL}/materials/${wb.id}/download`}
                                            alt={wb.fileName}
                                            className="max-h-full max-w-full object-contain p-1 group-hover:scale-105 transition-transform duration-500 bg-white"
                                            onError={(e: any) => {
                                                console.error("Image load failed for material ID:", wb.id);
                                                e.target.src = 'https://via.placeholder.com/400x225?text=Drawing+Load+Error';
                                            }}
                                        />
                                    )}
                                    <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-5 transition-all duration-300 pointer-events-none" />
                                </div>
                                <div className="p-5">
                                    <div className="flex justify-between items-start mb-3">
                                        <div className="flex-1 min-w-0 pr-4">
                                            <div className="flex items-center gap-2">
                                                {wb.fileType?.match(/\.(mp4|webm|avi|mov)$/i) && (
                                                    <span className="px-2 py-0.5 bg-red-100 text-red-700 text-[10px] font-bold rounded-full flex-shrink-0">🎬 REC</span>
                                                )}
                                                <h3 className="text-base font-bold text-gray-900 truncate" title={wb.fileName}>{wb.fileName}</h3>
                                            </div>
                                            <p className="text-xs font-semibold text-indigo-600 uppercase tracking-wider mt-1">{wb.courseCode} • {wb.courseName}</p>
                                        </div>
                                        <a
                                            href={`${API_BASE_URL}/materials/${wb.id}/download`}
                                            download={wb.fileName}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="p-2.5 bg-indigo-50 text-indigo-600 rounded-xl hover:bg-indigo-600 hover:text-white transition-all shadow-sm"
                                            title="Download"
                                        >
                                            <ArrowDownTrayIcon className="h-5 w-5" />
                                        </a>
                                    </div>
                                    <div className="flex items-center text-xs text-gray-500 mt-4 pt-4 border-t border-gray-50">
                                        <div className="flex items-center">
                                            <PhotoIcon className="h-3.5 w-3.5 mr-1" />
                                            <span>Session: {wb.sessionDate ? new Date(wb.sessionDate).toLocaleDateString() : 'N/A'}</span>
                                        </div>
                                        <span className="mx-2">•</span>
                                        <span>{wb.fileSize > 1024 * 1024 ? `${(wb.fileSize / (1024 * 1024)).toFixed(1)} MB` : `${(wb.fileSize / 1024).toFixed(1)} KB`}</span>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-20 text-center max-w-2xl mx-auto">
                        <div className="bg-indigo-50 h-24 w-24 rounded-full flex items-center justify-center mx-auto mb-6">
                            <PhotoIcon className="h-12 w-12 text-indigo-400" />
                        </div>
                        <h3 className="text-2xl font-bold text-gray-900 mb-2">No Whiteboards Saved</h3>
                        <p className="text-gray-500">
                            Drawings saved through the EduSync AI Desktop application during your lectures will appear here automatically.
                        </p>
                        <div className="mt-8">
                            <Link
                                href="/schedule"
                                className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-xl text-white bg-indigo-600 hover:bg-indigo-700 shadow-md transition-all active:scale-95"
                            >
                                View My Schedule
                            </Link>
                        </div>
                    </div>
                )}
            </main>
        </div>
    );
}
