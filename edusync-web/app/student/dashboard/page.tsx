'use client';
import { API_BASE_URL, API_SERVER_URL } from '@/lib/config';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { studentApi } from '@/lib/studentApi';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import DashboardLayout from '@/components/DashboardLayout';

export default function StudentDashboard() {
    const router = useRouter();
    const [activeTab, setActiveTab] = useState<'courses' | 'profile' | 'whiteboards' | 'attendance'>('courses');
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

    const studentUser = typeof window !== 'undefined' ? JSON.parse(localStorage.getItem('studentUser') || '{"fullName":"Student"}') : { fullName: 'Student' };

    const { data: profile } = useQuery<any>({
        queryKey: ['student-profile'],
        queryFn: studentApi.getProfile,
    });

    const studentNav = [
        { id: 'courses', label: 'Courses', icon: '📚' },
        { id: 'whiteboards', label: 'Recorded Lectures', icon: '🎥' },
        { id: 'attendance', label: 'Attendance', icon: '📋' },
        { id: 'profile', label: 'Profile', icon: '👤' },
    ];

    return (
        <DashboardLayout
            role="student"
            userName={studentUser?.fullName || 'Student'}
            profileImage={profile?.photoPath ? `${API_SERVER_URL}${profile.photoPath}` : undefined}
            navItems={studentNav}
            activeNav={activeTab}
            onNavChange={(id) => setActiveTab(id as any)}
            onLogout={handleLogout}
        >
            <div>
            {/* Live Now Banner */}
            {liveStreams.length > 0 && (
                <div className="bg-gradient-to-r from-red-600 to-pink-600 text-white rounded-2xl mb-6 overflow-hidden">
                    <div className="px-6 py-3">
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
                    {activeTab === 'courses' && <CoursesTab />}
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
                                    className="p-2 text-[#FF6B35] hover:bg-[#FFF3E0] rounded-lg transition-colors flex items-center text-sm font-medium"
                                >
                                    <svg className="h-5 w-5 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                    </svg>
                                    Refresh Gallery
                                </button>
                            </div>

                            {isFetchingAllWhiteboards ? (
                                <div className="flex justify-center py-20">
                                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#FF6B35]"></div>
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
                                                        </div>
                                                        <p className="text-xs font-medium text-[#FF6B35] uppercase tracking-wider mt-1">{wb.courseCode} • {wb.courseName}</p>
                                                    </div>
                                                    <a
                                                        href={`${API_BASE_URL}/materials/${wb.id}/download`}
                                                        download={wb.fileName}
                                                        target="_blank"
                                                        rel="noopener noreferrer"
                                                        className="p-2 bg-[#FFF3E0] text-[#FF6B35] rounded-xl hover:bg-[#FF6B35] hover:text-white transition-all shadow-sm"
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
                                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#FF6B35]"></div>
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
                                                                ? 'bg-[#FFF3E0] text-[#FF6B35]'
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
                                    <div className="bg-[#FFF3E0] h-20 w-20 rounded-full flex items-center justify-center mx-auto mb-6">
                                        <svg className="h-10 w-10 text-[#FF6B35]" fill="none" viewBox="0 0 24 24" stroke="currentColor">
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
        </DashboardLayout>
    );
}

// ─── Dummy Course Timetable Data ─────────────────────────────────────────────
const SEMESTER_START = new Date(2025, 0, 6); // Monday 6 Jan 2025
const SEMESTER_WEEKS = 12;

interface CourseEvent {
    code: string;
    title: string;
    lecturer: string;
    days: number[]; // 0=Sun,1=Mon,...6=Sat
    startTime: string;
    endTime: string;
    color: string;   // text color class
    bgColor: string; // background color (hex or tailwind)
    dotColor: string; // dot/accent color (hex)
}

const DUMMY_COURSES: CourseEvent[] = [
    { code: 'CSC301', title: 'Data Structures', lecturer: 'Dr. Adeyemi', days: [1, 3, 5], startTime: '08:00', endTime: '09:00', color: '#7c3aed', bgColor: '#f3e8ff', dotColor: '#7c3aed' },
    { code: 'CSC303', title: 'Operating Systems', lecturer: 'Prof. Okafor', days: [1, 3], startTime: '09:00', endTime: '10:00', color: '#2563eb', bgColor: '#dbeafe', dotColor: '#2563eb' },
    { code: 'CSC305', title: 'Computer Networks', lecturer: 'Dr. Bello', days: [2, 4, 5], startTime: '08:00', endTime: '09:30', color: '#0d9488', bgColor: '#ccfbf1', dotColor: '#0d9488' },
    { code: 'CSC307', title: 'Software Eng.', lecturer: 'Dr. Nwankwo', days: [1, 3, 5], startTime: '10:00', endTime: '11:00', color: '#ea580c', bgColor: '#ffedd5', dotColor: '#ea580c' },
    { code: 'CSC309', title: 'Database Systems', lecturer: 'Prof. Ibrahim', days: [2, 4], startTime: '10:00', endTime: '11:00', color: '#dc2626', bgColor: '#fee2e2', dotColor: '#dc2626' },
    { code: 'MTH301', title: 'Numerical Methods', lecturer: 'Dr. Chukwu', days: [2, 4], startTime: '11:30', endTime: '12:30', color: '#4f46e5', bgColor: '#e0e7ff', dotColor: '#4f46e5' },
    { code: 'CSC311', title: 'Artificial Intelligence', lecturer: 'Prof. Eze', days: [1, 3], startTime: '11:30', endTime: '12:30', color: '#059669', bgColor: '#d1fae5', dotColor: '#059669' },
    { code: 'CSC313', title: 'Computer Arch.', lecturer: 'Dr. Abubakar', days: [2, 4, 5], startTime: '14:00', endTime: '15:00', color: '#d97706', bgColor: '#fef3c7', dotColor: '#d97706' },
    { code: 'EEE301', title: 'Signal Processing', lecturer: 'Prof. Uche', days: [1, 3, 5], startTime: '14:00', endTime: '15:00', color: '#db2777', bgColor: '#fce7f3', dotColor: '#db2777' },
    { code: 'GST301', title: 'Entrepreneurship', lecturer: 'Dr. Fashola', days: [2, 4], startTime: '15:30', endTime: '17:00', color: '#475569', bgColor: '#f1f5f9', dotColor: '#475569' },
];

function getCoursesForDate(date: Date): CourseEvent[] {
    const dayOfWeek = date.getDay();
    const semesterEnd = new Date(SEMESTER_START);
    semesterEnd.setDate(semesterEnd.getDate() + SEMESTER_WEEKS * 7);
    if (date < SEMESTER_START || date >= semesterEnd) return [];
    return DUMMY_COURSES.filter(c => c.days.includes(dayOfWeek));
}

function getDaysInMonth(year: number, month: number) {
    return new Date(year, month + 1, 0).getDate();
}

function getFirstDayOfMonth(year: number, month: number) {
    return new Date(year, month, 1).getDay();
}

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
const DAY_HEADERS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

// Courses Tab Component — Monthly Calendar Timetable
function CoursesTab() {
    const [viewMonth, setViewMonth] = useState(0); // 0 = January 2025
    const [selectedDate, setSelectedDate] = useState<Date | null>(null);
    const [selectedCourse, setSelectedCourse] = useState<CourseEvent | null>(null);
    const [selectedCourseDate, setSelectedCourseDate] = useState<Date | null>(null);
    const [summaryTab, setSummaryTab] = useState<'general' | 'personalized'>('general');
    const [courses, setCourses] = useState<CourseEvent[]>(DUMMY_COURSES);

    // Fetch real lecturer names from API and merge into course data
    useEffect(() => {
        const fetchRealLecturers = async () => {
            try {
                const token = localStorage.getItem('studentToken');
                if (!token) return;
                const res = await fetch(`${API_BASE_URL}/students/my-courses`, {
                    headers: { Authorization: `Bearer ${token}` }
                });
                if (!res.ok) return;
                const enrolled: { courseCode: string; courseTitle: string; lecturerName: string }[] = await res.json();
                if (!enrolled.length) return;
                // Build lookup: courseCode -> real lecturer name
                const lecturerMap: Record<string, string> = {};
                enrolled.forEach(c => {
                    if (c.lecturerName && c.lecturerName !== 'TBA') {
                        lecturerMap[c.courseCode] = c.lecturerName;
                    }
                });
                // Merge into DUMMY_COURSES
                setCourses(DUMMY_COURSES.map(dc => ({
                    ...dc,
                    lecturer: lecturerMap[dc.code] || dc.lecturer
                })));
            } catch { /* keep dummy data on error */ }
        };
        fetchRealLecturers();
    }, []);

    const getCoursesForDateLocal = (date: Date): CourseEvent[] => {
        const dayOfWeek = date.getDay();
        const semesterEnd = new Date(SEMESTER_START);
        semesterEnd.setDate(semesterEnd.getDate() + SEMESTER_WEEKS * 7);
        if (date < SEMESTER_START || date >= semesterEnd) return [];
        return courses.filter(c => c.days.includes(dayOfWeek));
    };

    const year = 2025;
    const month = viewMonth; // 0-indexed
    const daysInMonth = getDaysInMonth(year, month);
    const firstDay = getFirstDayOfMonth(year, month);
    const today = new Date();
    const isToday = (d: number) => today.getFullYear() === year && today.getMonth() === month && today.getDate() === d;

    // Build calendar grid cells
    const calendarCells: (number | null)[] = [];
    for (let i = 0; i < firstDay; i++) calendarCells.push(null);
    for (let d = 1; d <= daysInMonth; d++) calendarCells.push(d);
    while (calendarCells.length % 7 !== 0) calendarCells.push(null);

    // Get courses for selected date (for detail panel)
    const selectedCourses = selectedDate ? getCoursesForDateLocal(selectedDate) : [];

    const prevMonth = () => setViewMonth(m => Math.max(0, m - 1));
    const nextMonth = () => setViewMonth(m => Math.min(2, m + 1)); // Jan-Mar 2025

    const handleCourseClick = (course: CourseEvent, dateCtx?: Date) => {
        setSelectedCourse(course);
        setSelectedCourseDate(dateCtx ?? selectedDate ?? today);
        setSummaryTab('general');
    };


    // ─── Course Detail View ─────────────────────────────────
    if (selectedCourse) {
        const dayNames = selectedCourse.days.map(d => DAY_HEADERS[d]).join(', ');
        return (
            <div className="space-y-5 pb-10">
                {/* Back button + course header */}
                <div className="flex items-center gap-4">
                    <button
                        onClick={() => setSelectedCourse(null)}
                        className="p-2 rounded-xl hover:bg-gray-100 transition-colors"
                        title="Back to Schedule"
                    >
                        <svg className="w-5 h-5 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" /></svg>
                    </button>
                    <div className="flex-1">
                        <div className="flex items-center gap-3">
                            <h2 className="text-2xl font-bold text-gray-900">{selectedCourse.code}</h2>
                            <span className="text-xs font-bold px-3 py-1 rounded-full" style={{ backgroundColor: selectedCourse.bgColor, color: selectedCourse.color }}>{selectedCourse.title}</span>
                        </div>
                        <p className="text-sm text-gray-400 mt-0.5">{selectedCourse.lecturer} • {dayNames} • {selectedCourse.startTime} – {selectedCourse.endTime}</p>
                    </div>
                </div>

                {/* Course info card */}
                <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5">
                    <div className="flex items-center gap-4">
                        <div className="w-14 h-14 rounded-2xl flex items-center justify-center text-2xl font-black text-white" style={{ backgroundColor: selectedCourse.dotColor }}>
                            {selectedCourse.code.slice(-3, -1)}
                        </div>
                        <div className="flex-1">
                            <h3 className="text-lg font-bold text-gray-900">{selectedCourse.title}</h3>
                            <p className="text-sm text-gray-500">{selectedCourse.lecturer}</p>
                        </div>
                        <div className="text-right">
                            <p className="text-xs font-bold text-gray-700">{dayNames}</p>
                            <p className="text-xs text-gray-400">{selectedCourse.startTime} – {selectedCourse.endTime}</p>
                            <p className="text-[10px] text-purple-500 font-bold mt-1">12-week semester</p>
                        </div>
                    </div>
                </div>

                {/* Sub-tabs: General Summaries / Personalized Summaries */}
                <div className="flex items-center gap-1 bg-gray-100 rounded-full p-1 w-fit">
                    <button
                        onClick={() => setSummaryTab('general')}
                        className={`px-5 py-2 text-sm font-bold rounded-full transition-all ${
                            summaryTab === 'general' ? 'bg-[#7c3aed] text-white shadow-sm' : 'text-gray-500 hover:text-gray-700'
                        }`}
                    >
                        📄 General Summaries
                    </button>
                    <button
                        onClick={() => setSummaryTab('personalized')}
                        className={`px-5 py-2 text-sm font-bold rounded-full transition-all ${
                            summaryTab === 'personalized' ? 'bg-[#7c3aed] text-white shadow-sm' : 'text-gray-500 hover:text-gray-700'
                        }`}
                    >
                        🎯 Personalized Summaries
                    </button>
                </div>

                {/* Tab Content */}
                {summaryTab === 'general' ? (
                    <GeneralSummariesSection
                        course={selectedCourse}
                        clickedDate={selectedCourseDate}
                    />
                ) : (
                    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-10 text-center">
                        <div className="text-4xl mb-3">🎯</div>
                        <h4 className="text-lg font-bold text-gray-900 mb-1">Personalized Summaries</h4>
                        <p className="text-sm text-gray-400">No personalized summaries available yet for {selectedCourse.code}.</p>
                    </div>
                )}
            </div>
        );
    }

    // ─── Calendar View (default) ─────────────────────────────────
    return (
        <div className="space-y-5 pb-10">
            {/* Top header bar */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <h2 className="text-2xl font-bold text-gray-900">My Schedule 📅</h2>
                    <p className="text-sm text-gray-400 mt-0.5">You are doing great, stay consistent!</p>
                </div>
                {/* View toggle pills */}
                <div className="flex items-center gap-1 bg-gray-100 rounded-full p-1">
                    <button className="px-4 py-1.5 text-xs font-bold rounded-full bg-[#7c3aed] text-white shadow-sm">Month</button>
                    <button className="px-4 py-1.5 text-xs font-medium rounded-full text-gray-500 hover:text-gray-700 transition-colors">Week</button>
                    <button className="px-4 py-1.5 text-xs font-medium rounded-full text-gray-500 hover:text-gray-700 transition-colors">Day</button>
                </div>
            </div>

            <div className="flex flex-col lg:flex-row gap-5">
                {/* ─── Main Calendar ─────────────────────────── */}
                <div className="flex-1 bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
                    {/* Month navigation */}
                    <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
                        <div className="flex items-center gap-3">
                            <h3 className="text-lg font-bold text-gray-900">{MONTH_NAMES[month]} {year}</h3>
                            <span className="text-xs bg-purple-100 text-purple-700 font-bold px-2 py-0.5 rounded-full">{DUMMY_COURSES.length} courses</span>
                        </div>
                        <div className="flex items-center gap-1">
                            <button onClick={prevMonth} disabled={viewMonth === 0} className="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-30 transition-colors">
                                <svg className="w-4 h-4 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" /></svg>
                            </button>
                            <button onClick={nextMonth} disabled={viewMonth >= 2} className="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-30 transition-colors">
                                <svg className="w-4 h-4 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" /></svg>
                            </button>
                        </div>
                    </div>

                    {/* Day headers */}
                    <div className="grid grid-cols-7 border-b border-gray-100">
                        {DAY_HEADERS.map(d => (
                            <div key={d} className="px-2 py-3 text-center text-xs font-bold text-gray-400 uppercase tracking-wider">{d}</div>
                        ))}
                    </div>

                    {/* Calendar grid */}
                    <div className="grid grid-cols-7">
                        {calendarCells.map((day, idx) => {
                            if (day === null) {
                                return <div key={`empty-${idx}`} className="min-h-[100px] bg-gray-50/50 border-b border-r border-gray-50" />;
                            }
                            const date = new Date(year, month, day);
                            const courses = getCoursesForDate(date);
                            const dayIsToday = isToday(day);
                            const isSelected = selectedDate?.getDate() === day && selectedDate?.getMonth() === month;

                            return (
                                <div
                                    key={day}
                                    onClick={() => setSelectedDate(date)}
                                    className={`min-h-[100px] p-1.5 border-b border-r border-gray-100 cursor-pointer transition-all hover:bg-purple-50/50 ${
                                        isSelected ? 'bg-purple-50 ring-2 ring-[#7c3aed] ring-inset' : ''
                                    }`}
                                >
                                    <div className={`text-xs font-bold mb-1 w-6 h-6 flex items-center justify-center rounded-full ${
                                        dayIsToday ? 'bg-[#7c3aed] text-white' : 'text-gray-700'
                                    }`}>
                                        {day}
                                    </div>
                                    <div className="space-y-0.5">
                                        {courses.slice(0, 3).map(c => (
                                            <div
                                                key={c.code}
                                                className="rounded-md px-1.5 py-0.5 text-[10px] font-bold truncate leading-tight"
                                                style={{ backgroundColor: c.bgColor, color: c.color }}
                                                title={`${c.code} — ${c.title}\n${c.startTime} – ${c.endTime}`}
                                            >
                                                {c.code} <span className="font-normal opacity-70">{c.startTime}</span>
                                            </div>
                                        ))}
                                        {courses.length > 3 && (
                                            <div className="text-[9px] font-bold text-purple-500 pl-1">+{courses.length - 3} more</div>
                                        )}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>

                {/* ─── Right Sidebar ─────────────────────────── */}
                <div className="w-full lg:w-[280px] space-y-5 flex-shrink-0">
                    {/* Mini Calendar */}
                    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
                        <div className="flex items-center justify-between mb-3">
                            <h4 className="text-sm font-bold text-gray-900">{MONTH_NAMES[month]} {year}</h4>
                            <div className="flex gap-1">
                                <button onClick={prevMonth} disabled={viewMonth === 0} className="p-1 rounded hover:bg-gray-100 disabled:opacity-30"><svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" /></svg></button>
                                <button onClick={nextMonth} disabled={viewMonth >= 2} className="p-1 rounded hover:bg-gray-100 disabled:opacity-30"><svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" /></svg></button>
                            </div>
                        </div>
                        <div className="grid grid-cols-7 gap-0.5 text-center">
                            {['Su','Mo','Tu','We','Th','Fr','Sa'].map(d => (
                                <div key={d} className="text-[10px] font-bold text-gray-400 py-1">{d}</div>
                            ))}
                            {calendarCells.map((day, idx) => (
                                <button
                                    key={idx}
                                    onClick={() => day && setSelectedDate(new Date(year, month, day))}
                                    disabled={!day}
                                    className={`text-[11px] py-1 rounded-full font-medium transition-all ${
                                        !day ? 'invisible' :
                                        isToday(day!) ? 'bg-[#7c3aed] text-white font-bold' :
                                        selectedDate?.getDate() === day && selectedDate?.getMonth() === month ? 'bg-purple-100 text-purple-700 font-bold' :
                                        'text-gray-600 hover:bg-purple-50'
                                    }`}
                                >
                                    {day || ''}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Selected Day Detail */}
                    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
                        <div className="flex items-center justify-between mb-3">
                            <h4 className="text-sm font-bold text-gray-900">
                                {selectedDate ? `${DAY_HEADERS[selectedDate.getDay()]}, ${MONTH_NAMES[selectedDate.getMonth()]} ${selectedDate.getDate()}` : 'Today\'s Classes'}
                            </h4>
                            <span className="text-[10px] font-bold text-purple-600 bg-purple-50 px-2 py-0.5 rounded-full">
                                {selectedCourses.length || getCoursesForDate(today).length} classes
                            </span>
                        </div>
                        <div className="space-y-2">
                            {(selectedCourses.length > 0 ? selectedCourses : getCoursesForDate(today)).map(c => (
                                <div key={c.code} onClick={() => handleCourseClick(c, selectedDate ?? today)} className="flex items-center gap-2.5 p-2.5 rounded-xl hover:bg-gray-50 transition-colors cursor-pointer">
                                    <div className="w-1 h-10 rounded-full flex-shrink-0" style={{ backgroundColor: c.dotColor }} />
                                    <div className="flex-1 min-w-0">
                                        <p className="text-xs font-bold text-gray-900 truncate">{c.title}</p>
                                        <p className="text-[10px] text-gray-400">{c.startTime} – {c.endTime}</p>
                                    </div>
                                    <span className="text-[10px] font-bold px-2 py-0.5 rounded-full flex-shrink-0" style={{ backgroundColor: c.bgColor, color: c.color }}>
                                        {c.code}
                                    </span>
                                </div>
                            ))}
                            {selectedCourses.length === 0 && getCoursesForDate(today).length === 0 && (
                                <p className="text-xs text-gray-400 text-center py-4">No classes scheduled</p>
                            )}
                        </div>
                    </div>

                    {/* Course Legend */}
                    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
                        <h4 className="text-sm font-bold text-gray-900 mb-3">My Courses</h4>
                        <div className="space-y-2">
                            {DUMMY_COURSES.map(c => (
                                <div key={c.code} onClick={() => handleCourseClick(c)} className="flex items-center gap-2 cursor-pointer hover:bg-gray-50 rounded-lg px-1 py-0.5 -mx-1 transition-colors">
                                    <div className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: c.dotColor }} />
                                    <span className="text-[11px] font-bold text-gray-700">{c.code}</span>
                                    <span className="text-[10px] text-gray-400 truncate">{c.title}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}


// ─── General Summaries Section ──────────────────────────────────────────────
// Fetches the exact AI daily summary for the clicked course + date.
function GeneralSummariesSection({ course, clickedDate }: { course: CourseEvent | null; clickedDate: Date | null }) {
    const effectiveDate = clickedDate || new Date();

    // Compute which semester week (1-12) the clicked date falls in
    const msPerWeek = 7 * 24 * 60 * 60 * 1000;
    const diffMs = effectiveDate.getTime() - SEMESTER_START.getTime();
    const weekNumber = diffMs < 0 ? 0 : Math.min(Math.floor(diffMs / msPerWeek) + 1, SEMESTER_WEEKS);

    // Compute which day ordinal (1, 2, 3) within the course schedule
    const clickedDow = effectiveDate.getDay();
    const courseDays = course?.days ?? [];
    const sortedDays = [...courseDays].sort((a: number, b: number) => a - b);
    const dayIndex = sortedDays.indexOf(clickedDow);
    const dayOrdinal = dayIndex >= 0 ? dayIndex + 1 : 1;
    const totalDays = Math.max(sortedDays.length, 1);
    const dayLabels = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    const dayName = dayLabels[clickedDow] || 'Today';

    const { data: summaries, isLoading } = useQuery({
        queryKey: ['course-summaries', course?.code],
        queryFn: () => course ? studentApi.getCourseSummariesByCode(course.code) : Promise.resolve([]),
        enabled: !!course,
    });

    if (!course) return null;

    if (isLoading) {
        return (
            <div className="flex justify-center py-16">
                <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-purple-600" />
            </div>
        );
    }

    // Look up the exact daily summary by weekNumber AND dayNumber
    const daySummary = (summaries || []).find(
        (s: any) => s.weekNumber === weekNumber && s.dayNumber === dayOrdinal
    );

    if (!daySummary) {
        return (
            <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-10 text-center">
                <div className="text-5xl mb-4">📄</div>
                <h4 className="text-lg font-bold text-gray-900 mb-1">No Summary Yet</h4>
                <p className="text-sm text-gray-400 max-w-sm mx-auto">
                    The lecturer has not uploaded and analyzed the syllabus for {course.code} yet.
                </p>
                <div className="mt-5 inline-flex items-center gap-2 text-xs text-purple-500 bg-purple-50 px-4 py-2 rounded-full font-semibold">
                    Week {weekNumber} &bull; Day {dayOrdinal} of {totalDays} &bull; {dayName}
                </div>
            </div>
        );
    }

    let keyTopics: string[] = [];
    try {
        keyTopics = typeof daySummary.keyTopics === 'string'
            ? JSON.parse(daySummary.keyTopics)
            : (daySummary.keyTopics || []);
    } catch { keyTopics = []; }

    const isLastDay = dayOrdinal === totalDays;

    return (
        <div className="space-y-4">
            {/* Header card */}
            <div className="bg-gradient-to-r from-purple-600 to-indigo-600 rounded-2xl p-5 text-white">
                <div className="flex items-start justify-between">
                    <div>
                        <div className="flex items-center gap-2 mb-2 flex-wrap">
                            <span className="text-xs font-bold bg-white/20 px-2.5 py-0.5 rounded-full">Week {weekNumber}</span>
                            <span className="text-xs font-bold bg-white/20 px-2.5 py-0.5 rounded-full">Day {dayOrdinal} of {totalDays}</span>
                            <span className="text-xs font-bold bg-white/20 px-2.5 py-0.5 rounded-full">{dayName}</span>
                        </div>
                        <h3 className="text-lg font-bold leading-snug">{daySummary.title}</h3>
                        <p className="text-sm text-white/80 mt-1">{course.code} &mdash; {course.title}</p>
                    </div>
                    <span className="text-3xl">🧠</span>
                </div>
                <p className="text-xs text-white/60 mt-3">
                    AI summary for {effectiveDate.toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'short', year: 'numeric' })}
                </p>
            </div>

            {/* Summary content */}
            {daySummary.summary && (
                <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
                    <h4 className="text-sm font-bold text-gray-700 mb-3 flex items-center gap-2">
                        <span className="w-6 h-6 bg-purple-100 text-purple-600 rounded-full flex items-center justify-center text-xs font-black">{dayOrdinal}</span>
                        Today&apos;s Lecture Summary
                    </h4>
                    <p className="text-gray-700 leading-relaxed whitespace-pre-wrap text-sm">{daySummary.summary}</p>
                </div>
            )}

            {/* Key topics */}
            {keyTopics.length > 0 && (
                <div className="bg-indigo-50 rounded-2xl p-5 border border-indigo-100">
                    <h4 className="text-sm font-bold text-indigo-800 mb-3">Key Topics &mdash; {dayName}</h4>
                    <div className="flex flex-wrap gap-2">
                        {keyTopics.map((topic: string, i: number) => (
                            <span key={i} className="px-3 py-1.5 bg-white text-indigo-700 text-xs font-semibold rounded-lg border border-indigo-200 shadow-sm">
                                #{topic}
                            </span>
                        ))}
                    </div>
                </div>
            )}

            {/* Preparation notes on last day */}
            {isLastDay && daySummary.preparationNotes && (
                <div className="bg-amber-50 rounded-2xl p-5 border border-amber-100">
                    <h4 className="text-sm font-bold text-amber-800 mb-2">Preparation for Next Week</h4>
                    <p className="text-amber-800 text-sm italic leading-relaxed">{daySummary.preparationNotes}</p>
                </div>
            )}

            {/* All available day summaries */}
            {(summaries || []).length > 0 && (
                <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
                    <h4 className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-3">Available Summaries</h4>
                    <div className="flex flex-wrap gap-1.5">
                        {(summaries as any[]).map((s: any) => (
                            <span
                                key={s.id}
                                className={`px-2.5 py-1 rounded-full text-xs font-bold ${
                                    s.weekNumber === weekNumber && s.dayNumber === dayOrdinal
                                        ? 'bg-purple-600 text-white'
                                        : 'bg-gray-100 text-gray-600'
                                }`}
                            >
                                W{s.weekNumber}D{s.dayNumber}
                            </span>
                        ))}
                    </div>
                </div>
            )}

            {/* AI Course Chatbot */}
            <SummaryChatbot summaryId={daySummary.id} courseCode={course.code} summaryTitle={daySummary.title} />
        </div>
    );
}

// ─── AI Course Chatbot ─────────────────────────────────────────────
interface ChatMsg { role: 'user' | 'assistant' | 'quiz'; content: string; quizData?: QuizQ[]; }
interface QuizQ { question: string; options: string[]; correctIndex: number; explanation: string; }

function SummaryChatbot({ summaryId, courseCode, summaryTitle }: { summaryId: number; courseCode: string; summaryTitle: string }) {
    const [isOpen, setIsOpen] = useState(false);
    const [msgs, setMsgs] = useState<ChatMsg[]>([
        { role: 'assistant', content: `Hi! I'm your AI tutor for ${courseCode}. Ask me anything about "${summaryTitle}" or click Quiz to test yourself! 🎓` }
    ]);
    const [input, setInput] = useState('');
    const [loading, setLoading] = useState(false);
    const [qAnswers, setQAnswers] = useState<Record<number, number>>({});
    const [qDone, setQDone] = useState<Record<number, boolean>>({});
    const endRef = useRef<HTMLDivElement>(null);

    useEffect(() => { endRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [msgs, isOpen]);

    const send = async () => {
        if (!input.trim() || loading) return;
        const q = input.trim(); setInput('');
        setMsgs(p => [...p, { role: 'user', content: q }]);
        setLoading(true);
        try {
            const d = await studentApi.askAI(summaryId, q);
            setMsgs(p => [...p, { role: 'assistant', content: d.response || 'Sorry, I could not generate a response.' }]);
        } catch { setMsgs(p => [...p, { role: 'assistant', content: '⚠️ Failed to get a response. Please try again.' }]); }
        setLoading(false);
    };

    const genQuiz = async () => {
        if (loading) return;
        setLoading(true);
        setMsgs(p => [...p, { role: 'user', content: '📝 Generate a quiz on this topic' }]);
        try {
            const token = localStorage.getItem('studentToken');
            const res = await fetch(`${API_BASE_URL}/chat/quiz`, {
                method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
                body: JSON.stringify({ summaryId, questionCount: 5 })
            });
            const data = await res.json();
            if (data.quiz) {
                const parsed: QuizQ[] = JSON.parse(data.quiz);
                setMsgs(p => [...p, { role: 'quiz', content: `Here's a ${parsed.length}-question quiz! Select your answers.`, quizData: parsed }]);
            } else { setMsgs(p => [...p, { role: 'assistant', content: '⚠️ Could not generate quiz. Try again.' }]); }
        } catch { setMsgs(p => [...p, { role: 'assistant', content: '⚠️ Failed to generate quiz. AI may be unavailable.' }]); }
        setLoading(false);
    };

    const pickAnswer = (mi: number, qi: number, oi: number) => {
        const k = mi * 100 + qi;
        if (qDone[k]) return;
        setQAnswers(p => ({ ...p, [k]: oi }));
        setQDone(p => ({ ...p, [k]: true }));
    };

    if (!isOpen) return (
        <div className="fixed bottom-6 right-6 z-50">
            <button onClick={() => setIsOpen(true)}
                className="bg-gradient-to-r from-purple-600 to-indigo-600 text-white rounded-full p-4 flex items-center gap-3 hover:from-purple-700 hover:to-indigo-700 transition-all duration-300 shadow-2xl hover:shadow-purple-500/50 hover:-translate-y-1 group">
                <span className="text-2xl group-hover:scale-110 transition-transform">🤖</span>
                <span className="font-bold pr-2 hidden sm:block">Ask AI Assistant</span>
            </button>
        </div>
    );

    return (
        <div className="fixed bottom-6 right-6 z-50 w-[90vw] sm:w-[420px] bg-white rounded-3xl shadow-2xl shadow-purple-500/20 border border-gray-200/60 overflow-hidden flex flex-col max-h-[85vh] animate-in slide-in-from-bottom-5">
            {/* Header */}
            <div className="bg-gradient-to-r from-purple-600 to-indigo-600 text-white p-4 flex items-center gap-3 cursor-pointer" onClick={() => setIsOpen(false)}>
                <span className="w-9 h-9 bg-white/20 rounded-full flex items-center justify-center text-lg">🤖</span>
                <div className="flex-1">
                    <div className="font-bold text-sm">AI Course Assistant — {courseCode}</div>
                    <div className="text-xs text-white/70">Ask me anything about this lecture</div>
                </div>
                <svg className="w-5 h-5 text-white/60" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" /></svg>
            </div>

            {/* Messages */}
            <div className="h-80 overflow-y-auto p-4 space-y-3 bg-gray-50/50">
                {msgs.map((m, i) => (
                    <div key={i}>
                        {m.role === 'user' ? (
                            <div className="flex justify-end"><div className="bg-purple-600 text-white rounded-2xl rounded-br-md px-4 py-2.5 max-w-[80%] text-sm">{m.content}</div></div>
                        ) : m.role === 'quiz' && m.quizData ? (
                            <div className="space-y-3">
                                <div className="flex items-start gap-2">
                                    <span className="w-7 h-7 bg-indigo-100 text-indigo-600 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0 mt-0.5">AI</span>
                                    <div className="bg-white rounded-2xl rounded-bl-md px-4 py-2.5 max-w-[90%] text-sm text-gray-800 border border-gray-100">{m.content}</div>
                                </div>
                                {m.quizData.map((q, qi) => {
                                    const k = i * 100 + qi; const done = qDone[k]; const sel = qAnswers[k]; const ok = sel === q.correctIndex;
                                    return (
                                        <div key={qi} className="bg-white rounded-xl border border-gray-200 p-4 ml-9">
                                            <p className="text-sm font-semibold text-gray-800 mb-2"><span className="text-purple-600 mr-1">Q{qi + 1}.</span>{q.question}</p>
                                            <div className="space-y-1.5">
                                                {q.options.map((o, oi) => {
                                                    let cls = 'bg-gray-50 border-gray-200 text-gray-700 hover:bg-purple-50 hover:border-purple-300 cursor-pointer';
                                                    if (done) {
                                                        if (oi === q.correctIndex) cls = 'bg-green-50 border-green-400 text-green-800';
                                                        else if (oi === sel && !ok) cls = 'bg-red-50 border-red-400 text-red-800';
                                                        else cls = 'bg-gray-50 border-gray-200 text-gray-400';
                                                    }
                                                    return (
                                                        <button key={oi} onClick={() => pickAnswer(i, qi, oi)} disabled={done}
                                                            className={`w-full text-left px-3 py-2 rounded-lg border text-xs font-medium transition-all ${cls}`}>
                                                            {o}{done && oi === q.correctIndex && ' ✅'}{done && oi === sel && !ok && ' ❌'}
                                                        </button>
                                                    );
                                                })}
                                            </div>
                                            {done && <p className={`text-xs mt-2 p-2 rounded-lg ${ok ? 'bg-green-50 text-green-700' : 'bg-amber-50 text-amber-700'}`}>{ok ? '🎉 Correct! ' : '💡 '}{q.explanation}</p>}
                                        </div>
                                    );
                                })}
                                {(() => {
                                    const tot = m.quizData!.length;
                                    const answered = m.quizData!.filter((_, qi) => qDone[i * 100 + qi]).length;
                                    if (answered === tot && tot > 0) {
                                        const correct = m.quizData!.filter((q, qi) => qAnswers[i * 100 + qi] === q.correctIndex).length;
                                        const pct = Math.round((correct / tot) * 100);
                                        return (
                                            <div className={`ml-9 p-3 rounded-xl border text-center text-sm font-bold ${pct >= 80 ? 'bg-green-50 border-green-200 text-green-700' : pct >= 50 ? 'bg-amber-50 border-amber-200 text-amber-700' : 'bg-red-50 border-red-200 text-red-700'}`}>
                                                {pct >= 80 ? '🏆' : pct >= 50 ? '👍' : '📚'} Score: {correct}/{tot} ({pct}%)
                                                {pct >= 80 ? ' — Excellent!' : pct >= 50 ? ' — Good job!' : ' — Review and try again!'}
                                            </div>
                                        );
                                    }
                                    return null;
                                })()}
                            </div>
                        ) : (
                            <div className="flex items-start gap-2">
                                <span className="w-7 h-7 bg-indigo-100 text-indigo-600 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0 mt-0.5">AI</span>
                                <div className="bg-white rounded-2xl rounded-bl-md px-4 py-2.5 max-w-[80%] text-sm text-gray-800 border border-gray-100 whitespace-pre-wrap">{m.content}</div>
                            </div>
                        )}
                    </div>
                ))}
                {loading && (
                    <div className="flex items-start gap-2">
                        <span className="w-7 h-7 bg-indigo-100 text-indigo-600 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">AI</span>
                        <div className="bg-white rounded-2xl rounded-bl-md px-4 py-3 border border-gray-100">
                            <div className="flex gap-1"><span className="w-2 h-2 bg-purple-400 rounded-full animate-bounce" style={{animationDelay:'0s'}} /><span className="w-2 h-2 bg-purple-400 rounded-full animate-bounce" style={{animationDelay:'0.15s'}} /><span className="w-2 h-2 bg-purple-400 rounded-full animate-bounce" style={{animationDelay:'0.3s'}} /></div>
                        </div>
                    </div>
                )}
                <div ref={endRef} />
            </div>

            {/* Input area */}
            <div className="border-t border-gray-200 p-3 flex gap-2 bg-white">
                <button onClick={genQuiz} disabled={loading}
                    className="px-3 py-2 bg-indigo-100 text-indigo-700 rounded-xl text-xs font-bold hover:bg-indigo-200 transition-colors disabled:opacity-50 flex items-center gap-1 flex-shrink-0"
                    title="Generate a quiz">📝 Quiz</button>
                <input value={input} onChange={e => setInput(e.target.value)}
                    onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); } }}
                    placeholder="Ask about this topic..."
                    className="flex-1 px-3 py-2 bg-gray-100 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-purple-300 transition-all" disabled={loading} />
                <button onClick={send} disabled={loading || !input.trim()}
                    className="w-9 h-9 bg-purple-600 text-white rounded-xl flex items-center justify-center hover:bg-purple-700 transition-colors disabled:opacity-50 flex-shrink-0">
                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" /></svg>
                </button>
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
                <div className="text-xs font-semibold text-[#FF6B35] bg-[#FFF3E0] border border-[#FFD0B0] px-3 py-1.5 rounded-full">
                    {summaries.length} Total Summaries
                </div>
            </div>

            {/* Course Tabs */}
            <div className="mb-6 overflow-x-auto pb-2">
                <div className="flex space-x-2 min-w-max">
                    <button
                        onClick={() => setSelectedCourseId('all')}
                        className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${selectedCourseId === 'all'
                            ? 'bg-[#FF6B35] text-white shadow-md shadow-[#FFF3E0]'
                            : 'bg-white text-gray-600 border border-gray-200 hover:border-[#FF6B35] hover:text-[#FF6B35]'
                            }`}
                    >
                        All Courses
                    </button>
                    {courses.map((course: any) => (
                        <button
                            key={course.id}
                            onClick={() => setSelectedCourseId(course.id)}
                            className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${selectedCourseId === course.id
                                ? 'bg-[#FF6B35] text-white shadow-md shadow-[#FFF3E0]'
                                : 'bg-white text-gray-600 border border-gray-200 hover:border-[#FF6B35] hover:text-[#FF6B35]'
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
                                    <tr key={`${summary.type}-${summary.id}`} className="hover:bg-[#FFF3E0]/50 transition-colors">
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="text-sm font-bold text-gray-900">{summary.courseCode}</div>
                                            <div className="text-[10px] text-gray-500 uppercase tracking-tight">{summary.courseName}</div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <span className={`px-3 py-1 text-[10px] font-black uppercase rounded-full ${summary.type === 'Weekly' ? 'bg-purple-100 text-purple-700' : 'bg-[#FFF3E0] text-[#FF6B35]'}`}>
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
                                                className="px-4 py-2 bg-[#FF6B35] text-white text-xs font-bold rounded-lg hover:bg-[#E55A2B] transform active:scale-95 transition-all shadow-sm hover:shadow-md"
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
                            className="px-4 py-2 bg-[#FF6B35] text-white rounded-lg hover:bg-[#E55A2B] transition-colors"
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
                                        className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-[#FFF3E0] file:text-[#FF6B35] hover:file:bg-[#FFF3E0]"
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
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]"
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
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]"
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
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]"
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
                                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]"
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
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]"
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
                                className="flex-1 px-4 py-2 bg-[#FF6B35] text-white rounded-lg hover:bg-[#E55A2B] disabled:opacity-50"
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


