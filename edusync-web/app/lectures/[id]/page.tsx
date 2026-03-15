'use client';
import { API_BASE_URL } from '@/lib/config';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sessionApi, materialsApi, attendanceApi } from '@/lib/api';
import { useState, use } from 'react';
import Link from 'next/link';
import { ArrowLeftIcon, DocumentTextIcon, CloudArrowUpIcon, TrashIcon } from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';

export default function LecturePreparationPage({ params }: { params: Promise<{ id: string }> }) {
    const resolvedParams = use(params);
    const sessionId = parseInt(resolvedParams.id);
    const [notes, setNotes] = useState('');
    const [selectedFile, setSelectedFile] = useState<File | null>(null);

    const queryClient = useQueryClient();

    const { data: session, isLoading: sessionLoading } = useQuery({
        queryKey: ['session', sessionId],
        queryFn: () => sessionApi.getById(sessionId),
    });

    const { data: materials, isLoading: materialsLoading } = useQuery({
        queryKey: ['materials', sessionId],
        queryFn: () => materialsApi.getBySession(sessionId),
    });

    const { data: attendance, isLoading: attendanceLoading } = useQuery({
        queryKey: ['attendance', sessionId],
        queryFn: () => attendanceApi.getBySession(sessionId),
    });

    const updateNotesMutation = useMutation({
        mutationFn: (content: string) => sessionApi.updateNotes(sessionId, content),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['session', sessionId] });
            toast.success('Notes saved successfully!');
        },
        onError: () => {
            toast.error('Failed to save notes');
        },
    });

    const uploadMutation = useMutation({
        mutationFn: (file: File) => materialsApi.upload(sessionId, file),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['materials', sessionId] });
            toast.success('Material uploaded successfully!');
            setSelectedFile(null);
        },
        onError: () => {
            toast.error('Failed to upload material');
        },
    });

    const deleteMutation = useMutation({
        mutationFn: materialsApi.delete,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['materials', sessionId] });
            toast.success('Material deleted successfully!');
        },
        onError: () => {
            toast.error('Failed to delete material');
        },
    });

    const handleSaveNotes = () => {
        updateNotesMutation.mutate(notes);
    };

    const handleFileUpload = () => {
        if (selectedFile) {
            uploadMutation.mutate(selectedFile);
        }
    };

    const formatFileSize = (bytes: number) => {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    };

    if (sessionLoading) {
        return (
            <div className="min-h-screen bg-gradient-to-br from-purple-50 to-pink-100 flex items-center justify-center">
                <div className="text-center">
                    <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
                    <p className="mt-4 text-gray-600">Loading lecture details...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gradient-to-br from-purple-50 to-pink-100">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
                    <div className="flex items-center space-x-4">
                        <Link href="/schedule" className="text-gray-600 hover:text-gray-900">
                            <ArrowLeftIcon className="h-6 w-6" />
                        </Link>
                        <div className="flex items-center space-x-3">
                            <DocumentTextIcon className="h-8 w-8 text-purple-600" />
                            <div>
                                <h1 className="text-2xl font-bold text-gray-900">Lecture Preparation</h1>
                                {session && (
                                    <p className="text-sm text-gray-600">
                                        {session.topic || 'Untitled Session'} - {new Date(session.scheduledDate).toLocaleDateString()}
                                    </p>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                    {/* Lecture Notes */}
                    <div className="bg-white rounded-xl shadow-lg p-6">
                        <h2 className="text-xl font-bold text-gray-900 mb-4">Lecture Notes</h2>
                        <textarea
                            value={notes || session?.notes?.content || ''}
                            onChange={(e) => setNotes(e.target.value)}
                            rows={15}
                            className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent resize-none"
                            placeholder="Write your lecture notes here..."
                        />
                        <button
                            onClick={handleSaveNotes}
                            disabled={updateNotesMutation.isPending}
                            className="mt-4 w-full bg-purple-600 text-white px-6 py-3 rounded-lg hover:bg-purple-700 transition-colors disabled:opacity-50"
                        >
                            {updateNotesMutation.isPending ? 'Saving...' : 'Save Notes'}
                        </button>
                    </div>

                    {/* Lecture Materials */}
                    <div className="bg-white rounded-xl shadow-lg p-6">
                        <h2 className="text-xl font-bold text-gray-900 mb-4">Lecture Materials</h2>

                        {/* Upload Section */}
                        <div className="mb-6 p-4 border-2 border-dashed border-gray-300 rounded-lg">
                            <input
                                type="file"
                                onChange={(e) => setSelectedFile(e.target.files?.[0] || null)}
                                className="w-full mb-3"
                            />
                            {selectedFile && (
                                <div className="mb-3 p-3 bg-gray-50 rounded-lg">
                                    <p className="text-sm text-gray-700">
                                        Selected: {selectedFile.name} ({formatFileSize(selectedFile.size)})
                                    </p>
                                </div>
                            )}
                            <button
                                onClick={handleFileUpload}
                                disabled={!selectedFile || uploadMutation.isPending}
                                className="w-full flex items-center justify-center space-x-2 bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700 transition-colors disabled:opacity-50"
                            >
                                <CloudArrowUpIcon className="h-5 w-5" />
                                <span>{uploadMutation.isPending ? 'Uploading...' : 'Upload Material'}</span>
                            </button>
                        </div>

                        {/* Materials List */}
                        <div className="space-y-3">
                            <h3 className="font-semibold text-gray-900">Uploaded Materials</h3>
                            {materialsLoading ? (
                                <p className="text-gray-600 text-sm">Loading materials...</p>
                            ) : materials && materials.length > 0 ? (
                                materials.map((material) => (
                                    <div key={material.id} className="flex flex-col p-3 bg-gray-50 rounded-lg border border-gray-100">
                                        <div className="flex items-center justify-between mb-2">
                                            <div className="flex-1">
                                                <p className="font-medium text-gray-900">{material.fileName}</p>
                                                <p className="text-xs text-gray-600">
                                                    {formatFileSize(material.fileSize)} • {material.fileType} •
                                                    {new Date(material.uploadedAt).toLocaleDateString()}
                                                </p>
                                            </div>
                                            <div className="flex items-center space-x-2">
                                                <a
                                                    href={`${API_BASE_URL}/materials/${material.id}/download`}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    className="text-purple-600 hover:text-purple-800"
                                                    title="Download"
                                                >
                                                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a2 2 0 002 2h12a2 2 0 002-2v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                                                    </svg>
                                                </a>
                                                <button
                                                    onClick={() => {
                                                        if (confirm('Are you sure you want to delete this material?')) {
                                                            deleteMutation.mutate(material.id);
                                                        }
                                                    }}
                                                    className="text-red-600 hover:text-red-800"
                                                    title="Delete"
                                                >
                                                    <TrashIcon className="h-5 w-5" />
                                                </button>
                                            </div>
                                        </div>
                                        {material.fileType.match(/\.(jpg|jpeg|png|gif|webp)$/i) && (
                                            <div className="aspect-video relative bg-white rounded-md border border-gray-200 overflow-hidden flex items-center justify-center">
                                                <img
                                                    src={`${API_BASE_URL}/materials/${material.id}/download`}
                                                    alt={material.fileName}
                                                    className="max-h-full max-w-full object-contain"
                                                />
                                            </div>
                                        )}
                                    </div>
                                ))
                            ) : (
                                <p className="text-gray-600 text-sm">No materials uploaded yet</p>
                            )}
                        </div>
                    </div>
                </div>

                {/* Attendance List Section */}
                <div className="mt-8">
                    <div className="bg-white rounded-xl shadow-lg p-6">
                        <div className="flex justify-between items-center mb-6">
                            <h2 className="text-xl font-bold text-gray-900">Attendance List</h2>
                            <span className="bg-purple-100 text-purple-700 px-3 py-1 rounded-full text-sm font-semibold">
                                {attendanceLoading ? 'Checking...' : attendance ? `${attendance.length} Student${attendance.length !== 1 ? 's' : ''} Present` : '0 Students Present'}
                            </span>
                        </div>

                        {attendanceLoading ? (
                            <div className="flex justify-center py-10">
                                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600"></div>
                            </div>
                        ) : attendance && attendance.length > 0 ? (
                            <div className="overflow-x-auto">
                                <table className="min-w-full divide-y divide-gray-200">
                                    <thead className="bg-gray-50">
                                        <tr>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Matric Number</th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Student Name</th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Check-in Time</th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Method</th>
                                        </tr>
                                    </thead>
                                    <tbody className="bg-white divide-y divide-gray-200">
                                        {attendance.map((record: any) => (
                                            <tr key={record.id} className="hover:bg-gray-50 transition-colors">
                                                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                                    {record.matricNumber}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.studentName}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 font-medium">
                                                    {new Date(record.checkInTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap">
                                                    <span className={`px-2 py-1 text-xs font-semibold rounded-full ${record.checkInMethod === 'Fingerprint'
                                                            ? 'bg-blue-100 text-blue-800'
                                                            : 'bg-indigo-100 text-indigo-800'
                                                        }`}>
                                                        {record.checkInMethod}
                                                    </span>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : (
                            <div className="text-center py-12 bg-gray-50 rounded-lg">
                                <p className="text-gray-500 italic">No attendance records found for this session.</p>
                                <p className="text-xs text-gray-400 mt-2">Attendance is synced automatically when the session ends in the desktop app.</p>
                            </div>
                        )}
                    </div>
                </div>
            </main>
        </div>
    );
}
