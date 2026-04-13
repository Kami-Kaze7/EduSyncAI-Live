// @ts-nocheck
'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5152/api';

export default function CourseInfoPage() {
    const params = useParams();
    const router = useRouter();
    const courseName = decodeURIComponent(params.courseName as string);

    const [course, setCourse] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [playingVideoId, setPlayingVideoId] = useState<number | null>(null);
    const [streamUrl, setStreamUrl] = useState<string | null>(null);
    const [streamLoading, setStreamLoading] = useState(false);

    useEffect(() => {
        const fetchCourse = async () => {
            try {
                const res = await axios.get(`${API_BASE_URL}/CourseVideos/featured`);
                const found = res.data.find((c: any) => c.courseName === courseName);
                if (found) {
                    setCourse(found);
                    if (found.videos?.length > 0) {
                        setPlayingVideoId(found.videos[0].id);
                    }
                }
            } catch (e) {
                console.error('Failed to load course', e);
            } finally {
                setLoading(false);
            }
        };
        fetchCourse();
    }, [courseName]);

    // Load stream URL for Wasabi videos
    useEffect(() => {
        if (!playingVideoId) return;
        const video = course?.videos?.find((v: any) => v.id === playingVideoId);
        if (!video) return;

        if (video.isWasabiVideo) {
            setStreamLoading(true);
            axios.get(`${API_BASE_URL}/CourseVideos/${playingVideoId}/stream-url`)
                .then(res => {
                    setStreamUrl(res.data.url);
                    setStreamLoading(false);
                })
                .catch(() => setStreamLoading(false));
        } else {
            setStreamUrl(null);
        }
    }, [playingVideoId, course]);

    if (loading) {
        return (
            <div className="min-h-screen bg-[#FAFAFA] flex items-center justify-center" style={{ fontFamily: "'Inter', sans-serif" }}>
                <div className="text-center">
                    <div className="w-12 h-12 border-4 border-[#FF6B35]/20 border-t-[#FF6B35] rounded-full animate-spin mx-auto mb-4" />
                    <p className="text-gray-500 text-sm">Loading course...</p>
                </div>
            </div>
        );
    }

    if (!course) {
        return (
            <div className="min-h-screen bg-[#FAFAFA] flex items-center justify-center" style={{ fontFamily: "'Inter', sans-serif" }}>
                <div className="text-center">
                    <div className="text-6xl mb-4">📚</div>
                    <h2 className="text-2xl font-bold text-gray-900 mb-2">Course Not Found</h2>
                    <p className="text-gray-500 mb-6">This course may no longer be available.</p>
                    <Link href="/" className="px-6 py-3 bg-[#FF6B35] text-white font-semibold rounded-xl hover:bg-[#e55a2b] transition-colors">
                        ← Back to Home
                    </Link>
                </div>
            </div>
        );
    }

    const activeVideo = course.videos?.find((v: any) => v.id === playingVideoId) || course.videos?.[0];

    return (
        <div className="min-h-screen bg-[#FAFAFA]" style={{ fontFamily: "'Inter', sans-serif" }}>
            {/* Navbar */}
            <nav className="bg-white/95 backdrop-blur-md shadow-sm sticky top-0 z-50">
                <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
                    <div className="flex items-center gap-4">
                        <Link href="/" className="flex items-center gap-2 group">
                            <div className="w-9 h-9 rounded-lg bg-[#FF6B35] flex items-center justify-center text-white text-sm font-bold shadow-md">
                                E
                            </div>
                            <span className="text-lg font-bold text-[#1A1A2E]">EduSync AI</span>
                        </Link>
                        <div className="hidden sm:flex items-center gap-1 text-sm text-gray-400">
                            <span>/</span>
                            <Link href="/#courses" className="hover:text-gray-600 transition-colors">Courses</Link>
                            <span>/</span>
                            <span className="text-gray-700 font-medium truncate max-w-[200px]">{course.courseName}</span>
                        </div>
                    </div>
                    <Link href="/student/login" className="px-5 py-2.5 bg-[#1A1A2E] text-white text-sm font-semibold rounded-lg hover:bg-[#2d2d4e] transition-colors shadow-md">
                        Sign In
                    </Link>
                </div>
            </nav>

            <main className="max-w-7xl mx-auto px-6 py-8">
                {/* Course Hero */}
                <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden mb-8">
                    <div className="h-48 bg-gradient-to-br from-[#FF6B35] via-[#FF8F5E] to-[#FFA07A] flex items-end p-8 relative overflow-hidden">
                        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'url("data:image/svg+xml,%3Csvg width=\'60\' height=\'60\' viewBox=\'0 0 60 60\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cg fill=\'none\' fill-rule=\'evenodd\'%3E%3Cg fill=\'%23ffffff\' fill-opacity=\'0.4\'%3E%3Cpath d=\'M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z\'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")' }} />
                        <div className="relative z-10">
                            <div className="flex items-center gap-2 mb-2">
                                <span className="px-3 py-1 bg-white/20 backdrop-blur-sm text-white text-xs font-semibold rounded-full">{course.facultyName}</span>
                                <span className="px-3 py-1 bg-white/20 backdrop-blur-sm text-white text-xs font-semibold rounded-full">{course.departmentName}</span>
                            </div>
                            <h1 className="text-3xl font-extrabold text-white mb-1">{course.courseName}</h1>
                            <div className="flex items-center gap-4 text-white/80 text-sm">
                                <span>📹 {course.videoCount} video{course.videoCount !== 1 ? 's' : ''}</span>
                                {course.price > 0 && <span className="bg-white/20 backdrop-blur-sm px-3 py-1 rounded-full font-bold text-white">₦{course.price.toLocaleString()}</span>}
                            </div>
                        </div>
                    </div>
                </div>

                {/* ── Main Layout Grid (matches student dashboard) ── */}
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
                    {/* Left Column — Video + Playlist/Details (2/3 width) */}
                    <div className="lg:col-span-2 space-y-5">
                        {/* Video Player */}
                        <div className="aspect-video bg-black rounded-2xl overflow-hidden shadow-lg">
                            {activeVideo ? (
                                activeVideo.isWasabiVideo ? (
                                    streamLoading ? (
                                        <div className="w-full h-full flex items-center justify-center text-white text-sm animate-pulse">Loading video...</div>
                                    ) : streamUrl ? (
                                        <video key={playingVideoId} src={streamUrl} controls autoPlay className="w-full h-full" controlsList="nodownload">
                                            Your browser does not support video playback.
                                        </video>
                                    ) : (
                                        <div className="w-full h-full flex items-center justify-center text-red-400 text-sm">Failed to load video</div>
                                    )
                                ) : (
                                    <iframe src={activeVideo.videoUrl} title={activeVideo.title} className="w-full h-full border-0" allowFullScreen />
                                )
                            ) : (
                                <div className="w-full h-full flex items-center justify-center text-gray-400">
                                    <p>Select a video to play</p>
                                </div>
                            )}
                        </div>

                        {/* Playlist + Course Details — side by side under video */}
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            {/* Playlist */}
                            <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
                                <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
                                    <h4 className="font-bold text-gray-800 text-sm">Playlist</h4>
                                    <p className="text-xs text-gray-400">{course.videos?.length || 0} video{(course.videos?.length || 0) !== 1 ? 's' : ''}</p>
                                </div>
                                <div className="divide-y divide-gray-50 max-h-[300px] overflow-y-auto">
                                    {course.videos?.map((video: any, idx: number) => (
                                        <button
                                            key={video.id}
                                            onClick={() => setPlayingVideoId(video.id)}
                                            className={`w-full text-left px-4 py-3 flex items-center gap-3 hover:bg-orange-50/60 transition-colors ${
                                                activeVideo?.id === video.id ? 'bg-orange-50 border-l-4 border-[#FF6B35]' : 'border-l-4 border-transparent'
                                            }`}
                                        >
                                            <div className={`w-7 h-7 rounded-lg flex items-center justify-center text-xs font-bold shrink-0 ${
                                                activeVideo?.id === video.id
                                                    ? 'bg-[#FF6B35] text-white shadow-md shadow-[#FF6B35]/30'
                                                    : 'bg-gray-100 text-gray-500'
                                            }`}>
                                                {activeVideo?.id === video.id ? '▶' : idx + 1}
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <p className={`text-sm font-medium truncate ${activeVideo?.id === video.id ? 'text-[#FF6B35]' : 'text-gray-800'}`}>
                                                    {video.title}
                                                </p>
                                                {video.duration && (
                                                    <span className="text-xs text-gray-400 mt-0.5 block">{video.duration}</span>
                                                )}
                                            </div>
                                            {activeVideo?.id === video.id && (
                                                <span className="text-[10px] font-bold text-[#FF6B35] bg-orange-100 px-2 py-0.5 rounded-full shrink-0">▶</span>
                                            )}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            {/* Course Details */}
                            <div className="bg-gradient-to-br from-gray-50 to-white rounded-2xl border border-gray-100 shadow-sm p-5">
                                <h4 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">Course Details</h4>
                                <div className="space-y-3">
                                    <div className="flex items-center gap-3">
                                        <div className="w-8 h-8 rounded-lg bg-orange-100 flex items-center justify-center text-sm">🏛️</div>
                                        <div>
                                            <p className="text-[11px] text-gray-400 font-medium">Faculty</p>
                                            <p className="text-sm font-semibold text-gray-800">{course.facultyName}</p>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-3">
                                        <div className="w-8 h-8 rounded-lg bg-blue-100 flex items-center justify-center text-sm">📘</div>
                                        <div>
                                            <p className="text-[11px] text-gray-400 font-medium">Department</p>
                                            <p className="text-sm font-semibold text-gray-800">{course.departmentName}</p>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-3">
                                        <div className="w-8 h-8 rounded-lg bg-purple-100 flex items-center justify-center text-sm">📚</div>
                                        <div>
                                            <p className="text-[11px] text-gray-400 font-medium">Course</p>
                                            <p className="text-sm font-semibold text-gray-800">{course.courseName}</p>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-3">
                                        <div className="w-8 h-8 rounded-lg bg-emerald-100 flex items-center justify-center text-sm">🎬</div>
                                        <div>
                                            <p className="text-[11px] text-gray-400 font-medium">Videos</p>
                                            <p className="text-sm font-semibold text-gray-800">{course.videoCount} video{course.videoCount !== 1 ? 's' : ''}</p>
                                        </div>
                                    </div>
                                    {course.price > 0 && (
                                        <div className="flex items-center gap-3">
                                            <div className="w-8 h-8 rounded-lg bg-green-100 flex items-center justify-center text-sm">💰</div>
                                            <div>
                                                <p className="text-[11px] text-gray-400 font-medium">Price</p>
                                                <p className="text-sm font-bold text-emerald-700">₦{course.price.toLocaleString()}</p>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>

                        {/* What You'll Learn Card */}
                        {course.whatYoullLearn && (
                            <div className="bg-gradient-to-br from-emerald-50 to-white rounded-2xl border border-emerald-100 shadow-sm p-5">
                                <h4 className="text-xs font-semibold text-emerald-600 uppercase tracking-wider mb-3 flex items-center gap-2">
                                    <span className="w-5 h-5 rounded-full bg-emerald-500 text-white flex items-center justify-center text-[10px]">✓</span>
                                    What You'll Learn
                                </h4>
                                <ul className="space-y-2">
                                    {course.whatYoullLearn.split('\n').filter((line: string) => line.trim()).map((point: string, idx: number) => (
                                        <li key={idx} className="flex items-start gap-2.5">
                                            <span className="mt-0.5 w-4 h-4 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center text-[10px] shrink-0">✓</span>
                                            <span className="text-sm text-gray-700">{point.trim()}</span>
                                        </li>
                                    ))}
                                </ul>
                            </div>
                        )}
                    </div>

                    {/* Right Column — Description Panel (1/3 width) */}
                    <div className="space-y-5">
                        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-5 h-fit">
                            <h3 className="text-lg font-bold text-gray-900 leading-snug">{activeVideo?.title || 'No video selected'}</h3>
                            {activeVideo?.duration && (
                                <div className="flex items-center gap-2 mt-2">
                                    <span className="inline-flex items-center gap-1 text-xs font-medium text-gray-500 bg-gray-100 px-2.5 py-1 rounded-full">
                                        ⏱️ {activeVideo.duration}
                                    </span>
                                </div>
                            )}
                            {activeVideo?.description && (
                                <div className="mt-4 pt-4 border-t border-gray-100">
                                    <h4 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">Description</h4>
                                    <p className="text-sm text-gray-600 leading-relaxed whitespace-pre-wrap">{activeVideo.description}</p>
                                </div>
                            )}
                        </div>

                        {/* Want Full Access CTA */}
                        <div className="bg-gradient-to-br from-[#1A1A2E] to-[#2d2d4e] rounded-2xl p-6 text-center">
                            <h3 className="text-lg font-bold text-white mb-2">Want Full Access?</h3>
                            <p className="text-white/50 text-sm mb-4">Sign up to track progress and access all courses.</p>
                            <Link href="/student/login" className="block w-full px-6 py-3 bg-[#FF6B35] text-white font-bold text-sm rounded-xl hover:bg-[#e55a2b] transition-colors shadow-lg shadow-[#FF6B35]/30">
                                Get Started Free
                            </Link>
                        </div>
                    </div>
                </div>
            </main>

            {/* Footer */}
            <footer className="bg-[#1A1A2E] text-white py-10 px-6 mt-16">
                <div className="max-w-7xl mx-auto flex flex-col md:flex-row items-center justify-between gap-4">
                    <div className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-lg bg-[#FF6B35] flex items-center justify-center text-white text-xs font-bold">E</div>
                        <span className="font-bold">EduSync AI</span>
                    </div>
                    <p className="text-xs text-white/30">© {new Date().getFullYear()} EduSync AI. All rights reserved.</p>
                </div>
            </footer>
        </div>
    );
}
