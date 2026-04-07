// @ts-nocheck
'use client';

import { useState, useEffect } from 'react';
import { studentApi } from '@/lib/studentApi';
import { useQuery } from '@tanstack/react-query';

export default function CourseVideosTab() {
    const [selectedCourse, setSelectedCourse] = useState<any>(null);
    const [activeVideo, setActiveVideo] = useState<any>(null);

    // Fetch enrolled courses
    const { data: myCourses, isLoading: loadingCourses } = useQuery({
        queryKey: ['my-courses'],
        queryFn: studentApi.getMyCourses,
    });

    // Fetch videos for the selected course
    const { data: videos, isLoading: loadingVideos, error: videosError, isError: isVideosError } = useQuery({
        queryKey: ['course-videos', selectedCourse?.id || selectedCourse?.Id],
        queryFn: () => studentApi.getCourseVideos(selectedCourse.id || selectedCourse.Id),
        enabled: !!selectedCourse
    });

    useEffect(() => {
        // Auto-select first course when loaded
        if (myCourses?.length > 0 && !selectedCourse) {
            setSelectedCourse(myCourses[0]);
        }
    }, [myCourses]);

    useEffect(() => {
        // Auto-select first video when course changes
        if (videos?.length > 0) {
            setActiveVideo(videos[0]);
        } else {
            setActiveVideo(null);
        }
    }, [videos, selectedCourse]);

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
                                const isActive = (activeVideo?.id || activeVideo?.Id) === vId;
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
                                            <div>
                                                <p className={`text-sm font-bold ${isActive ? 'text-blue-600' : 'text-gray-700'}`}>{vTitle}</p>
                                                {vDesc && <p className="text-xs text-gray-500 mt-1 line-clamp-1">{vDesc}</p>}
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
                                <iframe 
                                    src={activeVideo.videoUrl || activeVideo.VideoUrl} 
                                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" 
                                    allowFullScreen
                                    className="w-full h-full absolute top-0 left-0 border-0"
                                    title={activeVideo.title || activeVideo.Title}
                                />
                            </div>
                            
                            {/* Meta */}
                            <div className="p-6 md:p-8">
                                <h1 className="text-2xl md:text-3xl font-bold text-gray-900 mb-2">{activeVideo.title || activeVideo.Title}</h1>
                                
                                <div className="flex items-center gap-4 py-4 border-b border-gray-200 mb-6">
                                    <span className="bg-blue-50 text-blue-700 px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wide">
                                        {selectedCourse?.courseCode || selectedCourse?.CourseCode}
                                    </span>
                                    <span className="text-sm font-medium text-gray-500">
                                        {selectedCourse?.lecturerName || selectedCourse?.LecturerName || 'Assigned Lecturer'}
                                    </span>
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
