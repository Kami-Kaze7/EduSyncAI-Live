// @ts-nocheck
'use client';

import { useState } from 'react';
import { studentApi } from '@/lib/studentApi';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';

export default function CourseVideosTab() {
    const [activeFaculty, setActiveFaculty] = useState<string>('all');
    const [selectedCourse, setSelectedCourse] = useState<any>(null);
    const [playingVideoId, setPlayingVideoId] = useState<number | null>(null);

    const { data: faculties, isLoading } = useQuery({
        queryKey: ['student-all-videos'],
        queryFn: studentApi.getAllCourseVideos
    });

    const facultyNames: string[] = faculties?.map((f: any) => f.facultyName).filter((n: string) => n) || [];

    const displayedFaculties = activeFaculty === 'all'
        ? faculties || []
        : (faculties || []).filter((f: any) => f.facultyName === activeFaculty);

    const handleDownload = async (videoId: number) => {
        try {
            const data = await studentApi.getVideoDownloadUrl(videoId);
            window.open(data.url, '_blank');
        } catch {
            toast.error('Failed to get download link');
        }
    };

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-20">
                <div className="text-center">
                    <div className="w-12 h-12 border-4 border-amber-200 border-t-amber-500 rounded-full animate-spin mx-auto mb-4" />
                    <p className="text-gray-500 text-sm">Loading course videos...</p>
                </div>
            </div>
        );
    }

    // If a course is selected, show full video player view
    if (selectedCourse) {
        return (
            <CoursePlayerView
                course={selectedCourse}
                onBack={() => { setSelectedCourse(null); setPlayingVideoId(null); }}
                playingVideoId={playingVideoId}
                setPlayingVideoId={setPlayingVideoId}
                onDownload={handleDownload}
            />
        );
    }

    // Helper: get thumbnail for a course from first video that has one
    const getCourseThumbnail = (course: any) => {
        const thumb = course.videos?.find((v: any) => v.thumbnailUrl)?.thumbnailUrl;
        return thumb || null;
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div>
                <h2 className="text-xl font-semibold text-gray-900">Course Videos</h2>
                <p className="text-sm text-gray-400 mt-0.5">Browse available course recordings and lectures.</p>
            </div>

            {/* Faculty Tabs */}
            <div className="flex items-center gap-1 bg-gray-100 rounded-xl p-1 overflow-x-auto">
                <button
                    onClick={() => setActiveFaculty('all')}
                    className={`px-4 py-2 rounded-lg text-sm font-bold whitespace-nowrap transition-all ${
                        activeFaculty === 'all' ? 'bg-white shadow-sm text-gray-900' : 'text-gray-500 hover:text-gray-700'
                    }`}
                >
                    All Faculties
                </button>
                {facultyNames.map((name: string) => (
                    <button
                        key={name}
                        onClick={() => setActiveFaculty(name)}
                        className={`px-4 py-2 rounded-lg text-sm font-bold whitespace-nowrap transition-all ${
                            activeFaculty === name ? 'bg-white shadow-sm text-amber-700' : 'text-gray-500 hover:text-gray-700'
                        }`}
                    >
                        {name}
                    </button>
                ))}
            </div>

            {/* Course Grid */}
            {displayedFaculties.length === 0 ? (
                <div className="bg-white rounded-2xl border border-gray-100 p-16 text-center">
                    <div className="text-5xl mb-4">🎬</div>
                    <h3 className="text-lg font-bold text-gray-900 mb-2">No Videos Available</h3>
                    <p className="text-sm text-gray-400">Course videos will appear here once uploaded by the admin.</p>
                </div>
            ) : (
                displayedFaculties.map((faculty: any) => (
                    <div key={faculty.facultyName} className="space-y-4">
                        {activeFaculty === 'all' && (
                            <div className="flex items-center gap-3 px-1">
                                <span className="w-8 h-8 rounded-lg bg-gradient-to-br from-amber-400 to-amber-600 text-white flex items-center justify-center text-sm">🏛️</span>
                                <h3 className="text-base font-bold text-gray-900">{faculty.facultyName}</h3>
                            </div>
                        )}

                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                            {faculty.courses.map((course: any) => {
                                const thumbnail = getCourseThumbnail(course);
                                return (
                                    <div
                                        key={`${faculty.facultyName}-${course.courseName}`}
                                        onClick={() => setSelectedCourse({ ...course, facultyName: faculty.facultyName })}
                                        className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden hover:shadow-lg hover:border-amber-200 transition-all cursor-pointer group"
                                    >
                                        {/* Course Card Thumbnail Area */}
                                        <div
                                            className="h-40 bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center relative overflow-hidden"
                                            style={thumbnail ? { backgroundImage: `url(${thumbnail})`, backgroundSize: 'cover', backgroundPosition: 'center' } : {}}
                                        >
                                            {!thumbnail && (
                                                <div className="text-5xl opacity-30 group-hover:opacity-50 transition-opacity">📚</div>
                                            )}
                                            {/* Hover overlay */}
                                            <div className="absolute inset-0 bg-black/0 group-hover:bg-black/20 transition-all flex items-center justify-center">
                                                <div className="w-12 h-12 rounded-full bg-white/90 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity shadow-lg">
                                                    <svg className="w-5 h-5 text-amber-600 ml-0.5" fill="currentColor" viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
                                                </div>
                                            </div>
                                            <div className="absolute bottom-2 right-2 bg-black/60 text-white text-xs font-bold px-2 py-1 rounded-lg backdrop-blur-sm">
                                                {course.videos.length} video{course.videos.length !== 1 ? 's' : ''}
                                            </div>
                                            {course.price > 0 && (
                                                <div className="absolute top-2 right-2 bg-emerald-500 text-white text-xs font-bold px-2.5 py-1 rounded-lg shadow">
                                                    ₦{course.price.toLocaleString()}
                                                </div>
                                            )}
                                        </div>

                                        {/* Course Card Info */}
                                        <div className="p-4">
                                            <h4 className="font-bold text-gray-900 truncate">{course.courseName}</h4>
                                            <p className="text-xs text-gray-500 mt-0.5">{course.departmentName}</p>
                                            {course.description && (
                                                <p className="text-xs text-gray-400 mt-2 line-clamp-2">{course.description}</p>
                                            )}
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                ))
            )}
        </div>
    );
}

// ═══════════════════════════════════════════
// Course Player View (when a course is clicked)
// ═══════════════════════════════════════════
function CoursePlayerView({ course, onBack, playingVideoId, setPlayingVideoId, onDownload }: any) {
    const activeVideo = course.videos.find((v: any) => v.id === playingVideoId) || course.videos[0];

    return (
        <div className="space-y-5">
            {/* Back Button & Course Title */}
            <div className="flex items-center gap-3">
                <button onClick={onBack} className="text-gray-500 hover:text-gray-700 bg-gray-100 hover:bg-gray-200 p-2 rounded-lg transition-colors">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 19l-7-7 7-7" /></svg>
                </button>
                <h2 className="text-lg font-bold text-gray-900">{course.courseName}</h2>
            </div>

            {/* ── Main Layout Grid ── */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
                {/* Left Column — Video + Playlist/Details (2/3 width) */}
                <div className="lg:col-span-2 space-y-5">
                    {/* Video Player */}
                    <div className="aspect-video bg-black rounded-2xl overflow-hidden shadow-lg">
                        {activeVideo ? (
                            activeVideo.isWasabiVideo ? (
                                <WasabiVideoPlayer videoId={activeVideo.id} key={activeVideo.id} />
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
                                <p className="text-xs text-gray-400">{course.videos.length} video{course.videos.length !== 1 ? 's' : ''}</p>
                            </div>
                            <div className="divide-y divide-gray-50 max-h-[300px] overflow-y-auto">
                                {course.videos.map((video: any, idx: number) => (
                                    <button
                                        key={video.id}
                                        onClick={() => setPlayingVideoId(video.id)}
                                        className={`w-full text-left px-4 py-3 flex items-center gap-3 hover:bg-amber-50/60 transition-colors ${
                                            activeVideo?.id === video.id ? 'bg-amber-50 border-l-4 border-amber-500' : 'border-l-4 border-transparent'
                                        }`}
                                    >
                                        <div className={`w-7 h-7 rounded-lg flex items-center justify-center text-xs font-bold shrink-0 ${
                                            activeVideo?.id === video.id
                                                ? 'bg-amber-500 text-white shadow-md shadow-amber-200'
                                                : 'bg-gray-100 text-gray-500'
                                        }`}>
                                            {activeVideo?.id === video.id ? '▶' : idx + 1}
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <p className={`text-sm font-medium truncate ${activeVideo?.id === video.id ? 'text-amber-700' : 'text-gray-800'}`}>
                                                {video.title}
                                            </p>
                                            {video.duration && (
                                                <span className="text-xs text-gray-400 mt-0.5 block">{video.duration}</span>
                                            )}
                                        </div>
                                        {activeVideo?.id === video.id && (
                                            <span className="text-[10px] font-bold text-amber-600 bg-amber-100 px-2 py-0.5 rounded-full shrink-0">▶</span>
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
                                    <div className="w-8 h-8 rounded-lg bg-amber-100 flex items-center justify-center text-sm">🏛️</div>
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
                                        <p className="text-sm font-semibold text-gray-800">{course.videos.length} video{course.videos.length !== 1 ? 's' : ''}</p>
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
                <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-5 h-fit">
                    <h3 className="text-lg font-bold text-gray-900 leading-snug">{activeVideo?.title || 'No video selected'}</h3>
                    {activeVideo?.duration && (
                        <div className="flex items-center gap-2 mt-2">
                            <span className="inline-flex items-center gap-1 text-xs font-medium text-gray-500 bg-gray-100 px-2.5 py-1 rounded-full">
                                ⏱️ {activeVideo.duration}
                            </span>
                            {activeVideo?.isWasabiVideo && (
                                <button onClick={() => onDownload(activeVideo.id)} className="inline-flex items-center gap-1 text-xs font-medium text-blue-600 bg-blue-50 px-2.5 py-1 rounded-full hover:bg-blue-100 transition-colors">
                                    ⬇️ Download
                                </button>
                            )}
                        </div>
                    )}
                    {activeVideo?.description && (
                        <div className="mt-4 pt-4 border-t border-gray-100">
                            <h4 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">Description</h4>
                            <p className="text-sm text-gray-600 leading-relaxed whitespace-pre-wrap">{activeVideo.description}</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

// ═══════════════════════════════════════════
// Wasabi Video Player
// ═══════════════════════════════════════════
function WasabiVideoPlayer({ videoId }: { videoId: number }) {
    const [streamUrl, setStreamUrl] = useState<string | null>(null);
    const [loading, setLoading] = useState(true);

    useState(() => {
        studentApi.getVideoStreamUrl(videoId).then(data => {
            setStreamUrl(data.url);
            setLoading(false);
        }).catch(() => setLoading(false));
    });

    if (loading) return <div className="w-full h-full flex items-center justify-center text-white text-sm animate-pulse">Loading video...</div>;
    if (!streamUrl) return <div className="w-full h-full flex items-center justify-center text-red-400 text-sm">Failed to load video</div>;

    return (
        <video src={streamUrl} controls autoPlay className="w-full h-full" controlsList="nodownload">
            Your browser does not support video playback.
        </video>
    );
}
