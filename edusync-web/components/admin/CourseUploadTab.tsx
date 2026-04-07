// @ts-nocheck
'use client';

import { useState } from 'react';
import { adminApi } from '@/lib/adminApi';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

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
                    <p className="text-sm text-gray-400 mt-0.5">Navigate the hierarchy to embed Wasabi video links into courses.</p>
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
    
    // Icon mapping
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
                        <span className={`font-medium ${isCourse ? 'text-blue-600font-bold' : 'text-gray-800'}`}>
                            {node.type === 'Course' ? `${node.courseCode} - ${node.courseTitle}` : node.name}
                        </span>
                    </div>
                    {isCourse && (
                        <button 
                            onClick={(e) => { e.stopPropagation(); onAddVideo({id: node.id, name: node.courseTitle, videos: node.videos || []}); }}
                            className={node.videos?.length > 0 ? "bg-blue-100 hover:bg-blue-200 text-blue-700 px-3 py-1 text-xs font-bold rounded-lg transition-colors" : "bg-blue-50 hover:bg-blue-100 text-blue-600 px-3 py-1 text-xs font-bold rounded-lg transition-colors"}
                        >
                            {node.videos?.length > 0 ? `View Videos (${node.videos.length})` : '+ Add Video Link'}
                        </button>
                    )}
                </div>
            </div>

            {/* Recursively render children if expanded */}
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

            {/* Render Videos directly under a Course if it's expanded */}
            {expanded && isCourse && node.videos?.length > 0 && (
                <div className="mt-2 mb-4 space-y-2" style={{ paddingLeft: `${(depth + 1) * 28 + 12}px` }}>
                    {node.videos.map((video: any) => (
                        <div key={video.id} className="mb-2">
                            <div className="flex items-center gap-3 bg-slate-50 border border-slate-200 p-3 rounded-lg mr-4 group">
                                <div className="w-8 h-8 rounded bg-slate-200 flex items-center justify-center text-lg shrink-0">🎬</div>
                                <div className="flex-1 min-w-0">
                                    <h4 className="text-sm font-bold text-slate-800 truncate">{video.title}</h4>
                                    <p className="text-xs text-slate-500 truncate">{video.description || 'No description'}</p>
                                </div>
                                <div className="shrink-0">
                                    <button 
                                        type="button" 
                                        onClick={() => setPlayingVideoId(playingVideoId === video.id ? null : video.id)} 
                                        className="text-xs text-blue-600 hover:text-blue-800 hover:underline font-bold px-2 py-1 bg-blue-50 rounded"
                                    >
                                        {playingVideoId === video.id ? 'Close Player' : 'Play Video'}
                                    </button>
                                </div>
                            </div>
                            {playingVideoId === video.id && (
                                <div className="mt-2 w-full max-w-2xl aspect-video bg-black rounded-lg overflow-hidden border border-slate-200 shadow-sm mr-4">
                                    <iframe src={video.videoUrl} title={video.title} className="w-full h-full border-0" allowFullScreen />
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

function AddVideoModal({ course, onClose, onSuccess }: any) {
    const [title, setTitle] = useState('');
    const [desc, setDesc] = useState('');
    const [url, setUrl] = useState('');
    const [playingVideoId, setPlayingVideoId] = useState<number | null>(null);

    const addMutation = useMutation({
        mutationFn: (data: any) => adminApi.addCourseVideo(course.id, data),
        onSuccess: () => {
            toast.success('Video link embedded!');
            onSuccess();
        },
        onError: () => toast.error('Failed to embed video')
    });

    const onSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        
        // Auto-convert standard YouTube links to embed format
        let finalUrl = url;
        if (finalUrl.includes('youtube.com/watch?v=')) {
            finalUrl = finalUrl.replace('watch?v=', 'embed/');
            finalUrl = finalUrl.split('&')[0]; // Strip extra params
        } else if (finalUrl.includes('youtu.be/')) {
            finalUrl = finalUrl.replace('youtu.be/', 'youtube.com/embed/');
            finalUrl = finalUrl.split('?')[0]; // Strip extra params
        }

        addMutation.mutate({ title, description: desc, videoUrl: finalUrl });
    };

    return (
        <div className="fixed inset-0 bg-black/60 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-lg w-full max-w-lg overflow-hidden shadow-2xl animate-in fade-in zoom-in-95 duration-200 flex flex-col max-h-[90vh]">
                <div className="px-6 py-4 bg-gradient-to-r from-amber-500 to-amber-600 border-b border-blue-300shrink-0">
                    <h3 className="text-lg font-bold text-white">Manage Course Videos</h3>
                    <p className="text-blue-600text-sm">{course.name}</p>
                </div>
                
                <div className="overflow-y-auto flex-1">
                    {course.videos && course.videos.length > 0 && (
                        <div className="p-6 pb-2 border-b border-gray-200 bg-gray-50/50">
                            <h4 className="font-semibold text-gray-800 mb-3 text-sm uppercase tracking-wider">Existing Videos</h4>
                            <div className="space-y-3">
                                {course.videos.map((v: any, idx: number) => (
                                    <div key={v.id} className="p-3 bg-white shadow-sm border border-gray-200 rounded-lg flex items-center gap-3">
                                        <div className="w-6 h-6 rounded bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold shrink-0">{idx + 1}</div>
                                        <div className="flex-1 min-w-0">
                                            <p className="font-semibold text-sm text-gray-900 truncate">{v.title}</p>
                                            <button 
                                                type="button" 
                                                onClick={() => setPlayingVideoId(playingVideoId === v.id ? null : v.id)} 
                                                className="text-xs text-blue-600 hover:underline"
                                            >
                                                {playingVideoId === v.id ? 'Close Player' : 'Play Video'}
                                            </button>
                                            {playingVideoId === v.id && (
                                                <div className="mt-3 w-full aspect-video bg-black rounded-lg overflow-hidden">
                                                    <iframe src={v.videoUrl} className="w-full h-full border-0" allowFullScreen />
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    <form onSubmit={onSubmit} className="p-6 space-y-4">
                        <h4 className="font-semibold text-gray-800 text-sm uppercase tracking-wider">Add New Video</h4>
                        <div>
                            <label className="block text-sm font-semibold text-gray-700 mb-1">Video Title</label>
                            <input 
                                required 
                                type="text" 
                                className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-300focus:outline-none"
                                placeholder="e.g. Lesson 1: Introduction"
                                value={title}
                                onChange={e => setTitle(e.target.value)}
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-semibold text-gray-700 mb-1">Video Embed URL (Wasabi or YouTube)</label>
                            <input 
                                required 
                                type="url" 
                                className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-300focus:outline-none"
                                placeholder="https://youtu.be/... or https://s3.wasabisys.com/..."
                                value={url}
                                onChange={e => setUrl(e.target.value)}
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-semibold text-gray-700 mb-1">Description (Optional)</label>
                            <textarea 
                                rows={3}
                                className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-300focus:outline-none"
                                placeholder="Brief description of the lecture..."
                                value={desc}
                                onChange={e => setDesc(e.target.value)}
                            />
                        </div>
                        
                        <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
                            <button type="button" onClick={onClose} className="px-5 py-2 text-gray-600 font-semibold hover:bg-gray-100 rounded-lg transition-colors">
                                Close
                            </button>
                            <button disabled={addMutation.isPending} type="submit" className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg transition-colors shadow-sm flex items-center gap-2 disabled:opacity-50">
                                {addMutation.isPending ? 'Saving...' : 'Add Video'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
}
