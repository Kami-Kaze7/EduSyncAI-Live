// @ts-nocheck
'use client';

import { useState, useEffect } from 'react';
import { studentApi } from '@/lib/studentApi';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';

function formatFileSize(bytes: number): string {
    if (!bytes) return '';
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
}

function WasabiVideoPlayer({ videoId }: { videoId: number }) {
    const [streamUrl, setStreamUrl] = useState<string | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(false);

    useEffect(() => {
        setLoading(true);
        setError(false);
        studentApi.getVideoStreamUrl(videoId)
            .then(data => {
                setStreamUrl(data.url);
                setLoading(false);
            })
            .catch(() => {
                setError(true);
                setLoading(false);
            });
    }, [videoId]);

    if (loading) {
        return (
            <div className="w-full h-full flex flex-col items-center justify-center bg-gray-950 text-white">
                <div className="w-10 h-10 border-3 border-blue-400 border-t-transparent rounded-full animate-spin mb-3" />
                <span className="text-sm text-gray-400">Loading video stream...</span>
            </div>
        );
    }

    if (error || !streamUrl) {
        return (
            <div className="w-full h-full flex flex-col items-center justify-center bg-gray-950 text-red-400">
                <span className="text-3xl mb-2">⚠️</span>
                <span className="text-sm font-medium">Failed to load video stream</span>
                <button 
                    onClick={() => { setLoading(true); setError(false); studentApi.getVideoStreamUrl(videoId).then(d => { setStreamUrl(d.url); setLoading(false); }).catch(() => { setError(true); setLoading(false); }); }}
                    className="mt-3 text-xs text-blue-400 hover:text-blue-300 underline"
                >
                    Retry
                </button>
            </div>
        );
    }

    return (
        <video 
            src={streamUrl} 
            controls 
            autoPlay
            className="w-full h-full"
            style={{ backgroundColor: '#000' }}
            onError={() => setError(true)}
        >
            Your browser does not support video playback.
        </video>
    );
}

export default function CourseVideosTab() {
    const [selectedCourse, setSelectedCourse] = useState<any>(null);
    const [activeVideo, setActiveVideo] = useState<any>(null);
    const [downloading, setDownloading] = useState(false);

    // Fetch enrolled courses
    const { data: myCourses, isLoading: loadingCourses } = useQuery({
        queryKey: ['my-courses'],
        queryFn: studentApi.getMyCourses,
    });

    // Fetch videos for the selected course
    const { data: videos, isLoading: loadingVideos } = useQuery({
        queryKey: ['course-videos', selectedCourse?.id || selectedCourse?.Id],
        queryFn: () => studentApi.getCourseVideos(selectedCourse.id || selectedCourse.Id),
        enabled: !!selectedCourse
    });

    useEffect(() => {
        if (myCourses?.length > 0 && !selectedCourse) {
            setSelectedCourse(myCourses[0]);
        }
    }, [myCourses]);

    useEffect(() => {
        if (videos?.length > 0) {
            setActiveVideo(videos[0]);
        } else {
            setActiveVideo(null);
        }
    }, [videos, selectedCourse]);

    const handleDownload = async (video: any) => {
        const videoId = video.id || video.Id;
        setDownloading(true);
        try {
            const data = await studentApi.getVideoDownloadUrl(videoId);
            // Open download URL in a new tab
            window.open(data.url, '_blank');
            toast.success('Download started!');
        } catch {
            toast.error('Download not available for this video');
        } finally {
            setDownloading(false);
        }
    };

    if (loadingCourses) return <div className="p-10 text-center text-gray-500 animate-pulse">Loading Your Courses...</div>;

    if (!myCourses || myCourses.length === 0) {
        return (
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-16 text-center">
                <div className="text-5xl mb-4">🎓</div>
                <h3 className="text-xl font-bold text-gray-900 mb-2">No Enrolled Courses</h3>
                <p className="text-gray-500">You must be enrolled in courses to watch their videos.</p>
            </div>
        );
    }

    const isWasabi = activeVideo?.isWasabiVideo || activeVideo?.IsWasabiVideo;
    const activeVideoId = activeVideo?.id || activeVideo?.Id;

    return (
        <div className="flex flex-col lg:flex-row gap-6 min-h-[700px]">
            {/* Sidebar / Playlist */}
            <div className="w-full lg:w-1/3 xl:w-1/4 flex flex-col gap-4">
                <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
                    <label className="block text-xs font-bold text-gray-500 uppercase tracking-wider mb-2">Select Course</label>
                    <select
                        className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm font-semibold focus:ring-2 focus:ring-blue-300"
                        value={selectedCourse?.id || selectedCourse?.Id || ''}
                        onChange={(e) => {
                            const val = e.target.value;
                            const c = myCourses.find((c:any) => (c.id || c.Id).toString() === val);
                            if(c) setSelectedCourse(c);
                        }}
                    >
                        {myCourses.map((c: any) => (
                            <option key={c.id || c.Id} value={c.id || c.Id}>{c.courseCode || c.CourseCode} - {c.courseName || c.courseTitle || c.CourseTitle || c.CourseName}</option>
                        ))}
                    </select>
                </div>

                <div className="bg-white rounded-lg shadow-sm border border-gray-200 flex-1 flex flex-col overflow-hidden">
                    <div className="p-4 border-b border-gray-200 bg-gray-50">
                        <h3 className="font-bold text-gray-800">Course Content</h3>
                        <p className="text-xs text-gray-500 mt-1">{videos?.length || 0} Lessons</p>
                    </div>
                    
                    <div className="flex-1 overflow-y-auto p-3 space-y-2 max-h-[500px]">
                        {loadingVideos ? (
                            <p className="text-center text-sm text-gray-400 py-4">Loading videos...</p>
                        ) : videos?.length === 0 ? (
                            <p className="text-center text-sm text-gray-400 py-4 italic">No videos scheduled for this course yet.</p>
                        ) : (
                            videos?.map((v: any, idx: number) => {
                                const vId = v.id || v.Id;
                                const vTitle = v.title || v.Title;
                                const vDesc = v.description || v.Description;
                                const vIsWasabi = v.isWasabiVideo || v.IsWasabiVideo;
                                const vSize = v.fileSizeBytes || v.FileSizeBytes;
                                const isActive = activeVideoId === vId;
                                return (
                                    <button
                                        key={vId}
                                        onClick={() => setActiveVideo(v)}
                                        className={`w-full text-left px-4 py-3 rounded-lg transition-all ${isActive ? 'bg-blue-50 border border-blue-200' : 'hover:bg-gray-50 border border-transparent'}`}
                                    >
                                        <div className="flex gap-3">
                                            <div className={`mt-0.5 shrink-0 w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${isActive ? 'bg-blue-600 text-white' : 'bg-gray-200 text-gray-600'}`}>
                                                {idx + 1}
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <p className={`text-sm font-bold ${isActive ? 'text-blue-600' : 'text-gray-700'}`}>{vTitle}</p>
                                                {vDesc && <p className="text-xs text-gray-500 mt-1 line-clamp-1">{vDesc}</p>}
                                                <div className="flex items-center gap-2 mt-1">
                                                    <span className={`text-xs px-1.5 py-0.5 rounded font-medium ${vIsWasabi ? 'bg-sky-50 text-sky-600' : 'bg-purple-50 text-purple-600'}`}>
                                                        {vIsWasabi ? '☁️ Cloud' : '🔗 Embed'}
                                                    </span>
                                                    {vIsWasabi && vSize && (
                                                        <span className="text-xs text-gray-400">{formatFileSize(vSize)}</span>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </button>
                                );
                            })
                        )}
                    </div>
                </div>
            </div>

            {/* Video Player Main Area */}
            <div className="w-full lg:w-2/3 xl:w-3/4 flex flex-col">
                <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden flex-1 flex flex-col">
                    {activeVideo ? (
                        <>
                            {/* Player Wrapper */}
                            <div className="w-full aspect-video bg-black relative">
                                {isWasabi ? (
                                    <WasabiVideoPlayer videoId={activeVideoId} key={activeVideoId} />
                                ) : (
                                    <iframe 
                                        src={activeVideo.videoUrl || activeVideo.VideoUrl} 
                                        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" 
                                        allowFullScreen
                                        className="w-full h-full absolute top-0 left-0 border-0"
                                        title={activeVideo.title || activeVideo.Title}
                                    />
                                )}
                            </div>
                            
                            {/* Meta */}
                            <div className="p-6 md:p-8">
                                <h1 className="text-2xl md:text-3xl font-bold text-gray-900 mb-2">{activeVideo.title || activeVideo.Title}</h1>
                                
                                <div className="flex items-center gap-4 py-4 border-b border-gray-200 mb-6 flex-wrap">
                                    <span className="bg-blue-50 text-blue-700 px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wide">
                                        {selectedCourse?.courseCode || selectedCourse?.CourseCode}
                                    </span>
                                    <span className="text-sm font-medium text-gray-500">
                                        {selectedCourse?.lecturerName || selectedCourse?.LecturerName || 'Assigned Lecturer'}
                                    </span>
                                    
                                    {/* Source badge */}
                                    <span className={`px-2.5 py-1 rounded-full text-xs font-bold ${isWasabi ? 'bg-sky-50 text-sky-700' : 'bg-purple-50 text-purple-700'}`}>
                                        {isWasabi ? '☁️ Cloud Video' : '🔗 Embedded'}
                                    </span>

                                    {/* File size */}
                                    {isWasabi && (activeVideo.fileSizeBytes || activeVideo.FileSizeBytes) && (
                                        <span className="text-xs text-gray-400 font-medium">
                                            {formatFileSize(activeVideo.fileSizeBytes || activeVideo.FileSizeBytes)}
                                        </span>
                                    )}

                                    {/* Download button for Wasabi videos */}
                                    {isWasabi && (
                                        <button
                                            onClick={() => handleDownload(activeVideo)}
                                            disabled={downloading}
                                            className="ml-auto bg-emerald-50 hover:bg-emerald-100 text-emerald-700 px-4 py-1.5 rounded-lg text-xs font-bold transition-colors flex items-center gap-1.5 disabled:opacity-50"
                                        >
                                            {downloading ? (
                                                <>
                                                    <span className="w-3 h-3 border-2 border-emerald-400 border-t-transparent rounded-full animate-spin" />
                                                    Preparing...
                                                </>
                                            ) : (
                                                <>⬇️ Download</>
                                            )}
                                        </button>
                                    )}
                                </div>

                                <div>
                                    <h4 className="text-sm font-bold text-gray-900 mb-2">Lesson Description</h4>
                                    <p className="text-gray-600 leading-relaxed max-w-4xl text-sm md:text-base">
                                        {activeVideo.description || activeVideo.Description || "No specific description provided by the instructor."}
                                    </p>
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="flex-1 flex flex-col items-center justify-center p-12 text-center h-full min-h-[400px]">
                            <div className="w-24 h-24 bg-gray-50 rounded-full flex items-center justify-center mb-6">
                                <span className="text-4xl text-gray-300">🎬</span>
                            </div>
                            <h2 className="text-xl font-bold text-gray-800 mb-2">Select a video to start learning</h2>
                            <p className="text-gray-400 max-w-sm">Choose a lesson from the playlist on the left to begin watching.</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
