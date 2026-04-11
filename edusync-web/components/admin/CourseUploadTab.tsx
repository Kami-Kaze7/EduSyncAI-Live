// @ts-nocheck
'use client';

import { useState, useRef, useCallback } from 'react';
import { adminApi } from '@/lib/adminApi';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';

export default function CourseUploadTab() {
    const queryClient = useQueryClient();
    const [expandedNodes, setExpandedNodes] = useState<Record<string, boolean>>({});
    const [uploadingCourse, setUploadingCourse] = useState<{id: number, name: string} | null>(null);
    const [playingVideoId, setPlayingVideoId] = useState<number | null>(null);

    const { data: tree, isLoading } = useQuery({
        queryKey: ['hierarchy-tree'],
        queryFn: adminApi.getHierarchyTree
    });

    const toggleNode = (nodeId: string) => {
        setExpandedNodes(prev => ({ ...prev, [nodeId]: !prev[nodeId] }));
    };

    if (isLoading) return <div className="p-10 text-center text-gray-500 animate-pulse">Loading Tree...</div>;

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">Course Videos</h2>
                    <p className="text-sm text-gray-400 mt-0.5">Upload videos to Wasabi cloud or embed YouTube links.</p>
                </div>
            </div>

            <div className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm min-h-[500px]">
                {tree?.length === 0 ? (
                    <div className="text-center py-12">
                        <div className="text-4xl mb-3">🗂️</div>
                        <h3 className="text-lg font-bold text-gray-900 mb-1">Structure Empty</h3>
                        <p className="text-sm text-gray-400">Head over to the Faculties tab to build the academic hierarchy first.</p>
                    </div>
                ) : (
                    <div className="space-y-2">
                        {tree?.map((faculty: any) => (
                            <TreeNode 
                                key={`fac-${faculty.id}`} 
                                node={faculty} 
                                depth={0} 
                                expanded={expandedNodes[`fac-${faculty.id}`]}
                                onToggle={() => toggleNode(`fac-${faculty.id}`)}
                                expandedNodes={expandedNodes}
                                setExpandedNodes={setExpandedNodes}
                                onAddVideo={(course) => setUploadingCourse(course)}
                                playingVideoId={playingVideoId}
                                setPlayingVideoId={setPlayingVideoId}
                            />
                        ))}
                    </div>
                )}
            </div>

            {uploadingCourse && (
                <AddVideoModal 
                    course={uploadingCourse} 
                    onClose={() => setUploadingCourse(null)} 
                    onSuccess={() => {
                        setUploadingCourse(null);
                        queryClient.invalidateQueries({ queryKey: ['hierarchy-tree'] });
                    }} 
                />
            )}
        </div>
    );
}

function TreeNode({ node, depth, expanded, onToggle, expandedNodes, setExpandedNodes, onAddVideo, playingVideoId, setPlayingVideoId }: any) {
    const isLeaf = !node.children || node.children.length === 0;
    const isCourse = node.type === 'Course';
    
    const icons: any = {
        'Faculty': '🏛️',
        'Department': '🏢',
        'Year': '📚',
        'Course': '🎓'
    };

    return (
        <div className="w-full">
            <div 
                className={`flex items-center py-2 px-3 hover:bg-gray-50 rounded-lg cursor-pointer ${isCourse ? 'border-b border-gray-200 pb-3' : ''}`}
                style={{ paddingLeft: `${depth * 28 + 12}px` }}
                onClick={onToggle}
            >
                <div className="w-5 flex items-center justify-center shrink-0 mr-2 text-gray-400">
                    {!isCourse && !isLeaf ? (expanded ? '▼' : '▶') : (isCourse ? '📄' : '•')}
                </div>
                <div className="flex-1 flex justify-between items-center">
                    <div className="flex items-center gap-2">
                        <span className="text-xl">{icons[node.type]}</span>
                        <span className={`font-medium ${isCourse ? 'text-blue-600 font-bold' : 'text-gray-800'}`}>
                            {node.type === 'Course' ? `${node.courseCode} - ${node.courseTitle}` : node.name}
                        </span>
                    </div>
                    {isCourse && (
                        <button 
                            onClick={(e) => { e.stopPropagation(); onAddVideo({id: node.id, name: node.courseTitle, videos: node.videos || []}); }}
                            className={node.videos?.length > 0 ? "bg-blue-100 hover:bg-blue-200 text-blue-700 px-3 py-1 text-xs font-bold rounded-lg transition-colors" : "bg-blue-50 hover:bg-blue-100 text-blue-600 px-3 py-1 text-xs font-bold rounded-lg transition-colors"}
                        >
                            {node.videos?.length > 0 ? `View Videos (${node.videos.length})` : '+ Add Video'}
                        </button>
                    )}
                </div>
            </div>

            {expanded && !isCourse && node.children?.map((child: any) => (
                <TreeNode 
                    key={`${child.type}-${child.id}`} 
                    node={child} 
                    depth={depth + 1}
                    expanded={expandedNodes[`${child.type}-${child.id}`]}
                    onToggle={() => setExpandedNodes((p:any) => ({...p, [`${child.type}-${child.id}`]: !p[`${child.type}-${child.id}`]}))}
                    expandedNodes={expandedNodes}
                    setExpandedNodes={setExpandedNodes}
                    onAddVideo={onAddVideo}
                    playingVideoId={playingVideoId}
                    setPlayingVideoId={setPlayingVideoId}
                />
            ))}

            {expanded && isCourse && node.videos?.length > 0 && (
                <div className="mt-2 mb-4 space-y-2" style={{ paddingLeft: `${(depth + 1) * 28 + 12}px` }}>
                    {node.videos.map((video: any) => (
                        <div key={video.id} className="mb-2">
                            <div className="flex items-center gap-3 bg-slate-50 border border-slate-200 p-3 rounded-lg mr-4 group">
                                <div className="w-8 h-8 rounded bg-slate-200 flex items-center justify-center text-lg shrink-0">
                                    {video.isWasabiVideo ? '☁️' : '🎬'}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <h4 className="text-sm font-bold text-slate-800 truncate">{video.title}</h4>
                                    <div className="flex items-center gap-2 mt-0.5">
                                        <p className="text-xs text-slate-500 truncate">{video.description || 'No description'}</p>
                                        {video.isWasabiVideo && video.fileSizeBytes && (
                                            <span className="text-xs bg-emerald-50 text-emerald-700 px-1.5 py-0.5 rounded font-medium shrink-0">
                                                {formatFileSize(video.fileSizeBytes)}
                                            </span>
                                        )}
                                        <span className={`text-xs px-1.5 py-0.5 rounded font-medium shrink-0 ${video.isWasabiVideo ? 'bg-sky-50 text-sky-700' : 'bg-purple-50 text-purple-700'}`}>
                                            {video.isWasabiVideo ? '☁️ Cloud' : '🔗 Embed'}
                                        </span>
                                    </div>
                                </div>
                                <div className="shrink-0 flex gap-2">
                                    <button 
                                        type="button" 
                                        onClick={() => setPlayingVideoId(playingVideoId === video.id ? null : video.id)} 
                                        className="text-xs text-blue-600 hover:text-blue-800 hover:underline font-bold px-2 py-1 bg-blue-50 rounded"
                                    >
                                        {playingVideoId === video.id ? 'Close' : 'Play'}
                                    </button>
                                </div>
                            </div>
                            {playingVideoId === video.id && (
                                <div className="mt-2 w-full max-w-2xl aspect-video bg-black rounded-lg overflow-hidden border border-slate-200 shadow-sm mr-4">
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
            )}
            {expanded && isCourse && node.videos?.length === 0 && (
                 <div className="mt-2 mb-4 text-xs italic text-gray-400" style={{ paddingLeft: `${(depth + 1) * 28 + 12}px` }}>
                    No videos attached to this course yet.
                 </div>
            )}
        </div>
    );
}

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
        <video 
            src={streamUrl} 
            controls 
            autoPlay 
            className="w-full h-full"
            controlsList="nodownload"
        >
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

function AddVideoModal({ course, onClose, onSuccess }: any) {
    const [activeTab, setActiveTab] = useState<'upload' | 'embed'>('upload');
    const [title, setTitle] = useState('');
    const [desc, setDesc] = useState('');
    const [url, setUrl] = useState('');
    const [playingVideoId, setPlayingVideoId] = useState<number | null>(null);

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

    const queryClient = useQueryClient();

    // === URL Embed (existing flow) ===
    const addMutation = useMutation({
        mutationFn: (data: any) => adminApi.addCourseVideo(course.id, data),
        onSuccess: () => {
            toast.success('Video link embedded!');
            onSuccess();
        },
        onError: () => toast.error('Failed to embed video')
    });

    const onSubmitEmbed = (e: React.FormEvent) => {
        e.preventDefault();
        let finalUrl = url;
        if (finalUrl.includes('youtube.com/watch?v=')) {
            finalUrl = finalUrl.replace('watch?v=', 'embed/');
            finalUrl = finalUrl.split('&')[0];
        } else if (finalUrl.includes('youtu.be/')) {
            finalUrl = finalUrl.replace('youtu.be/', 'youtube.com/embed/');
            finalUrl = finalUrl.split('?')[0];
        }
        addMutation.mutate({ title, description: desc, videoUrl: finalUrl });
    };

    // === File Upload (new Wasabi flow) ===
    const handleFileSelect = (file: File) => {
        const allowedTypes = ['video/mp4', 'video/quicktime', 'video/x-msvideo', 'video/x-matroska', 'video/webm', 'video/avi'];
        if (!allowedTypes.includes(file.type) && !file.name.match(/\.(mp4|mov|avi|mkv|webm)$/i)) {
            toast.error('Please select a video file (MP4, MOV, AVI, MKV, or WebM)');
            return;
        }
        setSelectedFile(file);
        if (!title) {
            // Auto-fill title from filename
            setTitle(file.name.replace(/\.[^/.]+$/, '').replace(/[_-]/g, ' '));
        }
    };

    const handleDrop = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        setIsDragOver(false);
        const file = e.dataTransfer.files[0];
        if (file) handleFileSelect(file);
    }, [title]);

    const handleDragOver = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        setIsDragOver(true);
    }, []);

    const handleDragLeave = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        setIsDragOver(false);
    }, []);

    const cancelUpload = () => {
        abortControllerRef.current?.abort();
        setIsUploading(false);
        setUploadProgress(0);
        setUploadSpeed('');
    };

    const handleUpload = async () => {
        if (!selectedFile || !title.trim()) {
            toast.error('Please add a title and select a file');
            return;
        }

        setIsUploading(true);
        setUploadProgress(0);
        setUploadEta('');
        setUploadPhase('preparing');
        const startTime = Date.now();

        try {
            // Step 1: Get pre-signed upload URL from API
            toast.loading('Preparing upload...', { id: 'upload-progress' });
            const { uploadUrl, objectKey } = await adminApi.getUploadUrl(
                selectedFile.name,
                selectedFile.type || 'video/mp4',
                course.id
            );

            // Step 2: Upload file directly to Wasabi using pre-signed URL
            abortControllerRef.current = new AbortController();
            setUploadPhase('uploading');
            toast.loading('Uploading to cloud...', { id: 'upload-progress' });
            const uploadStartTime = Date.now();

            await axios.put(uploadUrl, selectedFile, {
                headers: {
                    'Content-Type': selectedFile.type || 'video/mp4',
                },
                // Performance: prevent axios from buffering or transforming the file
                maxContentLength: Infinity,
                maxBodyLength: Infinity,
                timeout: 0, // no timeout for large uploads
                transformRequest: [(data: any) => data], // pass File directly, skip JSON serialization
                signal: abortControllerRef.current.signal,
                onUploadProgress: (progressEvent) => {
                    const progress = progressEvent.total 
                        ? Math.round((progressEvent.loaded / progressEvent.total) * 100)
                        : 0;
                    setUploadProgress(progress);

                    // Calculate speed & ETA
                    const elapsed = (Date.now() - uploadStartTime) / 1000;
                    if (elapsed > 0.5 && progressEvent.loaded > 0) {
                        const speed = progressEvent.loaded / elapsed;
                        setUploadSpeed(formatFileSize(speed) + '/s');
                        
                        if (progressEvent.total && progress > 0 && progress < 100) {
                            const remaining = progressEvent.total - progressEvent.loaded;
                            const etaSeconds = remaining / speed;
                            if (etaSeconds < 60) {
                                setUploadEta(`~${Math.ceil(etaSeconds)}s left`);
                            } else {
                                setUploadEta(`~${Math.ceil(etaSeconds / 60)}m left`);
                            }
                        } else if (progress >= 100) {
                            setUploadEta('Finalizing...');
                        }
                    }
                }
            });

            // Step 3: Confirm upload with API (save metadata to DB)
            setUploadPhase('saving');
            toast.loading('Saving video metadata...', { id: 'upload-progress' });
            await adminApi.confirmVideoUpload(course.id, {
                title: title.trim(),
                description: desc.trim() || undefined,
                objectKey,
                originalFileName: selectedFile.name,
                fileSizeBytes: selectedFile.size,
            });

            toast.success('Video uploaded successfully! ☁️', { id: 'upload-progress' });
            onSuccess();
        } catch (error: any) {
            if (error.name === 'CanceledError' || error.code === 'ERR_CANCELED') {
                toast.dismiss('upload-progress');
                toast('Upload cancelled', { icon: '⚠️' });
            } else {
                console.error('Upload error:', error);
                toast.error('Upload failed: ' + (error.response?.data?.message || error.message), { id: 'upload-progress' });
            }
        } finally {
            setIsUploading(false);
            setUploadPhase('idle');
            abortControllerRef.current = null;
        }
    };

    // Delete video handler
    const deleteMutation = useMutation({
        mutationFn: (videoId: number) => adminApi.deleteCourseVideo(videoId),
        onSuccess: () => {
            toast.success('Video deleted');
            queryClient.invalidateQueries({ queryKey: ['hierarchy-tree'] });
            // Update local state
            course.videos = course.videos.filter((v: any) => v.id !== deleteMutation.variables);
        },
        onError: () => toast.error('Failed to delete video')
    });

    return (
        <div className="fixed inset-0 bg-black/60 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-xl w-full max-w-lg overflow-hidden shadow-2xl animate-in fade-in zoom-in-95 duration-200 flex flex-col max-h-[90vh]">
                {/* Header */}
                <div className="px-6 py-4 bg-gradient-to-r from-amber-500 to-amber-600 shrink-0">
                    <h3 className="text-lg font-bold text-white">Manage Course Videos</h3>
                    <p className="text-amber-100 text-sm">{course.name}</p>
                </div>
                
                <div className="overflow-y-auto flex-1">
                    {/* Existing Videos */}
                    {course.videos && course.videos.length > 0 && (
                        <div className="p-6 pb-2 border-b border-gray-200 bg-gray-50/50">
                            <h4 className="font-semibold text-gray-800 mb-3 text-sm uppercase tracking-wider">Existing Videos</h4>
                            <div className="space-y-3">
                                {course.videos.map((v: any, idx: number) => (
                                    <div key={v.id} className="p-3 bg-white shadow-sm border border-gray-200 rounded-lg flex items-center gap-3">
                                        <div className="w-6 h-6 rounded bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold shrink-0">{idx + 1}</div>
                                        <div className="flex-1 min-w-0">
                                            <p className="font-semibold text-sm text-gray-900 truncate">{v.title}</p>
                                            <div className="flex items-center gap-2 mt-0.5">
                                                <span className={`text-xs px-1.5 py-0.5 rounded font-medium ${v.isWasabiVideo ? 'bg-sky-50 text-sky-700' : 'bg-purple-50 text-purple-700'}`}>
                                                    {v.isWasabiVideo ? '☁️ Cloud' : '🔗 Embed'}
                                                </span>
                                                {v.isWasabiVideo && v.fileSizeBytes && (
                                                    <span className="text-xs text-gray-400">{formatFileSize(v.fileSizeBytes)}</span>
                                                )}
                                            </div>
                                            <button 
                                                type="button" 
                                                onClick={() => setPlayingVideoId(playingVideoId === v.id ? null : v.id)} 
                                                className="text-xs text-blue-600 hover:underline mt-1"
                                            >
                                                {playingVideoId === v.id ? 'Close Player' : 'Play Video'}
                                            </button>
                                            {playingVideoId === v.id && (
                                                <div className="mt-3 w-full aspect-video bg-black rounded-lg overflow-hidden">
                                                    {v.isWasabiVideo ? (
                                                        <WasabiVideoPlayer videoId={v.id} />
                                                    ) : (
                                                        <iframe src={v.videoUrl} className="w-full h-full border-0" allowFullScreen />
                                                    )}
                                                </div>
                                            )}
                                        </div>
                                        <button
                                            type="button"
                                            onClick={() => { if (confirm('Delete this video?')) deleteMutation.mutate(v.id); }}
                                            className="text-xs text-red-500 hover:text-red-700 hover:bg-red-50 px-2 py-1 rounded font-bold shrink-0"
                                        >
                                            🗑️
                                        </button>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* Tab Bar */}
                    <div className="flex border-b border-gray-200 mx-6 mt-4">
                        <button
                            type="button"
                            onClick={() => setActiveTab('upload')}
                            className={`px-4 py-2.5 text-sm font-bold transition-colors border-b-2 ${activeTab === 'upload' ? 'border-amber-500 text-amber-600' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
                        >
                            ☁️ Upload File
                        </button>
                        <button
                            type="button"
                            onClick={() => setActiveTab('embed')}
                            className={`px-4 py-2.5 text-sm font-bold transition-colors border-b-2 ${activeTab === 'embed' ? 'border-amber-500 text-amber-600' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
                        >
                            🔗 Embed URL
                        </button>
                    </div>

                    {/* Upload Tab */}
                    {activeTab === 'upload' && (
                        <div className="p-6 space-y-4">
                            {/* Drag & Drop Zone */}
                            <div
                                onDrop={handleDrop}
                                onDragOver={handleDragOver}
                                onDragLeave={handleDragLeave}
                                onClick={() => fileInputRef.current?.click()}
                                className={`relative border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all duration-200 ${
                                    isDragOver 
                                        ? 'border-amber-400 bg-amber-50 scale-[1.02]' 
                                        : selectedFile 
                                            ? 'border-emerald-300 bg-emerald-50' 
                                            : 'border-gray-300 bg-gray-50 hover:border-amber-300 hover:bg-amber-50/30'
                                }`}
                            >
                                <input
                                    ref={fileInputRef}
                                    type="file"
                                    accept="video/*,.mp4,.mov,.avi,.mkv,.webm"
                                    className="hidden"
                                    onChange={(e) => e.target.files?.[0] && handleFileSelect(e.target.files[0])}
                                />
                                {selectedFile ? (
                                    <div>
                                        <div className="text-3xl mb-2">✅</div>
                                        <p className="font-bold text-emerald-700 text-sm">{selectedFile.name}</p>
                                        <p className="text-xs text-emerald-600 mt-1">{formatFileSize(selectedFile.size)}</p>
                                        <button 
                                            type="button"
                                            onClick={(e) => { e.stopPropagation(); setSelectedFile(null); }}
                                            className="text-xs text-red-500 hover:underline mt-2"
                                        >
                                            Remove
                                        </button>
                                    </div>
                                ) : (
                                    <div>
                                        <div className="text-3xl mb-2">☁️</div>
                                        <p className="font-bold text-gray-700 text-sm">Drag & drop a video file here</p>
                                        <p className="text-xs text-gray-500 mt-1">or click to browse • MP4, MOV, AVI, MKV, WebM</p>
                                    </div>
                                )}
                            </div>

                            {/* Title */}
                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Video Title</label>
                                <input 
                                    type="text" 
                                    className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="e.g. Lesson 1: Introduction"
                                    value={title}
                                    onChange={e => setTitle(e.target.value)}
                                    disabled={isUploading}
                                />
                            </div>

                            {/* Description */}
                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Description (Optional)</label>
                                <textarea 
                                    rows={2}
                                    className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="Brief description of the lecture..."
                                    value={desc}
                                    onChange={e => setDesc(e.target.value)}
                                    disabled={isUploading}
                                />
                            </div>

                            {/* Progress Bar */}
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
                                        <div 
                                            className={`h-full rounded-full transition-all duration-300 ease-out ${
                                                uploadPhase === 'saving' 
                                                    ? 'bg-gradient-to-r from-emerald-400 to-emerald-500' 
                                                    : uploadPhase === 'preparing'
                                                        ? 'bg-gradient-to-r from-blue-400 to-blue-500 animate-pulse'
                                                        : 'bg-gradient-to-r from-amber-400 to-amber-500'
                                            }`}
                                            style={{ width: uploadPhase === 'preparing' ? '5%' : uploadPhase === 'saving' ? '100%' : `${uploadProgress}%` }}
                                        />
                                    </div>
                                    {uploadPhase === 'uploading' && (
                                        <p className="text-xs text-gray-400 italic">Upload speed depends on your internet connection and distance to Wasabi's EU data center.</p>
                                    )}
                                </div>
                            )}

                            {/* Actions */}
                            <div className="flex justify-end gap-3 pt-2">
                                {isUploading ? (
                                    <button 
                                        type="button" 
                                        onClick={cancelUpload} 
                                        className="px-5 py-2 text-red-600 font-semibold hover:bg-red-50 rounded-lg transition-colors"
                                    >
                                        Cancel Upload
                                    </button>
                                ) : (
                                    <>
                                        <button type="button" onClick={onClose} className="px-5 py-2 text-gray-600 font-semibold hover:bg-gray-100 rounded-lg transition-colors">
                                            Close
                                        </button>
                                        <button
                                            type="button"
                                            onClick={handleUpload}
                                            disabled={!selectedFile || !title.trim()}
                                            className="px-5 py-2 bg-amber-500 hover:bg-amber-600 text-white font-bold rounded-lg transition-colors shadow-sm flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            ☁️ Upload to Cloud
                                        </button>
                                    </>
                                )}
                            </div>
                        </div>
                    )}

                    {/* Embed Tab (existing YouTube flow) */}
                    {activeTab === 'embed' && (
                        <form onSubmit={onSubmitEmbed} className="p-6 space-y-4">
                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Video Title</label>
                                <input 
                                    required 
                                    type="text" 
                                    className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="e.g. Lesson 1: Introduction"
                                    value={title}
                                    onChange={e => setTitle(e.target.value)}
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Video Embed URL (YouTube)</label>
                                <input 
                                    required 
                                    type="url" 
                                    className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="https://youtu.be/... or https://youtube.com/watch?v=..."
                                    value={url}
                                    onChange={e => setUrl(e.target.value)}
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-semibold text-gray-700 mb-1">Description (Optional)</label>
                                <textarea 
                                    rows={2}
                                    className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-amber-300 focus:outline-none"
                                    placeholder="Brief description of the lecture..."
                                    value={desc}
                                    onChange={e => setDesc(e.target.value)}
                                />
                            </div>
                            
                            <div className="flex justify-end gap-3 pt-2">
                                <button type="button" onClick={onClose} className="px-5 py-2 text-gray-600 font-semibold hover:bg-gray-100 rounded-lg transition-colors">
                                    Close
                                </button>
                                <button disabled={addMutation.isPending} type="submit" className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg transition-colors shadow-sm flex items-center gap-2 disabled:opacity-50">
                                    {addMutation.isPending ? 'Saving...' : '🔗 Embed Video'}
                                </button>
                            </div>
                        </form>
                    )}
                </div>
            </div>
        </div>
    );
}
