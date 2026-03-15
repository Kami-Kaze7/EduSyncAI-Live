'use client';
import { API_BASE_URL, API_SERVER_URL } from '@/lib/config';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { studentApi } from '@/lib/studentApi';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

export default function StudentDashboard() {
    const router = useRouter();
    const [activeTab, setActiveTab] = useState<'courses' | 'summaries' | 'profile' | 'whiteboards' | 'attendance'>('courses');
    const [allWhiteboards, setAllWhiteboards] = useState<any[]>([]);
    const [isFetchingAllWhiteboards, setIsFetchingAllWhiteboards] = useState(false);
    const [attendanceRecords, setAttendanceRecords] = useState<any[]>([]);
    const [isFetchingAttendance, setIsFetchingAttendance] = useState(false);
    const [selectedSummary, setSelectedSummary] = useState<any>(null);
    const [showSummaryView, setShowSummaryView] = useState(false);
    const [chatMessages, setChatMessages] = useState<{ role: 'user' | 'assistant'; content: string }[]>([]);
    const [userQuestion, setUserQuestion] = useState('');
    const [isAskingAI, setIsAskingAI] = useState(false);
    const [materials, setMaterials] = useState<any[]>([]);
    const [isFetchingMaterials, setIsFetchingMaterials] = useState(false);
    const [liveStreams, setLiveStreams] = useState<any[]>([]);

    const handleAskAI = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!userQuestion.trim() || isAskingAI) return;

        const question = userQuestion.trim();
        setUserQuestion('');
        setChatMessages(prev => [...prev, { role: 'user', content: question }]);
        setIsAskingAI(true);

        try {
            const data = await studentApi.askAI(selectedSummary?.id || null, question);
            setChatMessages(prev => [...prev, { role: 'assistant', content: data.response }]);
        } catch (error) {
            toast.error('Failed to get response from AI Assistant');
            setChatMessages(prev => [...prev, { role: 'assistant', content: "I'm sorry, I encountered an error. Please try again." }]);
        } finally {
            setIsAskingAI(false);
        }
    };

    useEffect(() => {
        const token = localStorage.getItem('studentToken');
        if (!token) {
            router.push('/student/login');
        }
    }, [router]);

    useEffect(() => {
        const fetchMaterials = async () => {
            if (selectedSummary?.sessionId) {
                setIsFetchingMaterials(true);
                try {
                    const data = await studentApi.getSessionMaterials(selectedSummary.sessionId);
                    setMaterials(data || []);
                } catch (error) {
                    console.error("Failed to fetch materials:", error);
                } finally {
                    setIsFetchingMaterials(false);
                }
            } else {
                setMaterials([]);
            }
        };

        if (showSummaryView && selectedSummary) {
            fetchMaterials();
        }
    }, [showSummaryView, selectedSummary]);

    useEffect(() => {
        const fetchAllWhiteboards = async () => {
            setIsFetchingAllWhiteboards(true);
            try {
                const data = await studentApi.getMyWhiteboards();
                setAllWhiteboards(data || []);
            } catch (error) {
                console.error("Failed to fetch all whiteboards:", error);
            } finally {
                setIsFetchingAllWhiteboards(false);
            }
        };

        if (activeTab === 'whiteboards') {
            fetchAllWhiteboards();
        }
    }, [activeTab]);

    useEffect(() => {
        const fetchAttendance = async () => {
            setIsFetchingAttendance(true);
            try {
                const data = await studentApi.getMyAttendance();
                setAttendanceRecords(data || []);
            } catch (error) {
                console.error("Failed to fetch attendance:", error);
            } finally {
                setIsFetchingAttendance(false);
            }
        };

        if (activeTab === 'attendance') {
            fetchAttendance();
        }
    }, [activeTab]);

    // Poll for live streams every 15 seconds
    useEffect(() => {
        const fetchLiveStreams = async () => {
            try {
                const data = await studentApi.getActiveLiveStreams();
                setLiveStreams(data || []);
            } catch (error) {
                // Silently fail — not critical
            }
        };
        fetchLiveStreams();
        const interval = setInterval(fetchLiveStreams, 15000);
        return () => clearInterval(interval);
    }, []);

    const handleLogout = () => {
        localStorage.removeItem('studentToken');
        localStorage.removeItem('studentUser');
        toast.success('Logged out successfully');
        router.push('/student/login');
    };

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Live Now Banner */}
            {liveStreams.length > 0 && (
                <div className="bg-gradient-to-r from-red-600 to-pink-600 text-white">
                    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3">
                        {liveStreams.map((stream: any) => (
                            <div key={stream.sessionId} className="flex items-center justify-between">
                                <div className="flex items-center gap-3">
                                    <span className="flex items-center gap-2 bg-white/20 px-3 py-1 rounded-full text-sm font-bold">
                                        <span className="w-2 h-2 bg-white rounded-full animate-pulse"></span>
                                        LIVE NOW
                                    </span>
                                    <span className="font-semibold">{stream.courseName}</span>
                                    <span className="text-white/80 text-sm">• {stream.viewerCount || 0} watching</span>
                                </div>
                                <button
                                    onClick={() => router.push(`/live/${stream.sessionId}`)}
                                    className="bg-white text-red-600 px-4 py-1.5 rounded-lg text-sm font-bold hover:bg-gray-100 transition-colors"
                                >
                                    Join Class →
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            )}
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900">Student Dashboard</h1>
                        <p className="text-sm text-gray-600">Welcome back!</p>
                    </div>
                    <button
                        onClick={handleLogout}
                        className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                    >
                        Logout
                    </button>
                </div>
            </header>

            {/* Tabs */}
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-8">
                <div className="border-b border-gray-200">
                    <nav className="-mb-px flex space-x-8">
                        <button
                            onClick={() => setActiveTab('courses')}
                            className={`${activeTab === 'courses'
                                ? 'border-blue-500 text-blue-600'
                                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
                        >
                            Courses
                        </button>
                        <button
                            onClick={() => setActiveTab('summaries')}
                            className={`${activeTab === 'summaries'
                                ? 'border-blue-500 text-blue-600'
                                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
                        >
                            Class Summaries
                        </button>
                        <button
                            onClick={() => setActiveTab('whiteboards')}
                            className={`${activeTab === 'whiteboards'
                                ? 'border-blue-500 text-blue-600'
                                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
                        >
                            Whiteboards
                        </button>
                        <button
                            onClick={() => setActiveTab('attendance')}
                            className={`${activeTab === 'attendance'
                                ? 'border-blue-500 text-blue-600'
                                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
                        >
                            Attendance
                        </button>
                        <button
                            onClick={() => setActiveTab('profile')}
                            className={`${activeTab === 'profile'
                                ? 'border-blue-500 text-blue-600'
                                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
                        >
                            Profile
                        </button>
                    </nav>
                </div>

                {/* Content */}
                <div className="mt-8">
                    {activeTab === 'courses' && <CoursesTab />}
                    {activeTab === 'summaries' && <SummariesTab setSelectedSummary={setSelectedSummary} setShowSummaryView={setShowSummaryView} />}
                    {activeTab === 'profile' && <ProfileTab />}
                    {activeTab === 'whiteboards' && (
                        <div className="space-y-6 pb-20">
                            <div className="flex justify-between items-center">
                                <h2 className="text-2xl font-bold text-gray-900">My Saved Whiteboards</h2>
                                <button
                                    onClick={() => {
                                        const fetchAllWhiteboards = async () => {
                                            setIsFetchingAllWhiteboards(true);
                                            try {
                                                const data = await studentApi.getMyWhiteboards();
                                                setAllWhiteboards(data || []);
                                            } catch (error) {
                                                console.error("Failed to fetch all whiteboards:", error);
                                            } finally {
                                                setIsFetchingAllWhiteboards(false);
                                            }
                                        };
                                        fetchAllWhiteboards();
                                    }}
                                    className="p-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors flex items-center text-sm font-medium"
                                >
                                    <svg className="h-5 w-5 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                    </svg>
                                    Refresh Gallery
                                </button>
                            </div>

                            {isFetchingAllWhiteboards ? (
                                <div className="flex justify-center py-20">
                                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
                                </div>
                            ) : allWhiteboards.length > 0 ? (
                                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                                    {allWhiteboards.map((wb: any) => (
                                        <div key={wb.id} className="group bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden hover:shadow-xl transition-all duration-300 transform hover:-translate-y-1">
                                            <div className="aspect-video relative bg-gray-50 flex items-center justify-center overflow-hidden border-b border-gray-100">
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
                                                        className="max-h-full max-w-full object-contain p-2 group-hover:scale-105 transition-transform duration-500"
                                                        onError={(e: any) => {
                                                            e.target.src = 'https://via.placeholder.com/400x225?text=Whiteboard+Drawing';
                                                        }}
                                                    />
                                                )}
                                            </div>
                                            <div className="p-5">
                                                <div className="flex justify-between items-start mb-3">
                                                    <div>
                                                        <div className="flex items-center gap-2">
                                                            {wb.fileType?.match(/\.(mp4|webm|avi|mov)$/i) && (
                                                                <span className="px-2 py-0.5 bg-red-100 text-red-700 text-[10px] font-bold rounded-full">🎬 REC</span>
                                                            )}
                                                            <h3 className="text-base font-bold text-gray-900 truncate" title={wb.fileName}>{wb.fileName}</h3>
                                                        </div>
                                                        <p className="text-xs font-medium text-blue-600 uppercase tracking-wider mt-1">{wb.courseCode} • {wb.courseName}</p>
                                                    </div>
                                                    <a
                                                        href={`${API_BASE_URL}/materials/${wb.id}/download`}
                                                        download={wb.fileName}
                                                        target="_blank"
                                                        rel="noopener noreferrer"
                                                        className="p-2 bg-blue-50 text-blue-600 rounded-xl hover:bg-blue-600 hover:text-white transition-all shadow-sm"
                                                        title="Download"
                                                    >
                                                        <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a2 2 0 002 2h12a2 2 0 002-2v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                                                        </svg>
                                                    </a>
                                                </div>
                                                <div className="text-xs text-gray-500 mt-4 pt-4 border-t border-gray-50">
                                                    Captured on: {new Date(wb.uploadedAt).toLocaleDateString(undefined, { dateStyle: 'medium' })}
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            ) : (
                                <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-16 text-center">
                                    <h3 className="text-xl font-bold text-gray-900 mb-2">No Whiteboards Saved Yet</h3>
                                    <p className="text-gray-500 max-w-sm mx-auto">
                                        When drawings are saved during a session, they will appear here automatically.
                                    </p>
                                </div>
                            )}
                        </div>
                    )}

                    {activeTab === 'attendance' && (
                        <div className="space-y-6 pb-20">
                            <div className="flex justify-between items-center">
                                <h2 className="text-2xl font-bold text-gray-900">Attendance History</h2>
                                <p className="text-sm text-gray-500">Your presence recorded in synced sessions</p>
                            </div>

                            {isFetchingAttendance ? (
                                <div className="flex justify-center py-20">
                                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
                                </div>
                            ) : attendanceRecords.length > 0 ? (
                                <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
                                    <table className="min-w-full divide-y divide-gray-200">
                                        <thead className="bg-gray-50">
                                            <tr>
                                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Course</th>
                                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Session Date</th>
                                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Check-in Time</th>
                                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Method</th>
                                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                                            </tr>
                                        </thead>
                                        <tbody className="bg-white divide-y divide-gray-200">
                                            {attendanceRecords.map((record: any) => (
                                                <tr key={record.id} className="hover:bg-gray-50 transition-colors">
                                                    <td className="px-6 py-4 whitespace-nowrap">
                                                        <div className="text-sm font-bold text-gray-900">{record.courseCode}</div>
                                                        <div className="text-xs text-gray-500">{record.courseName}</div>
                                                    </td>
                                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                        {record.sessionDate ? new Date(record.sessionDate).toLocaleDateString() : 'N/A'}
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
                                                    <td className="px-6 py-4 whitespace-nowrap">
                                                        <span className="flex items-center text-sm font-medium text-green-600">
                                                            <svg className="h-4 w-4 mr-1.5" fill="currentColor" viewBox="0 0 20 20">
                                                                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                                                            </svg>
                                                            Present
                                                        </span>
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                            ) : (
                                <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-16 text-center">
                                    <div className="bg-blue-50 h-20 w-20 rounded-full flex items-center justify-center mx-auto mb-6">
                                        <svg className="h-10 w-10 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                                        </svg>
                                    </div>
                                    <h3 className="text-xl font-bold text-gray-900 mb-2">No Attendance Records</h3>
                                    <p className="text-gray-500 max-w-sm mx-auto">
                                        Your attendance for sessions will appear here once the lecturer ends and syncs the lecture details.
                                    </p>
                                </div>
                            )}
                        </div>
                    )}
                </div>

                {/* Full Screen View Summary & AI Chat Overlay */}
                {showSummaryView && selectedSummary && (
                    <div className="fixed inset-0 bg-white z-[100] flex flex-col overflow-hidden">
                        {/* Top Bar */}
                        <div className="bg-indigo-600 px-6 py-4 flex items-center justify-between text-white shadow-md">
                            <div className="flex items-center space-x-4">
                                <button
                                    onClick={() => {
                                        setShowSummaryView(false);
                                        setSelectedSummary(null);
                                        setChatMessages([]);
                                    }}
                                    className="p-2 hover:bg-indigo-700 rounded-full transition-colors"
                                    title="Close View"
                                >
                                    <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
                                    </svg>
                                </button>
                                <div>
                                    <h2 className="text-xl font-bold leading-tight">{selectedSummary.title}</h2>
                                    <p className="text-sm text-indigo-100 opacity-90">
                                        {selectedSummary.courseCode} — {selectedSummary.courseName} | Taught by {selectedSummary.lecturerName}
                                    </p>
                                </div>
                            </div>
                            <div className="flex items-center space-x-3">
                                <span className="text-xs bg-indigo-500 px-3 py-1 rounded-full border border-indigo-400">
                                    {selectedSummary.type === 'Weekly' ? `Week ${selectedSummary.weekNumber}` : 'Daily Summary'}
                                </span>
                                <button
                                    onClick={() => {
                                        setShowSummaryView(false);
                                        setSelectedSummary(null);
                                        setChatMessages([]);
                                    }}
                                    className="bg-white text-indigo-600 px-4 py-2 rounded-lg font-bold hover:bg-gray-100 transition-colors shadow-sm"
                                >
                                    Close Teaching
                                </button>
                            </div>
                        </div>

                        {/* Split Body */}
                        <div className="flex-1 flex overflow-hidden">
                            {/* Left Side: Summary Content (70%) */}
                            <div className="flex-[7] overflow-y-auto p-8 lg:p-12 bg-gray-50 border-r border-gray-200">
                                <div className="max-w-4xl mx-auto space-y-10">
                                    <section className="bg-white p-8 rounded-2xl shadow-sm border border-gray-100 prose prose-indigo max-w-none">
                                        <h3 className="text-2xl font-bold text-gray-900 border-b pb-4 mb-6">Weekly Learning Materials</h3>
                                        <div className="text-gray-700 whitespace-pre-wrap leading-relaxed space-y-4">
                                            {selectedSummary.summary}
                                        </div>
                                    </section>

                                    {selectedSummary.keyTopics && (
                                        <section className="bg-indigo-50 p-8 rounded-2xl border border-indigo-100">
                                            <h3 className="text-lg font-bold text-indigo-900 mb-4 flex items-center">
                                                <svg className="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 11h.01M7 15h.01M13 7h.01M13 11h.01M13 15h.01M17 7h.01M17 11h.01M17 15h.01" />
                                                </svg>
                                                Key Topics & Concepts
                                            </h3>
                                            <div className="flex flex-wrap gap-3">
                                                {(() => {
                                                    try {
                                                        const topics = typeof selectedSummary.keyTopics === 'string'
                                                            ? JSON.parse(selectedSummary.keyTopics)
                                                            : selectedSummary.keyTopics;
                                                        return Array.isArray(topics)
                                                            ? topics.map((topic: string, i: number) => (
                                                                <span key={i} className="px-3 py-1.5 bg-white text-indigo-700 text-sm font-semibold rounded-lg border border-indigo-200 shadow-sm">
                                                                    #{topic}
                                                                </span>
                                                            ))
                                                            : <p className="text-gray-600">{selectedSummary.keyTopics}</p>;
                                                    } catch {
                                                        return <p className="text-gray-600">{selectedSummary.keyTopics}</p>;
                                                    }
                                                })()}
                                            </div>
                                        </section>
                                    )}

                                    {selectedSummary.preparationNotes && (
                                        <section className="bg-amber-50 p-8 rounded-2xl border border-amber-100">
                                            <h3 className="text-lg font-bold text-amber-900 mb-4 flex items-center">
                                                <svg className="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
                                                </svg>
                                                Suggested Preparation
                                            </h3>
                                            <p className="text-amber-800 leading-relaxed italic">{selectedSummary.preparationNotes}</p>
                                        </section>
                                    )}

                                    {/* Whiteboards / Recordings / Materials Section */}
                                    {(isFetchingMaterials || materials.length > 0) && (
                                        <section className="bg-white p-8 rounded-2xl shadow-sm border border-gray-100">
                                            <h3 className="text-2xl font-bold text-gray-900 border-b pb-4 mb-6">Session Recordings & Materials</h3>
                                            {isFetchingMaterials ? (
                                                <div className="flex justify-center py-8">
                                                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
                                                </div>
                                            ) : (
                                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                                    {materials.map((material: any) => (
                                                        <div key={material.id} className="group relative bg-gray-50 rounded-xl overflow-hidden border border-gray-200 hover:shadow-md transition-all">
                                                            {material.fileType?.match(/\.(mp4|webm|avi|mov)$/i) ? (
                                                                /* Video Recording */
                                                                <div className="aspect-video relative bg-black">
                                                                    <video
                                                                        controls
                                                                        preload="metadata"
                                                                        className="w-full h-full object-contain"
                                                                        src={`${API_BASE_URL}/materials/${material.id}/download`}
                                                                    >
                                                                        Your browser does not support the video tag.
                                                                    </video>
                                                                </div>
                                                            ) : material.fileType?.match(/\.(jpg|jpeg|png|gif|webp)$/i) ? (
                                                                /* Image / Whiteboard Snapshot */
                                                                <div className="aspect-video relative bg-white flex items-center justify-center overflow-hidden">
                                                                    <img
                                                                        src={`${API_BASE_URL}/materials/${material.id}/download`}
                                                                        alt={material.fileName}
                                                                        className="max-h-full max-w-full object-contain"
                                                                        onError={(e: any) => {
                                                                            e.target.src = 'https://via.placeholder.com/400x225?text=Image+Load+Error';
                                                                        }}
                                                                    />
                                                                </div>
                                                            ) : (
                                                                /* Other File Types */
                                                                <div className="aspect-video flex flex-col items-center justify-center p-4">
                                                                    <svg className="h-12 w-12 text-gray-400 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                                                    </svg>
                                                                    <span className="text-xs font-medium text-gray-500">{material.fileType?.toUpperCase()} File</span>
                                                                </div>
                                                            )}
                                                            <div className="p-4 bg-white border-t border-gray-100 flex items-center justify-between">
                                                                <div className="truncate pr-4">
                                                                    <div className="flex items-center gap-2">
                                                                        {material.fileType?.match(/\.(mp4|webm|avi|mov)$/i) && (
                                                                            <span className="px-2 py-0.5 bg-red-100 text-red-700 text-[10px] font-bold rounded-full">🎬 RECORDING</span>
                                                                        )}
                                                                        <p className="text-sm font-bold text-gray-900 truncate" title={material.fileName}>{material.fileName}</p>
                                                                    </div>
                                                                    <p className="text-[10px] text-gray-500 uppercase tracking-tighter">
                                                                        {material.fileSize > 1024 * 1024 
                                                                            ? `${(material.fileSize / (1024 * 1024)).toFixed(1)} MB` 
                                                                            : `${(material.fileSize / 1024).toFixed(1)} KB`} • Session recording
                                                                    </p>
                                                                </div>
                                                                <a
                                                                    href={`${API_BASE_URL}/materials/${material.id}/download`}
                                                                    download={material.fileName}
                                                                    target="_blank"
                                                                    rel="noopener noreferrer"
                                                                    className="p-2 text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors flex-shrink-0"
                                                                    title="Download"
                                                                >
                                                                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a2 2 0 002 2h12a2 2 0 002-2v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                                                                    </svg>
                                                                </a>
                                                            </div>
                                                        </div>
                                                    ))}
                                                </div>
                                            )}
                                        </section>
                                    )}
                                </div>
                            </div>

                            {/* Right Side: Chatbox (30%) */}
                            <div className="flex-[3] flex flex-col bg-white border-l border-gray-200">
                                <div className="p-4 border-b border-gray-200 bg-gray-50 flex items-center justify-between">
                                    <div className="flex items-center space-x-2">
                                        <div className="h-3 w-3 bg-green-500 rounded-full animate-pulse"></div>
                                        <h3 className="text-sm font-bold text-gray-700 uppercase tracking-wider">AI Teaching Assistant</h3>
                                    </div>
                                    <span className="text-[10px] text-gray-400">Context: {selectedSummary.title}</span>
                                </div>

                                <div className="flex-1 overflow-y-auto p-4 space-y-4">
                                    {chatMessages.length === 0 ? (
                                        <div className="h-full flex flex-col items-center justify-center text-center p-6 space-y-4">
                                            <div className="bg-indigo-100 p-4 rounded-full">
                                                <svg className="h-8 w-8 text-indigo-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
                                                </svg>
                                            </div>
                                            <div>
                                                <p className="font-bold text-gray-900">Ask your AI Lecturer!</p>
                                                <p className="text-xs text-gray-500 mt-1">Questions about this week's topics, exams, or anything related to the course.</p>
                                            </div>
                                        </div>
                                    ) : (
                                        chatMessages.map((msg, i) => (
                                            <div key={i} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                                                <div className={`max-w-[85%] p-3 rounded-2xl text-sm ${msg.role === 'user'
                                                    ? 'bg-indigo-600 text-white rounded-tr-none shadow-md'
                                                    : 'bg-gray-100 text-gray-800 rounded-tl-none border border-gray-200'
                                                    }`}>
                                                    <p className="whitespace-pre-wrap">{msg.content}</p>
                                                </div>
                                            </div>
                                        ))
                                    )}
                                    {isAskingAI && (
                                        <div className="flex justify-start">
                                            <div className="bg-gray-100 p-3 rounded-2xl rounded-tl-none border border-gray-200 flex space-x-1">
                                                <div className="h-1.5 w-1.5 bg-gray-400 rounded-full animate-bounce"></div>
                                                <div className="h-1.5 w-1.5 bg-gray-400 rounded-full animate-bounce [animation-delay:0.2s]"></div>
                                                <div className="h-1.5 w-1.5 bg-gray-400 rounded-full animate-bounce [animation-delay:0.4s]"></div>
                                            </div>
                                        </div>
                                    )}
                                </div>

                                <div className="p-4 border-t border-gray-200">
                                    <form onSubmit={handleAskAI} className="relative">
                                        <input
                                            type="text"
                                            value={userQuestion}
                                            onChange={(e) => setUserQuestion(e.target.value)}
                                            placeholder="Ask a question..."
                                            disabled={isAskingAI}
                                            className="w-full pl-4 pr-12 py-3 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all outline-none text-sm disabled:opacity-50"
                                        />
                                        <button
                                            type="submit"
                                            disabled={!userQuestion.trim() || isAskingAI}
                                            className="absolute right-2 top-1.5 p-1.5 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:bg-gray-400"
                                        >
                                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                                            </svg>
                                        </button>
                                    </form>
                                    <p className="text-[10px] text-gray-400 mt-2 text-center italic">AI responses may be inaccurate. Always cross-check with course materials.</p>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

// Courses Tab Component
function CoursesTab() {
    const queryClient = useQueryClient();

    const { data: courses, isLoading } = useQuery({
        queryKey: ['student-courses'],
        queryFn: studentApi.getCourses,
    });

    const enrollMutation = useMutation({
        mutationFn: studentApi.enrollInCourse,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['student-courses'] });
            toast.success('Successfully enrolled in course!');
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to enroll');
        },
    });

    const unenrollMutation = useMutation({
        mutationFn: studentApi.unenrollFromCourse,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['student-courses'] });
            toast.success('Successfully unenrolled from course');
        },
        onError: () => {
            toast.error('Failed to unenroll');
        },
    });

    if (isLoading) {
        return <div className="text-center py-12">Loading courses...</div>;
    }

    return (
        <div>
            <h2 className="text-xl font-semibold text-gray-900 mb-6">Available Courses</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {courses?.map((course: any) => (
                    <div
                        key={course.id}
                        className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow"
                    >
                        <div className="flex justify-between items-start mb-4">
                            <div>
                                <h3 className="font-semibold text-lg text-gray-900">{course.courseCode}</h3>
                                <p className="text-sm text-gray-600 mt-1">{course.courseTitle}</p>
                            </div>
                            {course.isEnrolled && (
                                <span className="px-2 py-1 bg-green-100 text-green-800 text-xs font-semibold rounded">
                                    Enrolled
                                </span>
                            )}
                        </div>
                        <p className="text-sm text-gray-500 mb-4">
                            Lecturer: {course.lecturerName}
                        </p>
                        {course.isEnrolled ? (
                            <button
                                onClick={() => unenrollMutation.mutate(course.id)}
                                disabled={unenrollMutation.isPending}
                                className="w-full px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
                            >
                                {unenrollMutation.isPending ? 'Unenrolling...' : 'Unenroll'}
                            </button>
                        ) : (
                            <button
                                onClick={() => enrollMutation.mutate(course.id)}
                                disabled={enrollMutation.isPending}
                                className="w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
                            >
                                {enrollMutation.isPending ? 'Enrolling...' : 'Enroll'}
                            </button>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}

// Class Summaries Tab Component
function SummariesTab({ setSelectedSummary, setShowSummaryView }: { setSelectedSummary: any, setShowSummaryView: any }) {
    const [selectedCourseId, setSelectedCourseId] = useState<number | 'all'>('all');

    const { data: summaries, isLoading } = useQuery({
        queryKey: ['class-summaries'],
        queryFn: studentApi.getClassSummaries,
    });

    if (isLoading) {
        return <div className="text-center py-12">Loading class summaries...</div>;
    }

    if (!summaries || summaries.length === 0) {
        return (
            <div className="text-center py-12">
                <p className="text-gray-500">No class summaries available yet.</p>
                <p className="text-sm text-gray-400 mt-2">
                    Enroll in courses and wait for AI to generate your weekly teaching summaries.
                </p>
            </div>
        );
    }

    // Extract unique courses from summaries
    const courses = summaries.reduce((acc: any[], current: any) => {
        const x = acc.find(item => item.id === (current.courseId || 0));
        if (!x && current.courseCode) {
            return acc.concat([{
                id: current.courseId || 0,
                code: current.courseCode,
                name: current.courseName
            }]);
        } else {
            return acc;
        }
    }, []);

    const filteredSummaries = selectedCourseId === 'all'
        ? summaries
        : summaries.filter((s: any) => s.courseId === selectedCourseId);

    return (
        <div>
            <div className="flex flex-col md:flex-row md:items-center justify-between mb-8 gap-4">
                <div>
                    <h2 className="text-2xl font-bold text-gray-900">Weekly AI Teaching Summaries</h2>
                    <p className="text-sm text-gray-500 mt-1">Review your AI-generated summaries organized by course.</p>
                </div>
                <div className="text-xs font-semibold text-blue-700 bg-blue-50 border border-blue-100 px-3 py-1.5 rounded-full">
                    {summaries.length} Total Summaries
                </div>
            </div>

            {/* Course Tabs */}
            <div className="mb-6 overflow-x-auto pb-2">
                <div className="flex space-x-2 min-w-max">
                    <button
                        onClick={() => setSelectedCourseId('all')}
                        className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${selectedCourseId === 'all'
                            ? 'bg-blue-600 text-white shadow-md shadow-blue-200'
                            : 'bg-white text-gray-600 border border-gray-200 hover:border-blue-300 hover:text-blue-600'
                            }`}
                    >
                        All Courses
                    </button>
                    {courses.map((course: any) => (
                        <button
                            key={course.id}
                            onClick={() => setSelectedCourseId(course.id)}
                            className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${selectedCourseId === course.id
                                ? 'bg-blue-600 text-white shadow-md shadow-blue-200'
                                : 'bg-white text-gray-600 border border-gray-200 hover:border-blue-300 hover:text-blue-600'
                                }`}
                        >
                            {course.code}
                        </button>
                    ))}
                </div>
            </div>

            {filteredSummaries.length === 0 ? (
                <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
                    <p className="text-gray-500 font-medium">No summaries found for this course.</p>
                </div>
            ) : (
                <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
                    <div className="overflow-x-auto">
                        <table className="min-w-full divide-y divide-gray-200">
                            <thead className="bg-gray-50">
                                <tr>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-bold text-gray-500 uppercase tracking-wider">Course</th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-bold text-gray-500 uppercase tracking-wider">Week</th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-bold text-gray-500 uppercase tracking-wider">Topic</th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-bold text-gray-500 uppercase tracking-wider">Date</th>
                                    <th scope="col" className="px-6 py-3 text-right text-xs font-bold text-gray-500 uppercase tracking-wider">Action</th>
                                </tr>
                            </thead>
                            <tbody className="bg-white divide-y divide-gray-200">
                                {filteredSummaries.map((summary: any) => (
                                    <tr key={`${summary.type}-${summary.id}`} className="hover:bg-blue-50/50 transition-colors">
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="text-sm font-bold text-gray-900">{summary.courseCode}</div>
                                            <div className="text-[10px] text-gray-500 uppercase tracking-tight">{summary.courseName}</div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <span className={`px-3 py-1 text-[10px] font-black uppercase rounded-full ${summary.type === 'Weekly' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'}`}>
                                                {summary.type === 'Weekly' ? `Week ${summary.weekNumber}` : 'Day Summary'}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4">
                                            <div className="text-sm text-gray-900 font-bold">{summary.title}</div>
                                            <div className="text-xs text-gray-500 truncate max-w-[250px] mt-0.5">{summary.summary.substring(0, 80)}...</div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 font-medium">
                                            {new Date(summary.classDate).toLocaleDateString(undefined, { day: '2-digit', month: '2-digit', year: 'numeric' })}
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                            <button
                                                onClick={() => {
                                                    setSelectedSummary(summary);
                                                    setShowSummaryView(true);
                                                }}
                                                className="px-4 py-2 bg-blue-600 text-white text-xs font-bold rounded-lg hover:bg-blue-700 transform active:scale-95 transition-all shadow-sm hover:shadow-md"
                                            >
                                                View Teaching
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
        </div>
    );
}

// Profile Tab Component
function ProfileTab() {
    const queryClient = useQueryClient();
    const [editing, setEditing] = useState(false);
    const [photoPreview, setPhotoPreview] = useState<string | null>(null);
    const [formData, setFormData] = useState({
        fullName: '',
        email: '',
        age: '',
        hobbies: '',
        bio: '',
        photo: null as File | null,
    });

    const { data: profile, isLoading } = useQuery<any>({
        queryKey: ['student-profile'],
        queryFn: studentApi.getProfile,
    });

    useEffect(() => {
        if (profile) {
            setFormData({
                fullName: profile.fullName || '',
                email: profile.email || '',
                age: profile.age?.toString() || '',
                hobbies: profile.hobbies || '',
                bio: profile.bio || '',
                photo: null,
            });
        }
    }, [profile]);

    const updateMutation = useMutation({
        mutationFn: (data: FormData) => studentApi.updateProfile(data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['student-profile'] });
            toast.success('Profile updated successfully!');
            setEditing(false);
            setPhotoPreview(null);
        },
        onError: () => {
            toast.error('Failed to update profile');
        },
    });

    const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) {
            setFormData({ ...formData, photo: file });
            const reader = new FileReader();
            reader.onloadend = () => {
                setPhotoPreview(reader.result as string);
            };
            reader.readAsDataURL(file);
        }
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const data = new FormData();
        data.append('fullName', formData.fullName);
        data.append('email', formData.email);
        if (formData.age) data.append('age', formData.age);
        if (formData.hobbies) data.append('hobbies', formData.hobbies);
        if (formData.bio) data.append('bio', formData.bio);
        if (formData.photo) data.append('photo', formData.photo);

        updateMutation.mutate(data);
    };

    if (isLoading) {
        return <div className="text-center py-12">Loading profile...</div>;
    }

    return (
        <div className="max-w-3xl">
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <div className="flex justify-between items-center mb-6">
                    <h2 className="text-xl font-semibold text-gray-900">My Profile</h2>
                    {!editing && (
                        <button
                            onClick={() => setEditing(true)}
                            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                        >
                            Edit Profile
                        </button>
                    )}
                </div>

                {editing ? (
                    <form onSubmit={handleSubmit} className="space-y-6">
                        {/* Photo Upload */}
                        <div className="flex items-center space-x-6">
                            <div className="flex-shrink-0">
                                {photoPreview || profile?.photoPath ? (
                                    <img
                                        src={photoPreview || `${API_SERVER_URL}${profile.photoPath}`}
                                        alt="Profile"
                                        className="w-24 h-24 rounded-full object-cover"
                                    />
                                ) : (
                                    <div className="w-24 h-24 rounded-full bg-gray-200 flex items-center justify-center">
                                        <span className="text-gray-500 text-2xl">
                                            {profile?.fullName?.charAt(0) || '?'}
                                        </span>
                                    </div>
                                )}
                            </div>
                            <div>
                                <label className="block">
                                    <span className="sr-only">Choose profile photo</span>
                                    <input
                                        type="file"
                                        accept="image/*"
                                        onChange={handlePhotoChange}
                                        className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
                                    />
                                </label>
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Full Name
                                </label>
                                <input
                                    type="text"
                                    value={formData.fullName}
                                    onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Email
                                </label>
                                <input
                                    type="email"
                                    value={formData.email}
                                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Age
                                </label>
                                <input
                                    type="number"
                                    value={formData.age}
                                    onChange={(e) => setFormData({ ...formData, age: e.target.value })}
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Hobbies
                                </label>
                                <input
                                    type="text"
                                    value={formData.hobbies}
                                    onChange={(e) => setFormData({ ...formData, hobbies: e.target.value })}
                                    placeholder="e.g., Reading, Sports, Music"
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                                />
                            </div>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-2">
                                Bio
                            </label>
                            <textarea
                                value={formData.bio}
                                onChange={(e) => setFormData({ ...formData, bio: e.target.value })}
                                rows={4}
                                placeholder="Tell us about yourself..."
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                            />
                        </div>

                        <div className="flex gap-3">
                            <button
                                type="button"
                                onClick={() => {
                                    setEditing(false);
                                    setPhotoPreview(null);
                                }}
                                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                disabled={updateMutation.isPending}
                                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                            >
                                {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
                            </button>
                        </div>
                    </form>
                ) : (
                    <div className="space-y-6">
                        <div className="flex items-center space-x-6">
                            {profile?.photoPath ? (
                                <img
                                    src={`${API_SERVER_URL}${profile.photoPath}`}
                                    alt="Profile"
                                    className="w-24 h-24 rounded-full object-cover"
                                />
                            ) : (
                                <div className="w-24 h-24 rounded-full bg-gray-200 flex items-center justify-center">
                                    <span className="text-gray-500 text-3xl">
                                        {profile?.fullName?.charAt(0) || '?'}
                                    </span>
                                </div>
                            )}
                            <div>
                                <h3 className="text-2xl font-semibold text-gray-900">{profile?.fullName}</h3>
                                <p className="text-gray-600">{profile?.matricNumber}</p>
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div>
                                <h4 className="text-sm font-medium text-gray-500 mb-1">Email</h4>
                                <p className="text-gray-900">{profile?.email || 'Not provided'}</p>
                            </div>
                            <div>
                                <h4 className="text-sm font-medium text-gray-500 mb-1">Age</h4>
                                <p className="text-gray-900">{profile?.age || 'Not provided'}</p>
                            </div>
                            <div>
                                <h4 className="text-sm font-medium text-gray-500 mb-1">Hobbies</h4>
                                <p className="text-gray-900">{profile?.hobbies || 'Not provided'}</p>
                            </div>
                        </div>

                        {profile?.bio && (
                            <div>
                                <h4 className="text-sm font-medium text-gray-500 mb-1">Bio</h4>
                                <p className="text-gray-900">{profile.bio}</p>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}
