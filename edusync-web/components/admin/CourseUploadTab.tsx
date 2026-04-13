// @ts-nocheck
'use client';

import { useState, useRef, useCallback } from 'react';
import { adminApi } from '@/lib/adminApi';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';

export default function CourseUploadTab() {
    const queryClient = useQueryClient();
    const [activeFaculty, setActiveFaculty] = useState<string>('all');
    const [uploadModal, setUploadModal] = useState<any>(null); // null = closed, {} = open with prefill data
    const [editModal, setEditModal] = useState<any>(null);
    const [playingVideoId, setPlayingVideoId] = useState<number | null>(null);

    const { data: faculties, isLoading } = useQuery({
        queryKey: ['all-videos'],
        queryFn: adminApi.getAllVideos
    });

    const deleteMutation = useMutation({
        mutationFn: (videoId: number) => adminApi.deleteCourseVideo(videoId),
        onSuccess: () => {
            toast.success('Video deleted');
            queryClient.invalidateQueries({ queryKey: ['all-videos'] });
        },
        onError: () => toast.error('Failed to delete video')
    });

    const toggleFeaturedMutation = useMutation({
        mutationFn: ({ courseName, facultyName }: { courseName: string; facultyName: string }) =>
            adminApi.toggleFeatured(courseName, facultyName),
        onSuccess: (data) => {
            toast.success(data.isFeatured ? 'Course featured on landing page ⭐' : 'Course removed from featured');
            queryClient.invalidateQueries({ queryKey: ['all-videos'] });
        },
        onError: () => toast.error('Failed to toggle featured status')
    });

    // Get unique faculty names for tabs
    const facultyNames: string[] = faculties?.map((f: any) => f.facultyName).filter((n: string) => n) || [];

    // Filter displayed data
    const displayedFaculties = activeFaculty === 'all'
        ? faculties || []
        : (faculties || []).filter((f: any) => f.facultyName === activeFaculty);

    if (isLoading) return <div className="p-10 text-center text-gray-500 animate-pulse">Loading videos...</div>;

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex justify-between items-center mb-2">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">Course Videos</h2>
                    <p className="text-sm text-gray-400 mt-0.5">Upload and manage course videos organized by faculty.</p>
                </div>
                <button
                    onClick={() => setUploadModal({})}
                    className="px-5 py-2.5 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold rounded-xl transition-all shadow-lg shadow-amber-200 flex items-center gap-2 text-sm"
                >
                    <span className="text-lg">+</span> Upload New Video
                </button>
            </div>

            {/* Faculty Tabs */}
            <div className="flex items-center gap-1 bg-gray-100 rounded-xl p-1 overflow-x-auto">
                <button
                    onClick={() => setActiveFaculty('all')}
                    className={`px-4 py-2 rounded-lg text-sm font-bold whitespace-nowrap transition-all ${
                        activeFaculty === 'all'
                            ? 'bg-white shadow-sm text-gray-900'
                            : 'text-gray-500 hover:text-gray-700'
                    }`}
                >
                    All ({(faculties || []).reduce((acc: number, f: any) => acc + f.courses.reduce((a: number, c: any) => a + c.videos.length, 0), 0)})
                </button>
                {facultyNames.map((name: string) => {
                    const fac = faculties?.find((f: any) => f.facultyName === name);
                    const count = fac?.courses?.reduce((a: number, c: any) => a + c.videos.length, 0) || 0;
                    return (
                        <button
                            key={name}
                            onClick={() => setActiveFaculty(name)}
                            className={`px-4 py-2 rounded-lg text-sm font-bold whitespace-nowrap transition-all ${
                                activeFaculty === name
                                    ? 'bg-white shadow-sm text-amber-700'
                                    : 'text-gray-500 hover:text-gray-700'
                            }`}
                        >
                            {name} ({count})
                        </button>
                    );
                })}
            </div>

            {/* Content */}
            <div className="space-y-6">
                {displayedFaculties.length === 0 ? (
                    <div className="bg-white rounded-2xl border border-gray-100 p-16 text-center">
                        <div className="text-5xl mb-4">🎬</div>
                        <h3 className="text-lg font-bold text-gray-900 mb-2">No Videos Yet</h3>
                        <p className="text-sm text-gray-400 mb-6">Upload your first course video to get started.</p>
                        <button
                            onClick={() => setUploadModal({})}
                            className="px-5 py-2.5 bg-amber-500 hover:bg-amber-600 text-white font-bold rounded-xl transition-colors text-sm"
                        >
                            + Upload First Video
                        </button>
                    </div>
                ) : (
                    displayedFaculties.map((faculty: any) => (
                        <div key={faculty.facultyName} className="space-y-4">
                            {/* Faculty Header */}
                            {activeFaculty === 'all' && (
                                <div className="flex items-center gap-3 px-1">
                                    <span className="w-8 h-8 rounded-lg bg-gradient-to-br from-amber-400 to-amber-600 text-white flex items-center justify-center text-sm">🏛️</span>
                                    <h3 className="text-base font-bold text-gray-900">{faculty.facultyName}</h3>
                                    <span className="text-xs text-gray-400 bg-gray-100 px-2 py-0.5 rounded-full">
                                        {faculty.courses.length} course{faculty.courses.length !== 1 ? 's' : ''}
                                    </span>
                                    <button
                                        onClick={() => setUploadModal({ facultyName: faculty.facultyName })}
                                        className="ml-auto text-xs text-amber-600 hover:text-amber-700 font-bold bg-amber-50 hover:bg-amber-100 px-3 py-1.5 rounded-lg transition-colors"
                                    >
                                        + Add Video to {faculty.facultyName}
                                    </button>
                                </div>
                            )}

                            {/* Course Cards */}
                            {faculty.courses.map((course: any) => (
                                <div key={`${faculty.facultyName}-${course.courseName}`} className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden hover:shadow-md transition-shadow">
                                    {/* Course Header */}
                                    <div className="px-6 py-4 border-b border-gray-50 bg-gradient-to-r from-gray-50 to-white">
                                        <div className="flex items-center justify-between">
                                            <div className="flex items-center gap-3">
                                                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-lg">📚</div>
                                                <div>
                                                    <h4 className="font-bold text-gray-900">{course.courseName}</h4>
                                                    <div className="flex items-center gap-2 mt-0.5">
                                                        <span className="text-xs text-gray-500">{course.departmentName}</span>
                                                        {activeFaculty === 'all' && (
                                                            <span className="text-xs text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded font-medium">{faculty.facultyName}</span>
                                                        )}
                                                        {course.price > 0 && (
                                                            <span className="text-xs font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full">
                                                                ₦{course.price.toLocaleString()}
                                                            </span>
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <button
                                                    onClick={() => setUploadModal({
                                                        facultyName: faculty.facultyName,
                                                        departmentName: course.departmentName,
                                                        courseName: course.courseName,
                                                        price: course.price,
                                                        description: course.description
                                                    })}
                                                    className="text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-bold px-3 py-1.5 rounded-lg transition-colors"
                                                >
                                                    + Add Video
                                                </button>
                                                <button
                                                    onClick={() => toggleFeaturedMutation.mutate({
                                                        courseName: course.courseName,
                                                        facultyName: faculty.facultyName
                                                    })}
                                                    className={`text-xs font-bold px-3 py-1.5 rounded-lg transition-colors ${
                                                        course.isFeatured
                                                            ? 'bg-amber-100 hover:bg-amber-200 text-amber-700'
                                                            : 'bg-gray-50 hover:bg-gray-100 text-gray-500'
                                                    }`}
                                                    title={course.isFeatured ? 'Remove from featured' : 'Feature on landing page'}
                                                >
                                                    {course.isFeatured ? '⭐ Featured' : '☆ Feature'}
                                                </button>
                                                <button
                                                    onClick={() => setEditModal({
                                                        facultyName: faculty.facultyName,
                                                        departmentName: course.departmentName,
                                                        courseName: course.courseName,
                                                        price: course.price,
                                                        description: course.description
                                                    })}
                                                    className="text-xs bg-gray-50 hover:bg-gray-100 text-gray-600 font-bold px-3 py-1.5 rounded-lg transition-colors"
                                                >
                                                    ✏️ Edit
                                                </button>
                                            </div>
                                        </div>
                                        {course.description && (
                                            <p className="text-sm text-gray-500 mt-2 pl-[52px]">{course.description}</p>
                                        )}
                                    </div>

                                    {/* Video List */}
                                    <div className="divide-y divide-gray-50">
                                        {course.videos.map((video: any, idx: number) => (
                                            <div key={video.id} className="px-6 py-3">
                                                <div className="flex items-center gap-3 group">
                                                    <div className="w-7 h-7 rounded-lg bg-gray-100 flex items-center justify-center text-xs font-bold text-gray-500 shrink-0">
                                                        {idx + 1}
                                                    </div>
                                                    <div className="w-8 h-8 rounded bg-slate-100 flex items-center justify-center text-lg shrink-0 overflow-hidden">
                                                        {video.thumbnailUrl ? (
                                                            <img src={video.thumbnailUrl} alt="" className="w-full h-full object-cover" />
                                                        ) : (
                                                            video.isWasabiVideo ? '☁️' : '🎬'
                                                        )}
                                                    </div>
                                                    <div className="flex-1 min-w-0">
                                                        <h5 className="text-sm font-semibold text-gray-800 truncate">{video.title}</h5>
                                                        <div className="flex items-center gap-2 mt-0.5">
                                                            {video.duration && (
                                                                <span className="text-xs text-gray-400">⏱️ {video.duration}</span>
                                                            )}
                                                            <span className={`text-xs px-1.5 py-0.5 rounded font-medium ${video.isWasabiVideo ? 'bg-sky-50 text-sky-700' : 'bg-purple-50 text-purple-700'}`}>
                                                                {video.isWasabiVideo ? '☁️ Cloud' : '🔗 Embed'}
                                                            </span>
                                                            {video.isWasabiVideo && video.fileSizeBytes && (
                                                                <span className="text-xs text-gray-400">{formatFileSize(video.fileSizeBytes)}</span>
                                                            )}
                                                        </div>
                                                    </div>
                                                    <div className="shrink-0 flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                                                        <button
                                                            onClick={() => setPlayingVideoId(playingVideoId === video.id ? null : video.id)}
                                                            className="text-xs text-blue-600 hover:text-blue-800 font-bold px-2.5 py-1 bg-blue-50 rounded-lg"
                                                        >
                                                            {playingVideoId === video.id ? 'Close' : '▶ Play'}
                                                        </button>
                                                        <button
                                                            onClick={() => { if (confirm('Delete this video?')) deleteMutation.mutate(video.id); }}
                                                            className="text-xs text-red-500 hover:text-red-700 font-bold px-2 py-1 bg-red-50 rounded-lg"
                                                        >
                                                            🗑️
                                                        </button>
                                                    </div>
                                                </div>
                                                {playingVideoId === video.id && (
                                                    <div className="mt-3 ml-[66px] w-full max-w-2xl aspect-video bg-black rounded-lg overflow-hidden border border-slate-200 shadow-sm">
                                                        {video.isWasabiVideo ? (
                                                            <WasabiVideoPlayer videoId={video.id} />
                                                        ) : (
                                                            <iframe src={video.videoUrl} title={video.title} className="w-full h-full border-0" allowFullScreen />
                                                        )}
                                                    </div>
                                                )}
                                            </div>
                                        ))}
                                    </div>

                                    {course.videos.length === 0 && (
                                        <div className="px-6 py-6 text-center text-sm text-gray-400 italic">
                                            No videos in this course yet.
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    ))
                )}
            </div>

            {/* Upload Modal */}
            {uploadModal && (
                <UploadVideoModal
                    prefill={uploadModal}
                    existingFaculties={facultyNames}
                    existingData={faculties || []}
                    onClose={() => setUploadModal(null)}
                    onSuccess={() => {
                        setUploadModal(null);
                        queryClient.invalidateQueries({ queryKey: ['all-videos'] });
                    }}
                />
            )}

            {/* Edit Course Modal */}
            {editModal && (
                <EditCourseModal
                    course={editModal}
                    onClose={() => setEditModal(null)}
                    onSuccess={() => {
                        setEditModal(null);
                        queryClient.invalidateQueries({ queryKey: ['all-videos'] });
                    }}
                />
            )}
        </div>
    );
}

// ═══════════════════════════════════════════
// Wasabi Video Player (unchanged from original)
// ═══════════════════════════════════════════
function WasabiVideoPlayer({ videoId }: { videoId: number }) {
    const [streamUrl, setStreamUrl] = useState<string | null>(null);
    const [loading, setLoading] = useState(true);

    useState(() => {
        adminApi.getVideoStreamUrl(videoId).then(data => {
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

function formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
}

// ═══════════════════════════════════════════
// Upload Video Modal
// ═══════════════════════════════════════════
function UploadVideoModal({ prefill, existingFaculties, existingData, onClose, onSuccess }: any) {
    const [activeTab, setActiveTab] = useState<'upload' | 'embed'>('upload');

    // Metadata fields
    const [facultyName, setFacultyName] = useState(prefill.facultyName || '');
    const [departmentName, setDepartmentName] = useState(prefill.departmentName || '');
    const [courseName, setCourseName] = useState(prefill.courseName || '');
    const [price, setPrice] = useState(prefill.price || 0);
    const [title, setTitle] = useState('');
    const [desc, setDesc] = useState(prefill.description || '');
    const [duration, setDuration] = useState('');
    const [url, setUrl] = useState('');
    const [whatYoullLearn, setWhatYoullLearn] = useState('');

    // File upload state
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [uploadProgress, setUploadProgress] = useState(0);
    const [uploadSpeed, setUploadSpeed] = useState('');
    const [uploadEta, setUploadEta] = useState('');
    const [uploadPhase, setUploadPhase] = useState<'idle' | 'preparing' | 'uploading' | 'saving'>('idle');
    const [isUploading, setIsUploading] = useState(false);
    const [isDragOver, setIsDragOver] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const abortControllerRef = useRef<AbortController | null>(null);

    // Thumbnail state
    const [thumbnailPreview, setThumbnailPreview] = useState<string | null>(null);
    const thumbnailInputRef = useRef<HTMLInputElement>(null);

    // Autocomplete suggestions
    const existingDepartments = [...new Set(
        existingData
            .filter((f: any) => !facultyName || f.facultyName === facultyName)
            .flatMap((f: any) => f.courses.map((c: any) => c.departmentName))
            .filter(Boolean)
    )];
    const existingCourses = [...new Set(
        existingData
            .filter((f: any) => !facultyName || f.facultyName === facultyName)
            .flatMap((f: any) => f.courses.map((c: any) => c.courseName))
            .filter(Boolean)
    )];

    const isMetadataLocked = !!(prefill.courseName);

    const handleFileSelect = (file: File) => {
        const allowedTypes = ['video/mp4', 'video/quicktime', 'video/x-msvideo', 'video/x-matroska', 'video/webm', 'video/avi'];
        if (!allowedTypes.includes(file.type) && !file.name.match(/\.(mp4|mov|avi|mkv|webm)$/i)) {
            toast.error('Please select a video file (MP4, MOV, AVI, MKV, or WebM)');
            return;
        }
        setSelectedFile(file);
        if (!title) {
            setTitle(file.name.replace(/\.[^/.]+$/, '').replace(/[_-]/g, ' '));
        }
    };

    const handleDrop = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        setIsDragOver(false);
        const file = e.dataTransfer.files[0];
        if (file) handleFileSelect(file);
    }, [title]);

    const handleDragOver = useCallback((e: React.DragEvent) => { e.preventDefault(); setIsDragOver(true); }, []);
    const handleDragLeave = useCallback((e: React.DragEvent) => { e.preventDefault(); setIsDragOver(false); }, []);

    const cancelUpload = () => {
        abortControllerRef.current?.abort();
        setIsUploading(false);
        setUploadProgress(0);
        setUploadSpeed('');
    };

    const validateMetadata = () => {
        if (!facultyName.trim()) { toast.error('Faculty name is required'); return false; }
        if (!departmentName.trim()) { toast.error('Department name is required'); return false; }
        if (!courseName.trim()) { toast.error('Course name is required'); return false; }
        if (!title.trim()) { toast.error('Video title is required'); return false; }
        return true;
    };

    const handleThumbnailSelect = (file: File) => {
        if (!file.type.startsWith('image/')) {
            toast.error('Please select an image file (PNG, JPG, WebP)');
            return;
        }
        if (file.size > 2 * 1024 * 1024) {
            toast.error('Thumbnail must be under 2MB');
            return;
        }
        const reader = new FileReader();
        reader.onload = (e) => {
            setThumbnailPreview(e.target?.result as string);
        };
        reader.readAsDataURL(file);
    };

    // === Wasabi Upload ===
    const handleUpload = async () => {
        if (!selectedFile || !validateMetadata()) return;

        setIsUploading(true);
        setUploadProgress(0);
        setUploadEta('');
        setUploadPhase('preparing');

        try {
            toast.loading('Preparing upload...', { id: 'upload-progress' });
            const { uploadUrl, objectKey } = await adminApi.getUploadUrlV2(
                selectedFile.name,
                selectedFile.type || 'video/mp4'
            );

            abortControllerRef.current = new AbortController();
            setUploadPhase('uploading');
            toast.loading('Uploading to cloud...', { id: 'upload-progress' });
            const uploadStartTime = Date.now();

            await axios.put(uploadUrl, selectedFile, {
                headers: { 'Content-Type': selectedFile.type || 'video/mp4' },
                maxContentLength: Infinity,
                maxBodyLength: Infinity,
                timeout: 0,
                transformRequest: [(data: any) => data],
                signal: abortControllerRef.current.signal,
                onUploadProgress: (progressEvent) => {
                    const progress = progressEvent.total
                        ? Math.round((progressEvent.loaded / progressEvent.total) * 100) : 0;
                    setUploadProgress(progress);
                    const elapsed = (Date.now() - uploadStartTime) / 1000;
                    if (elapsed > 0.5 && progressEvent.loaded > 0) {
                        const speed = progressEvent.loaded / elapsed;
                        setUploadSpeed(formatFileSize(speed) + '/s');
                        if (progressEvent.total && progress > 0 && progress < 100) {
                            const remaining = progressEvent.total - progressEvent.loaded;
                            const etaSeconds = remaining / speed;
                            setUploadEta(etaSeconds < 60 ? `~${Math.ceil(etaSeconds)}s left` : `~${Math.ceil(etaSeconds / 60)}m left`);
                        } else if (progress >= 100) {
                            setUploadEta('Finalizing...');
                        }
                    }
                }
            });

            setUploadPhase('saving');
            toast.loading('Saving video metadata...', { id: 'upload-progress' });
            await adminApi.confirmVideoUploadV2({
                title: title.trim(),
                description: desc.trim() || undefined,
                objectKey,
                originalFileName: selectedFile.name,
                fileSizeBytes: selectedFile.size,
                facultyName: facultyName.trim(),
                departmentName: departmentName.trim(),
                courseName: courseName.trim(),
                duration: duration.trim() || undefined,
                price: Number(price) || 0,
                thumbnailUrl: thumbnailPreview || undefined,
                whatYoullLearn: whatYoullLearn.trim() || undefined,
            });

            toast.success('Video uploaded successfully! ☁️', { id: 'upload-progress' });
            onSuccess();
        } catch (error: any) {
            if (error.name === 'CanceledError' || error.code === 'ERR_CANCELED') {
                toast.dismiss('upload-progress');
                toast('Upload cancelled', { icon: '⚠️' });
            } else {
                toast.error('Upload failed: ' + (error.response?.data?.message || error.message), { id: 'upload-progress' });
            }
        } finally {
            setIsUploading(false);
            setUploadPhase('idle');
            abortControllerRef.current = null;
        }
    };

    // === YouTube Embed ===
    const onSubmitEmbed = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validateMetadata() || !url.trim()) { toast.error('Video URL is required'); return; }

        let finalUrl = url;
        if (finalUrl.includes('youtube.com/watch?v=')) {
            finalUrl = finalUrl.replace('watch?v=', 'embed/');
            finalUrl = finalUrl.split('&')[0];
        } else if (finalUrl.includes('youtu.be/')) {
            finalUrl = finalUrl.replace('youtu.be/', 'youtube.com/embed/');
            finalUrl = finalUrl.split('?')[0];
        }

        try {
            await adminApi.createVideoV2({
                title: title.trim(),
                description: desc.trim() || undefined,
                videoUrl: finalUrl,
                facultyName: facultyName.trim(),
                departmentName: departmentName.trim(),
                courseName: courseName.trim(),
                duration: duration.trim() || undefined,
                price: Number(price) || 0,
                thumbnailUrl: thumbnailPreview || undefined,
                whatYoullLearn: whatYoullLearn.trim() || undefined,
            });
            toast.success('Video link embedded!');
            onSuccess();
        } catch {
            toast.error('Failed to embed video');
        }
    };

    return (
        <div className="fixed inset-0 bg-black/60 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-xl w-full max-w-lg overflow-hidden shadow-2xl animate-in fade-in zoom-in-95 duration-200 flex flex-col max-h-[90vh]">
                {/* Header */}
                <div className="px-6 py-4 bg-gradient-to-r from-amber-500 to-amber-600 shrink-0">
                    <h3 className="text-lg font-bold text-white">
                        {isMetadataLocked ? `Add Video to ${prefill.courseName}` : 'Upload New Video'}
                    </h3>
                    <p className="text-amber-100 text-sm">
                        {isMetadataLocked ? prefill.facultyName : 'Fill in course details and upload a video'}
                    </p>
                </div>

                <div className="overflow-y-auto flex-1">
                    {/* Course Metadata Fields */}
                    <div className="p-6 pb-3 space-y-3 border-b border-gray-100 bg-gray-50/50">
                        <h4 className="font-semibold text-gray-800 text-sm uppercase tracking-wider">Course Details</h4>
                        <div className="grid grid-cols-2 gap-3">
                            <div>
                                <label className="block text-xs font-semibold text-gray-600 mb-1">Faculty *</label>
                                <input
                                    type="text"
                                    list="faculty-list"
                                    className="w-full px-3 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="e.g. Physical Science"
                                    value={facultyName}
                                    onChange={e => setFacultyName(e.target.value)}
                                    disabled={isMetadataLocked}
                                />
                                <datalist id="faculty-list">
                                    {existingFaculties.map((f: string) => <option key={f} value={f} />)}
                                </datalist>
                            </div>
                            <div>
                                <label className="block text-xs font-semibold text-gray-600 mb-1">Department *</label>
                                <input
                                    type="text"
                                    list="dept-list"
                                    className="w-full px-3 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="e.g. Physics"
                                    value={departmentName}
                                    onChange={e => setDepartmentName(e.target.value)}
                                    disabled={isMetadataLocked}
                                />
                                <datalist id="dept-list">
                                    {existingDepartments.map((d: string) => <option key={d} value={d} />)}
                                </datalist>
                            </div>
                        </div>
                        <div className="grid grid-cols-2 gap-3">
                            <div>
                                <label className="block text-xs font-semibold text-gray-600 mb-1">Course Name *</label>
                                <input
                                    type="text"
                                    list="course-list"
                                    className="w-full px-3 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="e.g. Physics 101"
                                    value={courseName}
                                    onChange={e => setCourseName(e.target.value)}
                                    disabled={isMetadataLocked}
                                />
                                <datalist id="course-list">
                                    {existingCourses.map((c: string) => <option key={c} value={c} />)}
                                </datalist>
                            </div>
                            <div>
                                <label className="block text-xs font-semibold text-gray-600 mb-1">Price (₦)</label>
                                <input
                                    type="number"
                                    className="w-full px-3 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="0"
                                    value={price}
                                    onChange={e => setPrice(Number(e.target.value))}
                                    disabled={isMetadataLocked}
                                />
                            </div>
                        </div>
                        {/* What You'll Learn */}
                        <div className="col-span-2">
                            <label className="block text-xs font-semibold text-gray-600 mb-1">What You'll Learn</label>
                            <textarea
                                className="w-full px-3 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-300 focus:outline-none resize-none"
                                placeholder="Enter each learning point on a new line, e.g.:\nUnderstand rock formations\nIdentify mineral types\nAnalyze geological maps"
                                rows={3}
                                value={whatYoullLearn}
                                onChange={e => setWhatYoullLearn(e.target.value)}
                            />
                        </div>
                    </div>

                    {/* Thumbnail Picker */}
                    <div className="px-6 pb-3 space-y-2">
                        <h4 className="font-semibold text-gray-800 text-sm uppercase tracking-wider">Video Thumbnail</h4>
                        <div className="flex items-center gap-4">
                            <div
                                onClick={() => thumbnailInputRef.current?.click()}
                                className={`w-28 h-20 rounded-lg border-2 border-dashed cursor-pointer flex items-center justify-center overflow-hidden transition-all hover:border-amber-400 hover:bg-amber-50/30 ${
                                    thumbnailPreview ? 'border-emerald-300 bg-emerald-50' : 'border-gray-300 bg-gray-50'
                                }`}
                            >
                                {thumbnailPreview ? (
                                    <img src={thumbnailPreview} alt="Thumbnail" className="w-full h-full object-cover rounded-md" />
                                ) : (
                                    <div className="text-center">
                                        <div className="text-xl">🖼️</div>
                                        <p className="text-[10px] text-gray-400 mt-0.5">Click to add</p>
                                    </div>
                                )}
                            </div>
                            <input ref={thumbnailInputRef} type="file" accept="image/*,.png,.jpg,.jpeg,.webp" className="hidden"
                                onChange={(e) => e.target.files?.[0] && handleThumbnailSelect(e.target.files[0])} />
                            <div className="flex-1">
                                <p className="text-xs text-gray-500">Optional thumbnail image for this video.</p>
                                <p className="text-xs text-gray-400 mt-0.5">PNG, JPG, or WebP • Max 2MB</p>
                                {thumbnailPreview && (
                                    <button type="button" onClick={() => setThumbnailPreview(null)}
                                        className="text-xs text-red-500 hover:underline mt-1">Remove thumbnail</button>
                                )}
                            </div>
                        </div>
                    </div>

                    {/* Tab Bar */}
                    <div className="flex border-b border-gray-200 mx-6 mt-3">
                        <button type="button" onClick={() => setActiveTab('upload')}
                            className={`px-4 py-2.5 text-sm font-bold transition-colors border-b-2 ${activeTab === 'upload' ? 'border-amber-500 text-amber-600' : 'border-transparent text-gray-500 hover:text-gray-700'}`}>
                            ☁️ Upload File
                        </button>
                        <button type="button" onClick={() => setActiveTab('embed')}
                            className={`px-4 py-2.5 text-sm font-bold transition-colors border-b-2 ${activeTab === 'embed' ? 'border-amber-500 text-amber-600' : 'border-transparent text-gray-500 hover:text-gray-700'}`}>
                            🔗 Embed URL
                        </button>
                    </div>

                    {/* Upload Tab */}
                    {activeTab === 'upload' && (
                        <div className="p-6 space-y-4">
                            {/* Drag & Drop */}
                            <div
                                onDrop={handleDrop} onDragOver={handleDragOver} onDragLeave={handleDragLeave}
                                onClick={() => fileInputRef.current?.click()}
                                className={`relative border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-all duration-200 ${
                                    isDragOver ? 'border-amber-400 bg-amber-50 scale-[1.02]'
                                    : selectedFile ? 'border-emerald-300 bg-emerald-50'
                                    : 'border-gray-300 bg-gray-50 hover:border-amber-300 hover:bg-amber-50/30'
                                }`}
                            >
                                <input ref={fileInputRef} type="file" accept="video/*,.mp4,.mov,.avi,.mkv,.webm" className="hidden"
                                    onChange={(e) => e.target.files?.[0] && handleFileSelect(e.target.files[0])} />
                                {selectedFile ? (
                                    <div>
                                        <div className="text-2xl mb-1">✅</div>
                                        <p className="font-bold text-emerald-700 text-sm">{selectedFile.name}</p>
                                        <p className="text-xs text-emerald-600 mt-1">{formatFileSize(selectedFile.size)}</p>
                                        <button type="button" onClick={(e) => { e.stopPropagation(); setSelectedFile(null); }}
                                            className="text-xs text-red-500 hover:underline mt-1">Remove</button>
                                    </div>
                                ) : (
                                    <div>
                                        <div className="text-2xl mb-1">☁️</div>
                                        <p className="font-bold text-gray-700 text-sm">Drag & drop a video file</p>
                                        <p className="text-xs text-gray-500 mt-1">or click to browse • MP4, MOV, AVI, MKV, WebM</p>
                                    </div>
                                )}
                            </div>

                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Video Title *</label>
                                <input type="text" className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="e.g. Lesson 1: Introduction" value={title} onChange={e => setTitle(e.target.value)} disabled={isUploading} />
                            </div>
                            <div className="grid grid-cols-2 gap-3">
                                <div>
                                    <label className="block text-sm font-semibold text-gray-700 mb-1">Duration</label>
                                    <input type="text" className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                        placeholder="e.g. 1h 30m" value={duration} onChange={e => setDuration(e.target.value)} disabled={isUploading} />
                                </div>
                                <div>
                                    <label className="block text-sm font-semibold text-gray-700 mb-1">Description</label>
                                    <input type="text" className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                        placeholder="Brief description..." value={desc} onChange={e => setDesc(e.target.value)} disabled={isUploading} />
                                </div>
                            </div>

                            {/* Progress */}
                            {isUploading && (
                                <div className="space-y-2">
                                    <div className="flex justify-between text-xs text-gray-600">
                                        <span className="font-bold">
                                            {uploadPhase === 'preparing' && '⏳ Preparing upload...'}
                                            {uploadPhase === 'uploading' && '☁️ Uploading to cloud...'}
                                            {uploadPhase === 'saving' && '✅ Saving video metadata...'}
                                        </span>
                                        {uploadPhase === 'uploading' && (
                                            <span className="font-mono">{uploadProgress}% • {uploadSpeed} {uploadEta && `• ${uploadEta}`}</span>
                                        )}
                                    </div>
                                    <div className="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
                                        <div className={`h-full rounded-full transition-all duration-300 ease-out ${
                                            uploadPhase === 'saving' ? 'bg-gradient-to-r from-emerald-400 to-emerald-500'
                                            : uploadPhase === 'preparing' ? 'bg-gradient-to-r from-blue-400 to-blue-500 animate-pulse'
                                            : 'bg-gradient-to-r from-amber-400 to-amber-500'
                                        }`} style={{ width: uploadPhase === 'preparing' ? '5%' : uploadPhase === 'saving' ? '100%' : `${uploadProgress}%` }} />
                                    </div>
                                </div>
                            )}

                            <div className="flex justify-end gap-3 pt-2">
                                {isUploading ? (
                                    <button type="button" onClick={cancelUpload} className="px-5 py-2 text-red-600 font-semibold hover:bg-red-50 rounded-lg transition-colors">Cancel Upload</button>
                                ) : (
                                    <>
                                        <button type="button" onClick={onClose} className="px-5 py-2 text-gray-600 font-semibold hover:bg-gray-100 rounded-lg transition-colors">Close</button>
                                        <button type="button" onClick={handleUpload} disabled={!selectedFile || !title.trim()}
                                            className="px-5 py-2 bg-amber-500 hover:bg-amber-600 text-white font-bold rounded-lg transition-colors shadow-sm flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed">
                                            ☁️ Upload to Cloud
                                        </button>
                                    </>
                                )}
                            </div>
                        </div>
                    )}

                    {/* Embed Tab */}
                    {activeTab === 'embed' && (
                        <form onSubmit={onSubmitEmbed} className="p-6 space-y-4">
                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Video Title *</label>
                                <input required type="text" className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="e.g. Lesson 1: Introduction" value={title} onChange={e => setTitle(e.target.value)} />
                            </div>
                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Video URL (YouTube) *</label>
                                <input required type="url" className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="https://youtu.be/... or https://youtube.com/watch?v=..." value={url} onChange={e => setUrl(e.target.value)} />
                            </div>
                            <div className="grid grid-cols-2 gap-3">
                                <div>
                                    <label className="block text-sm font-semibold text-gray-700 mb-1">Duration</label>
                                    <input type="text" className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                        placeholder="e.g. 45m" value={duration} onChange={e => setDuration(e.target.value)} />
                                </div>
                                <div>
                                    <label className="block text-sm font-semibold text-gray-700 mb-1">Description</label>
                                    <input type="text" className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                        placeholder="Brief description..." value={desc} onChange={e => setDesc(e.target.value)} />
                                </div>
                            </div>
                            <div className="flex justify-end gap-3 pt-2">
                                <button type="button" onClick={onClose} className="px-5 py-2 text-gray-600 font-semibold hover:bg-gray-100 rounded-lg transition-colors">Close</button>
                                <button type="submit" className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg transition-colors shadow-sm flex items-center gap-2">
                                    🔗 Embed Video
                                </button>
                            </div>
                        </form>
                    )}
                </div>
            </div>
        </div>
    );
}

// ═══════════════════════════════════════════
// Edit Course Modal
// ═══════════════════════════════════════════
function EditCourseModal({ course, onClose, onSuccess }: any) {
    const [description, setDescription] = useState(course.description || '');
    const [departmentName, setDepartmentName] = useState(course.departmentName || '');
    const [price, setPrice] = useState(course.price || 0);
    const [saving, setSaving] = useState(false);

    const handleSave = async () => {
        setSaving(true);
        try {
            await adminApi.updateCourseMetadata({
                courseName: course.courseName,
                facultyName: course.facultyName,
                description,
                departmentName,
                price: Number(price) || 0,
            });
            toast.success('Course updated!');
            onSuccess();
        } catch {
            toast.error('Failed to update course');
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="fixed inset-0 bg-black/60 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-xl w-full max-w-md overflow-hidden shadow-2xl">
                <div className="px-6 py-4 bg-gradient-to-r from-blue-500 to-blue-600">
                    <h3 className="text-lg font-bold text-white">Edit Course</h3>
                    <p className="text-blue-100 text-sm">{course.courseName}</p>
                </div>
                <div className="p-6 space-y-4">
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-1">Course Name</label>
                        <input type="text" className="w-full px-4 py-2 bg-gray-100 border border-gray-200 rounded-lg text-sm text-gray-500" value={course.courseName} disabled />
                        <p className="text-xs text-gray-400 mt-1">Course name cannot be changed (it groups videos together)</p>
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-1">Department</label>
                        <input type="text" className="w-full px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-blue-300 focus:outline-none"
                            value={departmentName} onChange={e => setDepartmentName(e.target.value)} />
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-1">Description</label>
                        <textarea rows={3} className="w-full px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-blue-300 focus:outline-none"
                            placeholder="Course description..." value={description} onChange={e => setDescription(e.target.value)} />
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-1">Price (₦)</label>
                        <input type="number" className="w-full px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-blue-300 focus:outline-none"
                            value={price} onChange={e => setPrice(Number(e.target.value))} />
                    </div>
                    <div className="flex justify-end gap-3 pt-2">
                        <button type="button" onClick={onClose} className="px-5 py-2 text-gray-600 font-semibold hover:bg-gray-100 rounded-lg transition-colors">Cancel</button>
                        <button type="button" onClick={handleSave} disabled={saving}
                            className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg transition-colors shadow-sm disabled:opacity-50">
                            {saving ? 'Saving...' : 'Save Changes'}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}
