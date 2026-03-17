'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState, useEffect } from 'react';
import AuthGuard from '@/components/AuthGuard';
import DashboardLayout from '@/components/DashboardLayout';
import { useAuthStore } from '@/lib/store';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { courseApi, sessionApi, attendanceApi, materialsApi } from '@/lib/api';
import { API_BASE_URL } from '@/lib/config';
import { PlusIcon, AcademicCapIcon, CalendarIcon, DocumentTextIcon, ClipboardDocumentCheckIcon, UserGroupIcon, PhotoIcon, ArrowDownTrayIcon, ArrowPathIcon } from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import type { Course, ClassSession } from '@/types';

const lecturerNav = [
  { id: 'overview', label: 'Dashboard', icon: '📊' },
  { id: 'courses', label: 'Courses', icon: '🎓' },
  { id: 'schedule', label: 'Schedule', icon: '📅' },
  { id: 'lectures', label: 'Preparation', icon: '📝' },
  { id: 'whiteboards', label: 'Whiteboards', icon: '🖊️' },
  { id: 'attendance', label: 'Attendance', icon: '📋' },
];

/* ─── Overview Tab ─── */
function OverviewTab() {
  return (
    <>
      <div className="mb-8">
        <h3 className="text-sm font-semibold text-[#9CA3AF] uppercase tracking-wider mb-4">Overview</h3>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {[
            { label: 'Total Courses', value: '—', icon: '🎓', bg: 'bg-[#FFF3E0]', color: 'text-[#FF6B35]' },
            { label: 'Active Sessions', value: '—', icon: '📡', bg: 'bg-emerald-50', color: 'text-emerald-600' },
            { label: 'Total Students', value: '—', icon: '👥', bg: 'bg-blue-50', color: 'text-blue-600' },
            { label: 'Whiteboards', value: '—', icon: '🖊️', bg: 'bg-purple-50', color: 'text-purple-600' },
          ].map((stat, i) => (
            <div key={i} className="bg-white rounded-2xl p-5 border border-gray-100 hover:shadow-md transition-shadow">
              <div className={`w-10 h-10 ${stat.bg} rounded-xl flex items-center justify-center text-xl mb-3`}>{stat.icon}</div>
              <p className={`text-2xl font-extrabold ${stat.color}`}>{stat.value}</p>
              <p className="text-xs text-[#9CA3AF] mt-1">{stat.label}</p>
            </div>
          ))}
        </div>
      </div>

      <div>
        <h3 className="text-sm font-semibold text-[#9CA3AF] uppercase tracking-wider mb-4">Quick Access</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {[
            { title: 'Course Management', desc: 'Create, edit, and manage your courses.', icon: '🎓', accent: 'border-[#FF6B35]/20 hover:border-[#FF6B35]/50' },
            { title: 'Lecture Schedule', desc: 'View and manage your lecture schedule.', icon: '📅', accent: 'border-emerald-200 hover:border-emerald-400' },
            { title: 'Lecture Preparation', desc: 'Upload materials and prepare for lectures.', icon: '📝', accent: 'border-purple-200 hover:border-purple-400' },
            { title: 'Whiteboards Gallery', desc: 'View saved whiteboard drawings.', icon: '🖊️', accent: 'border-blue-200 hover:border-blue-400' },
            { title: 'Attendance Records', desc: 'View student attendance.', icon: '📋', accent: 'border-amber-200 hover:border-amber-400' },
          ].map((card, i) => (
            <div key={i} className={`bg-white rounded-2xl p-6 border-2 ${card.accent} hover:shadow-lg transition-all duration-300 h-full`}>
              <div className="text-3xl mb-4">{card.icon}</div>
              <h4 className="text-base font-bold text-[#1A1A2E] mb-2">{card.title}</h4>
              <p className="text-sm text-[#6B7280] leading-relaxed">{card.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}

/* ─── Courses Tab ─── */
function CoursesTab() {
  const [isCreating, setIsCreating] = useState(false);
  const [newCourse, setNewCourse] = useState({ courseCode: '', courseName: '', description: '', creditHours: 3, lecturerId: 1 });
  const queryClient = useQueryClient();

  const { data: courses, isLoading } = useQuery({ queryKey: ['courses'], queryFn: () => courseApi.getAll() });

  const createMutation = useMutation({
    mutationFn: courseApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courses'] });
      toast.success('Course created successfully!');
      setIsCreating(false);
      setNewCourse({ courseCode: '', courseName: '', description: '', creditHours: 3, lecturerId: 1 });
    },
    onError: () => toast.error('Failed to create course'),
  });

  const deleteMutation = useMutation({
    mutationFn: courseApi.delete,
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['courses'] }); toast.success('Course deleted!'); },
    onError: () => toast.error('Failed to delete course'),
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-lg font-bold text-[#1A1A2E]">Course Management</h3>
        <button onClick={() => setIsCreating(true)} className="flex items-center gap-2 bg-[#FF6B35] text-white px-4 py-2 rounded-lg hover:bg-[#e55a2b] transition-colors text-sm font-medium">
          <PlusIcon className="h-4 w-4" /> New Course
        </button>
      </div>

      {/* Create Course Modal */}
      {isCreating && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-8 max-w-2xl w-full mx-4">
            <h2 className="text-2xl font-bold text-gray-900 mb-6">Create New Course</h2>
            <form onSubmit={(e) => { e.preventDefault(); createMutation.mutate(newCourse); }} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Course Code</label>
                <input type="text" required value={newCourse.courseCode} onChange={(e) => setNewCourse({ ...newCourse, courseCode: e.target.value })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35] focus:border-transparent" placeholder="e.g., CS101" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Course Name</label>
                <input type="text" required value={newCourse.courseName} onChange={(e) => setNewCourse({ ...newCourse, courseName: e.target.value })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35] focus:border-transparent" placeholder="e.g., Introduction to Programming" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea value={newCourse.description} onChange={(e) => setNewCourse({ ...newCourse, description: e.target.value })} rows={3} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35] focus:border-transparent" placeholder="Course description..." />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Credit Hours</label>
                <input type="number" required min="1" max="6" value={newCourse.creditHours} onChange={(e) => setNewCourse({ ...newCourse, creditHours: parseInt(e.target.value) })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35] focus:border-transparent" />
              </div>
              <div className="flex space-x-4 pt-4">
                <button type="submit" disabled={createMutation.isPending} className="flex-1 bg-[#FF6B35] text-white px-6 py-3 rounded-lg hover:bg-[#e55a2b] transition-colors disabled:opacity-50">{createMutation.isPending ? 'Creating...' : 'Create Course'}</button>
                <button type="button" onClick={() => setIsCreating(false)} className="flex-1 bg-gray-200 text-gray-700 px-6 py-3 rounded-lg hover:bg-gray-300 transition-colors">Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Courses Grid */}
      {isLoading ? (
        <div className="text-center py-12"><div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-[#FF6B35]" /><p className="mt-4 text-gray-600">Loading courses...</p></div>
      ) : courses && courses.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {courses.map((course: Course) => (
            <div key={course.id} className="bg-white rounded-2xl border border-gray-100 p-6 hover:shadow-lg transition-shadow">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h3 className="text-lg font-bold text-gray-900">{course.courseCode}</h3>
                  <p className="text-sm text-gray-600">{course.creditHours} Credit Hours</p>
                </div>
                <button onClick={() => { if (confirm('Delete this course?')) deleteMutation.mutate(course.id); }} className="text-red-500 hover:text-red-700 text-sm">Delete</button>
              </div>
              <h4 className="text-xl font-semibold text-gray-800 mb-2">{course.courseName}</h4>
              {course.description && <p className="text-gray-600 text-sm mb-4 line-clamp-2">{course.description}</p>}
              <div className="flex items-center justify-between pt-4 border-t border-gray-100">
                <span className="text-sm text-gray-500">Created {new Date(course.createdAt).toLocaleDateString()}</span>
                <Link href={`/courses/${course.id}`} className="text-[#FF6B35] hover:text-[#e55a2b] text-sm font-medium">View Details →</Link>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="text-center py-12 bg-white rounded-2xl border border-gray-100">
          <AcademicCapIcon className="h-16 w-16 text-gray-300 mx-auto mb-4" />
          <h3 className="text-xl font-semibold text-gray-900 mb-2">No courses yet</h3>
          <p className="text-gray-600 mb-6">Get started by creating your first course</p>
          <button onClick={() => setIsCreating(true)} className="inline-flex items-center gap-2 bg-[#FF6B35] text-white px-6 py-3 rounded-lg hover:bg-[#e55a2b] transition-colors"><PlusIcon className="h-5 w-5" /> Create Course</button>
        </div>
      )}
    </div>
  );
}

/* ─── Schedule Tab ─── */
function ScheduleTab() {
  const [isCreating, setIsCreating] = useState(false);
  const [newSession, setNewSession] = useState({ courseId: 0, scheduledDate: '', topic: '', location: '', durationMinutes: 60, status: 'Scheduled' as const });
  const queryClient = useQueryClient();

  const { data: sessions, isLoading } = useQuery({ queryKey: ['sessions'], queryFn: () => sessionApi.getAll() });
  const { data: courses } = useQuery({ queryKey: ['courses'], queryFn: () => courseApi.getAll() });

  const createMutation = useMutation({
    mutationFn: sessionApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sessions'] });
      toast.success('Session scheduled!');
      setIsCreating(false);
      setNewSession({ courseId: 0, scheduledDate: '', topic: '', location: '', durationMinutes: 60, status: 'Scheduled' });
    },
    onError: () => toast.error('Failed to create session'),
  });

  const deleteMutation = useMutation({
    mutationFn: sessionApi.delete,
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['sessions'] }); toast.success('Session deleted!'); },
    onError: () => toast.error('Failed to delete session'),
  });

  const getCourseName = (courseId: number) => {
    const c = courses?.find((c: Course) => c.id === courseId);
    return c ? `${c.courseCode} - ${c.courseName}` : 'Unknown Course';
  };

  const formatDate = (d: string) => new Date(d).toLocaleString('en-US', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-lg font-bold text-[#1A1A2E]">Lecture Schedule</h3>
        <button onClick={() => setIsCreating(true)} className="flex items-center gap-2 bg-[#FF6B35] text-white px-4 py-2 rounded-lg hover:bg-[#e55a2b] transition-colors text-sm font-medium">
          <PlusIcon className="h-4 w-4" /> New Session
        </button>
      </div>

      {isCreating && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-8 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
            <h2 className="text-2xl font-bold text-gray-900 mb-6">Schedule New Session</h2>
            <form onSubmit={(e) => { e.preventDefault(); if (newSession.courseId === 0) { toast.error('Select a course'); return; } createMutation.mutate(newSession); }} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Course *</label>
                <select required value={newSession.courseId} onChange={(e) => setNewSession({ ...newSession, courseId: parseInt(e.target.value) })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]">
                  <option value={0}>Select a course...</option>
                  {courses?.map((c: Course) => <option key={c.id} value={c.id}>{c.courseCode} - {c.courseName}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Date & Time *</label>
                <input type="datetime-local" required value={newSession.scheduledDate} onChange={(e) => setNewSession({ ...newSession, scheduledDate: e.target.value })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Topic</label>
                <input type="text" value={newSession.topic} onChange={(e) => setNewSession({ ...newSession, topic: e.target.value })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]" placeholder="e.g., Introduction to Variables" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Location</label>
                <input type="text" value={newSession.location} onChange={(e) => setNewSession({ ...newSession, location: e.target.value })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]" placeholder="e.g., Room 101" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Duration (min)</label>
                <input type="number" required min="15" max="240" step="15" value={newSession.durationMinutes} onChange={(e) => setNewSession({ ...newSession, durationMinutes: parseInt(e.target.value) })} className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF6B35]" />
              </div>
              <div className="flex space-x-4 pt-4">
                <button type="submit" disabled={createMutation.isPending} className="flex-1 bg-[#FF6B35] text-white px-6 py-3 rounded-lg hover:bg-[#e55a2b] disabled:opacity-50">{createMutation.isPending ? 'Scheduling...' : 'Schedule Session'}</button>
                <button type="button" onClick={() => setIsCreating(false)} className="flex-1 bg-gray-200 text-gray-700 px-6 py-3 rounded-lg hover:bg-gray-300">Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="text-center py-12"><div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-[#FF6B35]" /><p className="mt-4 text-gray-600">Loading schedule...</p></div>
      ) : sessions && sessions.length > 0 ? (
        <div className="space-y-4">
          {sessions.map((session: ClassSession) => (
            <div key={session.id} className="bg-white rounded-2xl border border-gray-100 p-6 hover:shadow-lg transition-shadow">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center space-x-3 mb-2">
                    <span className={`px-3 py-1 rounded-full text-sm font-medium ${session.status === 'Scheduled' ? 'bg-blue-100 text-blue-800' : session.status === 'Completed' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>{session.status}</span>
                    <span className="text-sm text-gray-600">{session.durationMinutes} min</span>
                  </div>
                  <h3 className="text-xl font-bold text-gray-900 mb-2">{getCourseName(session.courseId)}</h3>
                  {session.topic && <p className="text-gray-700 mb-2">Topic: {session.topic}</p>}
                  <div className="flex items-center space-x-4 text-sm text-gray-600">
                    <span>📅 {formatDate(session.scheduledDate)}</span>
                    {session.location && <span>📍 {session.location}</span>}
                  </div>
                </div>
                <div className="flex flex-col space-y-2">
                  <Link href={`/lectures/${session.id}`} className="text-[#FF6B35] hover:text-[#e55a2b] text-sm font-medium">Prepare Lecture →</Link>
                  <button onClick={() => { if (confirm('Delete this session?')) deleteMutation.mutate(session.id); }} className="text-red-500 hover:text-red-700 text-sm">Delete</button>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="text-center py-12 bg-white rounded-2xl border border-gray-100">
          <CalendarIcon className="h-16 w-16 text-gray-300 mx-auto mb-4" />
          <h3 className="text-xl font-semibold text-gray-900 mb-2">No sessions scheduled</h3>
          <p className="text-gray-600 mb-6">Get started by scheduling your first lecture</p>
          <button onClick={() => setIsCreating(true)} className="inline-flex items-center gap-2 bg-[#FF6B35] text-white px-6 py-3 rounded-lg hover:bg-[#e55a2b]"><PlusIcon className="h-5 w-5" /> Schedule Session</button>
        </div>
      )}
    </div>
  );
}

/* ─── Lectures/Preparation Tab ─── */
function LecturesTab() {
  return (
    <div className="text-center bg-white rounded-2xl border border-gray-100 p-12">
      <DocumentTextIcon className="h-16 w-16 text-gray-300 mx-auto mb-4" />
      <h2 className="text-2xl font-bold text-gray-900 mb-4">Prepare Your Lectures</h2>
      <p className="text-gray-600 mb-6 max-w-2xl mx-auto">
        To prepare for a lecture, go to the Schedule tab and click &quot;Prepare Lecture&quot; on any scheduled session.
        You&apos;ll be able to add notes and upload materials for that specific lecture.
      </p>
    </div>
  );
}

/* ─── Whiteboards Tab ─── */
function WhiteboardsTab() {
  const { lecturer } = useAuthStore();
  const [whiteboards, setWhiteboards] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchWhiteboards = async () => {
    if (!lecturer?.id) return;
    setIsLoading(true);
    try {
      const data = await materialsApi.getByLecturer(lecturer.id);
      setWhiteboards(data || []);
    } catch {
      toast.error('Failed to load whiteboards');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { fetchWhiteboards(); }, [lecturer]);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-lg font-bold text-[#1A1A2E]">Saved Whiteboards</h3>
        <button onClick={fetchWhiteboards} className="flex items-center gap-2 text-[#FF6B35] hover:bg-[#FFF3E0] px-3 py-2 rounded-lg transition-colors text-sm font-medium">
          <ArrowPathIcon className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} /> Sync
        </button>
      </div>

      {isLoading ? (
        <div className="flex flex-col items-center py-16">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#FF6B35] mb-4" />
          <p className="text-gray-500">Loading drawings...</p>
        </div>
      ) : whiteboards.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {whiteboards.map((wb: any) => (
            <div key={wb.id} className="group bg-white rounded-2xl border border-gray-100 overflow-hidden hover:shadow-lg transition-all">
              <div className="aspect-video relative bg-slate-50 flex items-center justify-center overflow-hidden border-b border-gray-100">
                {wb.fileType?.match(/\.(mp4|webm|avi|mov)$/i) ? (
                  <video controls preload="metadata" className="w-full h-full object-contain bg-black" src={`${API_BASE_URL}/materials/${wb.id}/download`}>Your browser does not support the video tag.</video>
                ) : (
                  <img src={`${API_BASE_URL}/materials/${wb.id}/download`} alt={wb.fileName} className="max-h-full max-w-full object-contain p-1 group-hover:scale-105 transition-transform bg-white" onError={(e: any) => { e.target.src = 'https://via.placeholder.com/400x225?text=Load+Error'; }} />
                )}
              </div>
              <div className="p-5">
                <div className="flex justify-between items-start mb-3">
                  <div className="flex-1 min-w-0 pr-4">
                    <div className="flex items-center gap-2">
                      {wb.fileType?.match(/\.(mp4|webm|avi|mov)$/i) && <span className="px-2 py-0.5 bg-red-100 text-red-700 text-[10px] font-bold rounded-full">🎬 REC</span>}
                      <h4 className="text-base font-bold text-gray-900 truncate">{wb.fileName}</h4>
                    </div>
                    <p className="text-xs font-semibold text-[#FF6B35] uppercase tracking-wider mt-1">{wb.courseCode} • {wb.courseName}</p>
                  </div>
                  <a href={`${API_BASE_URL}/materials/${wb.id}/download`} download={wb.fileName} target="_blank" rel="noopener noreferrer" className="p-2.5 bg-[#FFF3E0] text-[#FF6B35] rounded-xl hover:bg-[#FF6B35] hover:text-white transition-all" title="Download">
                    <ArrowDownTrayIcon className="h-5 w-5" />
                  </a>
                </div>
                <div className="flex items-center text-xs text-gray-500 mt-4 pt-4 border-t border-gray-50">
                  <PhotoIcon className="h-3.5 w-3.5 mr-1" />
                  <span>Session: {wb.sessionDate ? new Date(wb.sessionDate).toLocaleDateString() : 'N/A'}</span>
                  <span className="mx-2">•</span>
                  <span>{wb.fileSize > 1024 * 1024 ? `${(wb.fileSize / (1024 * 1024)).toFixed(1)} MB` : `${(wb.fileSize / 1024).toFixed(1)} KB`}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-gray-100 p-16 text-center">
          <PhotoIcon className="h-16 w-16 text-gray-300 mx-auto mb-4" />
          <h3 className="text-xl font-bold text-gray-900 mb-2">No Whiteboards Saved</h3>
          <p className="text-gray-500">Drawings saved through the EduSync AI Desktop app will appear here.</p>
        </div>
      )}
    </div>
  );
}

/* ─── Attendance Tab ─── */
function AttendanceTab() {
  const { lecturer } = useAuthStore();
  const { data: sessions, isLoading } = useQuery({
    queryKey: ['all-sessions', lecturer?.id],
    queryFn: () => sessionApi.getAll({ lecturerId: lecturer?.id }),
    enabled: !!lecturer?.id,
  });

  if (isLoading) {
    return <div className="flex justify-center py-16"><div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#FF6B35]" /></div>;
  }

  return (
    <div>
      <h3 className="text-lg font-bold text-[#1A1A2E] mb-6">Attendance Records</h3>
      <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Course</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Topic</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Present</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {sessions && sessions.length > 0 ? (
                sessions.map((s: any) => (
                  <tr key={s.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-bold text-gray-900">{s.course?.courseCode}</div>
                      <div className="text-xs text-gray-500">{s.course?.courseName || s.course?.courseTitle || 'Unknown'}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{s.topic || 'Untitled'}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      <div className="flex items-center"><CalendarIcon className="h-4 w-4 mr-1.5" />{new Date(s.scheduledDate).toLocaleDateString()}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center text-sm font-semibold text-[#FF6B35]"><UserGroupIcon className="h-4 w-4 mr-1.5" />{s.attendanceCount || 0}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <Link href={`/lectures/${s.id}`} className="text-[#FF6B35] hover:text-[#e55a2b] bg-[#FFF3E0] px-3 py-1.5 rounded-md text-sm font-medium transition-colors">View List</Link>
                    </td>
                  </tr>
                ))
              ) : (
                <tr><td colSpan={5} className="px-6 py-12 text-center text-gray-500">No sessions found. Start a session in the desktop app to record attendance.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

/* ═══════════════════════════════════════════════
   Main Dashboard Page
   ═══════════════════════════════════════════════ */
export default function Home() {
  const router = useRouter();
  const { lecturer, logout } = useAuthStore();
  const [activeNav, setActiveNav] = useState('overview');

  const handleLogout = () => {
    logout();
    toast.success('Logged out successfully');
    router.push('/login');
  };

  return (
    <AuthGuard>
      <DashboardLayout
        role="lecturer"
        userName={lecturer?.fullName || 'Lecturer'}
        navItems={lecturerNav}
        activeNav={activeNav}
        onNavChange={setActiveNav}
        onLogout={handleLogout}
      >
        {activeNav === 'overview' && <OverviewTab />}
        {activeNav === 'courses' && <CoursesTab />}
        {activeNav === 'schedule' && <ScheduleTab />}
        {activeNav === 'lectures' && <LecturesTab />}
        {activeNav === 'whiteboards' && <WhiteboardsTab />}
        {activeNav === 'attendance' && <AttendanceTab />}
      </DashboardLayout>
    </AuthGuard>
  );
}
