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
  { id: 'whiteboards', label: 'Recorded Lectures', icon: '🎥' },
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
  const { data: courses, isLoading } = useQuery({ queryKey: ['courses'], queryFn: () => courseApi.getAll() });

  return (
    <div>
      <div className="mb-6">
        <h3 className="text-lg font-bold text-[#1A1A2E]">My Courses</h3>
        <p className="text-sm text-gray-400 mt-0.5">Courses assigned to you by the institution</p>
      </div>

      {isLoading ? (
        <div className="text-center py-12"><div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-[#FF6B35]" /><p className="mt-4 text-gray-600">Loading courses...</p></div>
      ) : courses && courses.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {courses.map((course: Course) => (
            <div key={course.id} className="bg-white rounded-2xl border border-gray-100 p-6 hover:shadow-lg transition-shadow">
              <div className="mb-4">
                <h3 className="text-lg font-bold text-gray-900">{course.courseCode}</h3>
                <p className="text-sm text-gray-600">{course.creditHours} Credit Hours</p>
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
          <h3 className="text-xl font-semibold text-gray-900 mb-2">No courses assigned</h3>
          <p className="text-gray-600">Courses will appear here once assigned by the institution.</p>
        </div>
      )}
    </div>
  );
}

/* ─── Schedule Tab — Monthly Calendar Timetable ─── */
const LECTURER_SEMESTER_START = new Date(2025, 0, 6);
const LECTURER_SEMESTER_WEEKS = 12;

interface LecturerCourseSlot {
  code: string;
  title: string;
  days: number[]; // 0=Sun..6=Sat
  startTime: string;
  endTime: string;
  room: string;
  color: string;
  bgColor: string;
  dotColor: string;
}

const LECTURER_COURSES: LecturerCourseSlot[] = [
  { code: 'CSC301', title: 'Data Structures', days: [1, 3, 5], startTime: '08:00', endTime: '09:00', room: 'LT-A', color: '#7c3aed', bgColor: '#f3e8ff', dotColor: '#7c3aed' },
  { code: 'CSC307', title: 'Software Eng.', days: [1, 3, 5], startTime: '10:00', endTime: '11:00', room: 'LT-C', color: '#ea580c', bgColor: '#ffedd5', dotColor: '#ea580c' },
  { code: 'CSC311', title: 'Artificial Intelligence', days: [1, 3], startTime: '11:30', endTime: '12:30', room: 'LT-B', color: '#059669', bgColor: '#d1fae5', dotColor: '#059669' },
  { code: 'EEE301', title: 'Signal Processing', days: [1, 3, 5], startTime: '14:00', endTime: '15:00', room: 'Lab-2', color: '#db2777', bgColor: '#fce7f3', dotColor: '#db2777' },
];

const L_MONTH_NAMES = ['January','February','March','April','May','June','July','August','September','October','November','December'];
const L_DAY_HEADERS = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];

function getLecturerCoursesForDate(date: Date): LecturerCourseSlot[] {
  const dayOfWeek = date.getDay();
  const semEnd = new Date(LECTURER_SEMESTER_START);
  semEnd.setDate(semEnd.getDate() + LECTURER_SEMESTER_WEEKS * 7);
  if (date < LECTURER_SEMESTER_START || date >= semEnd) return [];
  return LECTURER_COURSES.filter(c => c.days.includes(dayOfWeek));
}

function ScheduleTab() {
  const [viewMonth, setViewMonth] = useState(0);
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);

  const year = 2025;
  const month = viewMonth;
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const firstDay = new Date(year, month, 1).getDay();
  const today = new Date();
  const isToday = (d: number) => today.getFullYear() === year && today.getMonth() === month && today.getDate() === d;

  const cells: (number | null)[] = [];
  for (let i = 0; i < firstDay; i++) cells.push(null);
  for (let d = 1; d <= daysInMonth; d++) cells.push(d);
  while (cells.length % 7 !== 0) cells.push(null);

  const selCourses = selectedDate ? getLecturerCoursesForDate(selectedDate) : [];

  return (
    <div className="space-y-5 pb-10">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Teaching Schedule 📅</h2>
          <p className="text-sm text-gray-400 mt-0.5">Your lecture timetable for the semester</p>
        </div>
        <div className="flex items-center gap-1 bg-gray-100 rounded-full p-1">
          <button className="px-4 py-1.5 text-xs font-bold rounded-full bg-[#FF6B35] text-white shadow-sm">Month</button>
          <button className="px-4 py-1.5 text-xs font-medium rounded-full text-gray-500 hover:text-gray-700 transition-colors">Week</button>
          <button className="px-4 py-1.5 text-xs font-medium rounded-full text-gray-500 hover:text-gray-700 transition-colors">Day</button>
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-5">
        {/* Main Calendar */}
        <div className="flex-1 bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
            <div className="flex items-center gap-3">
              <h3 className="text-lg font-bold text-gray-900">{L_MONTH_NAMES[month]} {year}</h3>
              <span className="text-xs bg-orange-100 text-[#FF6B35] font-bold px-2 py-0.5 rounded-full">{LECTURER_COURSES.length} courses</span>
            </div>
            <div className="flex items-center gap-1">
              <button onClick={() => setViewMonth(m => Math.max(0, m - 1))} disabled={viewMonth === 0} className="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-30 transition-colors">
                <svg className="w-4 h-4 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" /></svg>
              </button>
              <button onClick={() => setViewMonth(m => Math.min(2, m + 1))} disabled={viewMonth >= 2} className="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-30 transition-colors">
                <svg className="w-4 h-4 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" /></svg>
              </button>
            </div>
          </div>

          <div className="grid grid-cols-7 border-b border-gray-100">
            {L_DAY_HEADERS.map(d => (
              <div key={d} className="px-2 py-3 text-center text-xs font-bold text-gray-400 uppercase tracking-wider">{d}</div>
            ))}
          </div>

          <div className="grid grid-cols-7">
            {cells.map((day, idx) => {
              if (day === null) return <div key={`e-${idx}`} className="min-h-[100px] bg-gray-50/50 border-b border-r border-gray-50" />;
              const date = new Date(year, month, day);
              const courses = getLecturerCoursesForDate(date);
              const dayIsToday = isToday(day);
              const isSel = selectedDate?.getDate() === day && selectedDate?.getMonth() === month;
              return (
                <div key={day} onClick={() => setSelectedDate(date)} className={`min-h-[100px] p-1.5 border-b border-r border-gray-100 cursor-pointer transition-all hover:bg-orange-50/50 ${isSel ? 'bg-orange-50 ring-2 ring-[#FF6B35] ring-inset' : ''}`}>
                  <div className={`text-xs font-bold mb-1 w-6 h-6 flex items-center justify-center rounded-full ${dayIsToday ? 'bg-[#FF6B35] text-white' : 'text-gray-700'}`}>{day}</div>
                  <div className="space-y-0.5">
                    {courses.slice(0, 3).map(c => (
                      <div key={c.code} className="rounded-md px-1.5 py-0.5 text-[10px] font-bold truncate leading-tight" style={{ backgroundColor: c.bgColor, color: c.color }} title={`${c.code} — ${c.title}\n${c.startTime} – ${c.endTime} • ${c.room}`}>
                        {c.code} <span className="font-normal opacity-70">{c.startTime}</span>
                      </div>
                    ))}
                    {courses.length > 3 && <div className="text-[9px] font-bold text-[#FF6B35] pl-1">+{courses.length - 3} more</div>}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Right Sidebar */}
        <div className="w-full lg:w-[280px] space-y-5 flex-shrink-0">
          {/* Mini Calendar */}
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-bold text-gray-900">{L_MONTH_NAMES[month]} {year}</h4>
              <div className="flex gap-1">
                <button onClick={() => setViewMonth(m => Math.max(0, m - 1))} disabled={viewMonth === 0} className="p-1 rounded hover:bg-gray-100 disabled:opacity-30"><svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" /></svg></button>
                <button onClick={() => setViewMonth(m => Math.min(2, m + 1))} disabled={viewMonth >= 2} className="p-1 rounded hover:bg-gray-100 disabled:opacity-30"><svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" /></svg></button>
              </div>
            </div>
            <div className="grid grid-cols-7 gap-0.5 text-center">
              {['Su','Mo','Tu','We','Th','Fr','Sa'].map(d => <div key={d} className="text-[10px] font-bold text-gray-400 py-1">{d}</div>)}
              {cells.map((day, idx) => (
                <button key={idx} onClick={() => day && setSelectedDate(new Date(year, month, day))} disabled={!day} className={`text-[11px] py-1 rounded-full font-medium transition-all ${!day ? 'invisible' : isToday(day!) ? 'bg-[#FF6B35] text-white font-bold' : selectedDate?.getDate() === day && selectedDate?.getMonth() === month ? 'bg-orange-100 text-[#FF6B35] font-bold' : 'text-gray-600 hover:bg-orange-50'}`}>{day || ''}</button>
              ))}
            </div>
          </div>

          {/* Day Detail */}
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-bold text-gray-900">{selectedDate ? `${L_DAY_HEADERS[selectedDate.getDay()]}, ${L_MONTH_NAMES[selectedDate.getMonth()]} ${selectedDate.getDate()}` : 'Today\'s Classes'}</h4>
              <span className="text-[10px] font-bold text-[#FF6B35] bg-orange-50 px-2 py-0.5 rounded-full">{selCourses.length || getLecturerCoursesForDate(today).length} classes</span>
            </div>
            <div className="space-y-2">
              {(selCourses.length > 0 ? selCourses : getLecturerCoursesForDate(today)).map(c => (
                <div key={c.code} className="flex items-center gap-2.5 p-2.5 rounded-xl hover:bg-gray-50 transition-colors">
                  <div className="w-1 h-10 rounded-full flex-shrink-0" style={{ backgroundColor: c.dotColor }} />
                  <div className="flex-1 min-w-0">
                    <p className="text-xs font-bold text-gray-900 truncate">{c.title}</p>
                    <p className="text-[10px] text-gray-400">{c.startTime} – {c.endTime} • {c.room}</p>
                  </div>
                  <span className="text-[10px] font-bold px-2 py-0.5 rounded-full flex-shrink-0" style={{ backgroundColor: c.bgColor, color: c.color }}>{c.code}</span>
                </div>
              ))}
              {selCourses.length === 0 && getLecturerCoursesForDate(today).length === 0 && (
                <p className="text-xs text-gray-400 text-center py-4">No classes scheduled</p>
              )}
            </div>
          </div>

          {/* Course Legend */}
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
            <h4 className="text-sm font-bold text-gray-900 mb-3">My Teaching Load</h4>
            <div className="space-y-2">
              {LECTURER_COURSES.map(c => (
                <div key={c.code} className="flex items-center gap-2">
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
