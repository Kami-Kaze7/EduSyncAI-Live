'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { adminApi } from '@/lib/adminApi';
import { courseApi } from '@/lib/api';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import DashboardLayout from '@/components/DashboardLayout';

export default function AdminDashboard() {
    const router = useRouter();
    const queryClient = useQueryClient();
    const [activeTab, setActiveTab] = useState<'lecturers' | 'courses' | 'students'>('lecturers');

    useEffect(() => {
        const token = localStorage.getItem('adminToken');
        if (!token) {
            router.push('/admin/login');
        }
    }, [router]);

    const handleLogout = () => {
        localStorage.removeItem('adminToken');
        localStorage.removeItem('adminUser');
        toast.success('Logged out successfully');
        router.push('/admin/login');
    };

    const adminUser = typeof window !== 'undefined' ? JSON.parse(localStorage.getItem('adminUser') || '{"fullName":"Admin"}') : { fullName: 'Admin' };

    const adminNav = [
        { id: 'lecturers', label: 'Lecturers', icon: '👨‍🏫' },
        { id: 'courses', label: 'Courses', icon: '🎓' },
        { id: 'students', label: 'Students', icon: '👨‍🎓' },
    ];

    return (
        <DashboardLayout
            role="admin"
            userName={adminUser?.fullName || 'Admin'}
            navItems={adminNav}
            activeNav={activeTab}
            onNavChange={(id) => setActiveTab(id as any)}
            onLogout={handleLogout}
        >
            <div>
                {activeTab === 'lecturers' && <LecturersTab />}
                {activeTab === 'courses' && <CoursesTab />}
                {activeTab === 'students' && <StudentsTab />}
            </div>
        </DashboardLayout>
    );
}

// ═══════════════════════════
//  Lecturers Tab
// ═══════════════════════════
// Dummy faculty/department assignments for lecturers
const LECTURER_META: Record<string, { faculty: string; department: string; courses: { code: string; year: number }[] }> = {};

function assignLecturerMeta(lecturer: any) {
    if (LECTURER_META[lecturer.id]) return LECTURER_META[lecturer.id];
    
    const fIdx = lecturer.id % FACULTIES.length;
    const f = FACULTIES[fIdx];
    const dIdx = (lecturer.id * 3) % f.departments.length;
    
    // Assign 1-3 random courses with years
    const numCourses = (lecturer.id % 3) + 1;
    const courses = [];
    for (let i = 0; i < numCourses; i++) {
        const year = ((lecturer.id + i) % 4) + 1;
        courses.push({
            code: `${f.departments[dIdx].substring(0, 3).toUpperCase()}${(year * 100) + i + 1}`,
            year
        });
    }
    
    const meta = { faculty: f.name, department: f.departments[dIdx], courses };
    LECTURER_META[lecturer.id] = meta;
    return meta;
}

function LecturersTab() {
    const queryClient = useQueryClient();
    const [showAddModal, setShowAddModal] = useState(false);
    const [showImportModal, setShowImportModal] = useState(false);
    const [filterFaculty, setFilterFaculty] = useState('all');
    const [filterDept, setFilterDept] = useState('all');

    const { data: lecturers, isLoading } = useQuery({
        queryKey: ['lecturers'],
        queryFn: adminApi.getLecturers,
    });

    const deleteMutation = useMutation({
        mutationFn: adminApi.deleteLecturer,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['lecturers'] });
            toast.success('Lecturer deleted successfully');
        },
        onError: () => toast.error('Failed to delete lecturer'),
    });

    const handleDelete = (id: number) => {
        if (confirm('Are you sure you want to delete this lecturer?')) {
            deleteMutation.mutate(id);
        }
    };

    // Enrich lecturers with metadata
    const enrichedLecturers = (lecturers || []).map((l: any) => ({ ...l, ...assignLecturerMeta(l) }));

    const allFaculties = [...new Set(enrichedLecturers.map((l: any) => l.faculty))];
    const allDepts = filterFaculty === 'all'
        ? [...new Set(enrichedLecturers.map((l: any) => l.department))]
        : [...new Set(enrichedLecturers.filter((l: any) => l.faculty === filterFaculty).map((l: any) => l.department))];

    const filtered = enrichedLecturers.filter((l: any) => {
        if (filterFaculty !== 'all' && l.faculty !== filterFaculty) return false;
        if (filterDept !== 'all' && l.department !== filterDept) return false;
        return true;
    });

    const grouped: Record<string, Record<string, any[]>> = {};
    filtered.forEach((l: any) => {
        if (!grouped[l.faculty]) grouped[l.faculty] = {};
        if (!grouped[l.faculty][l.department]) grouped[l.faculty][l.department] = [];
        grouped[l.faculty][l.department].push(l);
    });

    const yearColors: Record<number, string> = {
        1: 'bg-emerald-100 text-emerald-700',
        2: 'bg-blue-100 text-blue-700',
        3: 'bg-purple-100 text-purple-700',
        4: 'bg-amber-100 text-amber-700',
    };

    return (
        <div>
            <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">Lecturer Directory</h2>
                    <p className="text-sm text-gray-400 mt-0.5">{enrichedLecturers.length} registered lecturers</p>
                </div>
                <div className="flex gap-3">
                    <button onClick={() => setShowImportModal(true)} className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors text-sm">
                        Import Excel
                    </button>
                    <button onClick={() => setShowAddModal(true)} className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors text-sm">
                        + Add Lecturer
                    </button>
                </div>
            </div>

            {/* Filters */}
            <div className="bg-white rounded-xl border border-gray-100 p-4 mb-5">
                <div className="flex flex-wrap items-center gap-3">
                    <span className="text-xs font-bold text-gray-400 uppercase tracking-wider">Filter by:</span>

                    <select
                        value={filterFaculty}
                        onChange={(e) => { setFilterFaculty(e.target.value); setFilterDept('all'); }}
                        className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                    >
                        <option value="all">All Faculties</option>
                        {allFaculties.map(f => <option key={String(f)} value={String(f)}>{String(f)}</option>)}
                    </select>

                    <select
                        value={filterDept}
                        onChange={(e) => setFilterDept(e.target.value)}
                        className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                    >
                        <option value="all">All Departments</option>
                        {allDepts.map(d => <option key={String(d)} value={String(d)}>{String(d)}</option>)}
                    </select>
                </div>
                <div className="flex gap-2 mt-3">
                    <span className="text-xs text-gray-400">{filtered.length} lecturers shown</span>
                    {(filterFaculty !== 'all' || filterDept !== 'all') && (
                        <button onClick={() => { setFilterFaculty('all'); setFilterDept('all'); }} className="text-xs text-indigo-600 hover:text-indigo-700 font-medium">
                            Clear filters
                        </button>
                    )}
                </div>
            </div>

            {isLoading ? (
                <div className="text-center py-12">Loading...</div>
            ) : (
                <div className="space-y-6">
                    {Object.entries(grouped).map(([faculty, departments]) => (
                        <div key={faculty} className="bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-sm">
                            <div className="px-6 py-4 bg-gradient-to-r from-indigo-50 to-blue-50 border-b border-gray-100">
                                <div className="flex items-center justify-between">
                                    <h3 className="text-sm font-bold text-gray-900 flex items-center gap-2">
                                        <span className="w-7 h-7 rounded-lg bg-indigo-500 text-white flex items-center justify-center text-xs">🏛️</span>
                                        {faculty}
                                    </h3>
                                    <span className="text-xs text-gray-500 font-medium">
                                        {Object.values(departments).flat().length} lecturers
                                    </span>
                                </div>
                            </div>

                            <div className="divide-y divide-gray-50">
                                {Object.entries(departments).map(([dept, deptLecturers]) => (
                                    <div key={dept} className="px-6 py-5">
                                        <div className="flex items-center gap-2 mb-4">
                                            <span className="text-xs font-bold text-amber-600 bg-amber-50 px-2.5 py-1 rounded-full">{dept}</span>
                                            <span className="text-[10px] text-gray-400">{deptLecturers.length} lecturers</span>
                                        </div>
                                        
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                            {deptLecturers.map((lecturer: any) => (
                                                <div key={lecturer.id} className="p-4 rounded-xl border border-gray-100 bg-gray-50 hover:border-indigo-100 hover:shadow-md transition-all group relative">
                                                    <button onClick={() => handleDelete(lecturer.id)} className="absolute top-2 right-2 text-red-400 hover:text-red-600 opacity-0 group-hover:opacity-100 transition-opacity bg-white hover:bg-red-50 p-1.5 rounded-md shadow-sm">
                                                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                                                    </button>
                                                    
                                                    <div className="flex items-start gap-3 mb-4">
                                                        <div className="w-12 h-12 rounded-full bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white text-lg font-bold flex-shrink-0 shadow-sm ring-2 ring-white">
                                                            {lecturer.fullName?.charAt(0)?.toUpperCase() || '?'}
                                                        </div>
                                                        <div className="flex-1 min-w-0 pr-6">
                                                            <h4 className="text-base font-bold text-gray-900 truncate">{lecturer.fullName}</h4>
                                                            <div className="flex flex-col gap-0.5 mt-0.5">
                                                                <span className="text-xs text-gray-500 flex items-center gap-1">
                                                                    <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
                                                                    {lecturer.username}
                                                                </span>
                                                                <span className="text-xs text-gray-500 flex items-center gap-1">
                                                                    <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                                                                    {lecturer.email}
                                                                </span>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div className="pt-3 border-t border-gray-200/60">
                                                        <p className="text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-2">Teaching Portfolio</p>
                                                        <div className="flex flex-wrap gap-2">
                                                            {lecturer.courses.map((course: any, idx: number) => (
                                                                <div key={idx} className="flex items-center gap-1.5 bg-white border border-gray-200 px-2 py-1 rounded-md shadow-sm">
                                                                    <span className="text-xs font-bold text-gray-700">{course.code}</span>
                                                                    <span className="w-1 h-1 rounded-full bg-gray-300"></span>
                                                                    <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded ${yearColors[course.year]}`}>
                                                                        Yr {course.year}
                                                                    </span>
                                                                </div>
                                                            ))}
                                                        </div>
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                    {filtered.length === 0 && (
                        <div className="text-center py-12 bg-white rounded-2xl border border-gray-100">
                            <div className="text-4xl mb-3">👨‍🏫</div>
                            <h3 className="text-lg font-bold text-gray-900 mb-1">No lecturers found</h3>
                            <p className="text-sm text-gray-400">Try adjusting the filters or add new lecturers.</p>
                        </div>
                    )}
                </div>
            )}

            {showAddModal && <AddLecturerModal onClose={() => setShowAddModal(false)} />}
            {showImportModal && <ImportLecturersModal onClose={() => setShowImportModal(false)} />}
        </div>
    );
}

// ═══════════════════════════
//  Courses Tab (NEW)
// ═══════════════════════════
function CoursesTab() {
    const queryClient = useQueryClient();
    const [showCreateModal, setShowCreateModal] = useState(false);

    const { data: courses, isLoading } = useQuery({
        queryKey: ['admin-courses'],
        queryFn: adminApi.getCourses,
    });

    const { data: lecturers } = useQuery({
        queryKey: ['lecturers'],
        queryFn: adminApi.getLecturers,
    });

    const deleteMutation = useMutation({
        mutationFn: adminApi.deleteCourse,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['admin-courses'] });
            toast.success('Course deleted successfully');
        },
        onError: () => toast.error('Failed to delete course'),
    });

    const handleDelete = (id: number) => {
        if (confirm('Are you sure you want to delete this course?')) {
            deleteMutation.mutate(id);
        }
    };

    const getLecturerName = (lecturerId: number) => {
        const lec = lecturers?.find((l: any) => l.id === lecturerId);
        return lec ? lec.fullName : 'Unassigned';
    };

    const handleDownloadSyllabus = async (courseId: number, courseCode: string) => {
        try {
            toast.loading('Downloading syllabus...', { id: `download-${courseId}` });
            const blob = await courseApi.downloadSyllabus(courseId);
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${courseCode}_Syllabus.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            toast.success('Download complete', { id: `download-${courseId}` });
        } catch (error) {
            console.error('Download failed:', error);
            toast.error('Failed to download syllabus', { id: `download-${courseId}` });
        }
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">Course Management</h2>
                    <p className="text-sm text-gray-400 mt-0.5">Create courses and assign them to lecturers</p>
                </div>
                <button
                    onClick={() => setShowCreateModal(true)}
                    className="px-4 py-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition-colors font-medium"
                >
                    + Create Course
                </button>
            </div>

            {isLoading ? (
                <div className="text-center py-12"><div className="inline-block animate-spin rounded-full h-10 w-10 border-b-2 border-amber-600" /><p className="mt-3 text-gray-500">Loading courses...</p></div>
            ) : courses && courses.length > 0 ? (
                <div className="bg-white shadow-sm rounded-lg overflow-hidden">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Course Code</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Course Name</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Credits</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Assigned Lecturer</th>
                                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Syllabus</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {courses.map((course: any) => (
                                <tr key={course.id} className="hover:bg-gray-50 transition-colors">
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <span className="text-sm font-bold text-amber-700 bg-amber-50 px-2.5 py-1 rounded-md">{course.courseCode}</span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{course.courseName}</td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{course.creditHours}</td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <span className="text-sm text-gray-700 flex items-center gap-1.5">
                                            <span className="w-6 h-6 rounded-full bg-indigo-100 text-indigo-600 flex items-center justify-center text-[10px] font-bold">
                                                {getLecturerName(course.lecturerId).charAt(0).toUpperCase()}
                                            </span>
                                            {getLecturerName(course.lecturerId)}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-center">
                                        {course.syllabusPath ? (
                                            <button
                                                onClick={() => handleDownloadSyllabus(course.id, course.courseCode)}
                                                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 rounded-md text-xs font-medium transition-colors border border-indigo-200"
                                                title="Download Syllabus"
                                            >
                                                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                                                PDF
                                            </button>
                                        ) : (
                                            <span className="text-xs text-gray-400 italic">Not Uploaded</span>
                                        )}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-400">
                                        {new Date(course.createdAt).toLocaleDateString()}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                        <button onClick={() => handleDelete(course.id)} className="text-red-600 hover:text-red-900">Delete</button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            ) : (
                <div className="text-center py-16 bg-white rounded-2xl border border-gray-100">
                    <div className="text-5xl mb-4">🎓</div>
                    <h3 className="text-xl font-bold text-gray-900 mb-2">No courses yet</h3>
                    <p className="text-gray-500 mb-6">Create your first course and assign it to a lecturer</p>
                    <button onClick={() => setShowCreateModal(true)} className="px-6 py-3 bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition-colors font-medium">
                        + Create Course
                    </button>
                </div>
            )}

            {showCreateModal && <CreateCourseModal lecturers={lecturers || []} onClose={() => setShowCreateModal(false)} />}
        </div>
    );
}

// Create Course Modal
function CreateCourseModal({ lecturers, onClose }: { lecturers: any[]; onClose: () => void }) {
    const queryClient = useQueryClient();
    const [formData, setFormData] = useState({
        courseCode: '',
        courseName: '',
        description: '',
        creditHours: 3,
        lecturerId: 0,
    });

    const createMutation = useMutation({
        mutationFn: adminApi.createCourse,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['admin-courses'] });
            toast.success('Course created and assigned successfully!');
            onClose();
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to create course');
        },
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (formData.lecturerId === 0) {
            toast.error('Please select a lecturer');
            return;
        }
        createMutation.mutate(formData);
    };

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-xl p-6 w-full max-w-lg mx-4">
                <h3 className="text-lg font-bold text-gray-900 mb-1">Create New Course</h3>
                <p className="text-sm text-gray-400 mb-5">Fill in the details and assign to a lecturer</p>

                <form onSubmit={handleSubmit} className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Course Code *</label>
                            <input
                                type="text"
                                value={formData.courseCode}
                                onChange={(e) => setFormData({ ...formData, courseCode: e.target.value })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                                placeholder="e.g., CSC301"
                                required
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Credit Hours *</label>
                            <input
                                type="number"
                                min="1"
                                max="6"
                                value={formData.creditHours}
                                onChange={(e) => setFormData({ ...formData, creditHours: parseInt(e.target.value) })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                                required
                            />
                        </div>
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Course Name *</label>
                        <input
                            type="text"
                            value={formData.courseName}
                            onChange={(e) => setFormData({ ...formData, courseName: e.target.value })}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                            placeholder="e.g., Data Structures & Algorithms"
                            required
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                        <textarea
                            value={formData.description}
                            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                            rows={2}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                            placeholder="Brief course description..."
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Assign to Lecturer *</label>
                        <select
                            value={formData.lecturerId}
                            onChange={(e) => setFormData({ ...formData, lecturerId: parseInt(e.target.value) })}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                            required
                        >
                            <option value={0}>-- Select a lecturer --</option>
                            {lecturers.map((lec: any) => (
                                <option key={lec.id} value={lec.id}>
                                    {lec.fullName} ({lec.username})
                                </option>
                            ))}
                        </select>
                        {lecturers.length === 0 && (
                            <p className="text-xs text-amber-600 mt-1">No lecturers available. Add lecturers first.</p>
                        )}
                    </div>

                    <div className="flex gap-3 pt-3">
                        <button type="button" onClick={onClose} className="flex-1 px-4 py-2.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors">
                            Cancel
                        </button>
                        <button type="submit" disabled={createMutation.isPending} className="flex-1 px-4 py-2.5 bg-amber-600 text-white rounded-lg hover:bg-amber-700 disabled:opacity-50 transition-colors font-medium">
                            {createMutation.isPending ? 'Creating...' : 'Create & Assign'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

// ═══════════════════════════
//  Students Tab (Segmented)
// ═══════════════════════════

// Dummy faculty/department/year assignments for display
const STUDENT_FACULTIES: Record<string, { faculty: string; department: string; year: number }> = {};
const FACULTIES = [
    { name: 'Faculty of Science', departments: ['Computer Science', 'Mathematics', 'Physics', 'Chemistry', 'Biology'] },
    { name: 'Faculty of Engineering', departments: ['Electrical Engineering', 'Mechanical Engineering', 'Civil Engineering'] },
    { name: 'Faculty of Arts', departments: ['English', 'History', 'Philosophy'] },
];

function assignStudentMeta(student: any): { faculty: string; department: string; year: number } {
    if (STUDENT_FACULTIES[student.id]) return STUDENT_FACULTIES[student.id];
    // Deterministic assignment based on student id
    const fIdx = student.id % FACULTIES.length;
    const f = FACULTIES[fIdx];
    const dIdx = student.id % f.departments.length;
    const year = ((student.id * 7) % 4) + 1; // 1-4
    const meta = { faculty: f.name, department: f.departments[dIdx], year };
    STUDENT_FACULTIES[student.id] = meta;
    return meta;
}

function StudentsTab() {
    const queryClient = useQueryClient();
    const [showAddModal, setShowAddModal] = useState(false);
    const [showImportModal, setShowImportModal] = useState(false);
    const [filterFaculty, setFilterFaculty] = useState('all');
    const [filterDept, setFilterDept] = useState('all');
    const [filterYear, setFilterYear] = useState('all');
    const [viewMode, setViewMode] = useState<'table' | 'cards'>('cards');

    const { data: students, isLoading } = useQuery({
        queryKey: ['students'],
        queryFn: adminApi.getStudents,
    });

    const deleteMutation = useMutation({
        mutationFn: adminApi.deleteStudent,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['students'] });
            toast.success('Student deleted successfully');
        },
        onError: () => toast.error('Failed to delete student'),
    });

    const handleDelete = (id: number) => {
        if (confirm('Are you sure you want to delete this student?')) {
            deleteMutation.mutate(id);
        }
    };

    // Enrich students with faculty/dept/year
    const enrichedStudents = (students || []).map((s: any) => ({ ...s, ...assignStudentMeta(s) }));

    // Get unique values for filters
    const allFaculties = [...new Set(enrichedStudents.map((s: any) => s.faculty))];
    const allDepts = filterFaculty === 'all'
        ? [...new Set(enrichedStudents.map((s: any) => s.department))]
        : [...new Set(enrichedStudents.filter((s: any) => s.faculty === filterFaculty).map((s: any) => s.department))];

    // Apply filters
    const filtered = enrichedStudents.filter((s: any) => {
        if (filterFaculty !== 'all' && s.faculty !== filterFaculty) return false;
        if (filterDept !== 'all' && s.department !== filterDept) return false;
        if (filterYear !== 'all' && s.year !== parseInt(filterYear)) return false;
        return true;
    });

    // Group by faculty then department
    const grouped: Record<string, Record<string, any[]>> = {};
    filtered.forEach((s: any) => {
        if (!grouped[s.faculty]) grouped[s.faculty] = {};
        if (!grouped[s.faculty][s.department]) grouped[s.faculty][s.department] = [];
        grouped[s.faculty][s.department].push(s);
    });

    const yearColors: Record<number, string> = {
        1: 'bg-emerald-100 text-emerald-700',
        2: 'bg-blue-100 text-blue-700',
        3: 'bg-purple-100 text-purple-700',
        4: 'bg-amber-100 text-amber-700',
    };

    return (
        <div>
            {/* Header */}
            <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">Student Directory</h2>
                    <p className="text-sm text-gray-400 mt-0.5">{enrichedStudents.length} registered students</p>
                </div>
                <div className="flex gap-3">
                    <button onClick={() => setShowImportModal(true)} className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors text-sm">
                        Import Excel
                    </button>
                    <button onClick={() => setShowAddModal(true)} className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors text-sm">
                        + Add Student
                    </button>
                </div>
            </div>

            {/* Filters */}
            <div className="bg-white rounded-xl border border-gray-100 p-4 mb-5">
                <div className="flex flex-wrap items-center gap-3">
                    <span className="text-xs font-bold text-gray-400 uppercase tracking-wider">Filter by:</span>

                    <select
                        value={filterFaculty}
                        onChange={(e) => { setFilterFaculty(e.target.value); setFilterDept('all'); }}
                        className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                    >
                        <option value="all">All Faculties</option>
                        {allFaculties.map(f => <option key={f} value={f}>{f}</option>)}
                    </select>

                    <select
                        value={filterDept}
                        onChange={(e) => setFilterDept(e.target.value)}
                        className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                    >
                        <option value="all">All Departments</option>
                        {allDepts.map(d => <option key={d} value={d}>{d}</option>)}
                    </select>

                    <select
                        value={filterYear}
                        onChange={(e) => setFilterYear(e.target.value)}
                        className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                    >
                        <option value="all">All Years</option>
                        <option value="1">Year 1</option>
                        <option value="2">Year 2</option>
                        <option value="3">Year 3</option>
                        <option value="4">Year 4</option>
                    </select>

                    <div className="ml-auto flex items-center gap-1 bg-gray-100 rounded-lg p-0.5">
                        <button onClick={() => setViewMode('cards')} className={`px-3 py-1 rounded-md text-xs font-medium transition-colors ${viewMode === 'cards' ? 'bg-white shadow-sm text-gray-900' : 'text-gray-500'}`}>Cards</button>
                        <button onClick={() => setViewMode('table')} className={`px-3 py-1 rounded-md text-xs font-medium transition-colors ${viewMode === 'table' ? 'bg-white shadow-sm text-gray-900' : 'text-gray-500'}`}>Table</button>
                    </div>
                </div>
                <div className="flex gap-2 mt-3">
                    <span className="text-xs text-gray-400">{filtered.length} students shown</span>
                    {(filterFaculty !== 'all' || filterDept !== 'all' || filterYear !== 'all') && (
                        <button onClick={() => { setFilterFaculty('all'); setFilterDept('all'); setFilterYear('all'); }} className="text-xs text-amber-600 hover:text-amber-700 font-medium">
                            Clear filters
                        </button>
                    )}
                </div>
            </div>

            {isLoading ? (
                <div className="text-center py-12">Loading...</div>
            ) : viewMode === 'cards' ? (
                /* Card View - Grouped by Faculty & Department */
                <div className="space-y-6">
                    {Object.entries(grouped).map(([faculty, departments]) => (
                        <div key={faculty} className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-amber-50 to-orange-50 border-b border-gray-100">
                                <div className="flex items-center justify-between">
                                    <h3 className="text-sm font-bold text-gray-900 flex items-center gap-2">
                                        <span className="w-7 h-7 rounded-lg bg-amber-500 text-white flex items-center justify-center text-xs">🏛️</span>
                                        {faculty}
                                    </h3>
                                    <span className="text-xs text-gray-400 font-medium">
                                        {Object.values(departments).flat().length} students
                                    </span>
                                </div>
                            </div>

                            <div className="divide-y divide-gray-50">
                                {Object.entries(departments).map(([dept, deptStudents]) => (
                                    <div key={dept} className="px-6 py-4">
                                        <div className="flex items-center gap-2 mb-3">
                                            <span className="text-xs font-bold text-indigo-600 bg-indigo-50 px-2.5 py-1 rounded-full">{dept}</span>
                                            <span className="text-[10px] text-gray-400">{deptStudents.length} students</span>
                                        </div>
                                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                                            {deptStudents.map((student: any) => (
                                                <div key={student.id} className="flex items-center gap-3 p-3 rounded-xl bg-gray-50 hover:bg-gray-100 transition-colors group">
                                                    <div className="w-9 h-9 rounded-full bg-gradient-to-br from-amber-400 to-orange-500 flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
                                                        {student.fullName?.charAt(0)?.toUpperCase() || '?'}
                                                    </div>
                                                    <div className="flex-1 min-w-0">
                                                        <p className="text-sm font-semibold text-gray-900 truncate">{student.fullName}</p>
                                                        <p className="text-[10px] text-gray-400">{student.matricNumber}</p>
                                                    </div>
                                                    <div className="flex items-center gap-1.5 flex-shrink-0">
                                                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${yearColors[student.year] || 'bg-gray-100 text-gray-600'}`}>
                                                            Yr {student.year}
                                                        </span>
                                                        <button onClick={() => handleDelete(student.id)} className="text-red-400 hover:text-red-600 opacity-0 group-hover:opacity-100 transition-opacity text-xs">✕</button>
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                    {filtered.length === 0 && (
                        <div className="text-center py-12 bg-white rounded-2xl border border-gray-100">
                            <div className="text-4xl mb-3">👨‍🎓</div>
                            <h3 className="text-lg font-bold text-gray-900 mb-1">No students found</h3>
                            <p className="text-sm text-gray-400">Try adjusting the filters or add new students.</p>
                        </div>
                    )}
                </div>
            ) : (
                /* Table View */
                <div className="bg-white shadow-sm rounded-lg overflow-hidden">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Matric Number</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Full Name</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Faculty</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Department</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Year</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {filtered.map((student: any) => (
                                <tr key={student.id} className="hover:bg-gray-50 transition-colors">
                                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{student.matricNumber}</td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">{student.fullName}</td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{student.faculty}</td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{student.department}</td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <span className={`text-xs font-bold px-2 py-0.5 rounded-full ${yearColors[student.year] || 'bg-gray-100 text-gray-600'}`}>
                                            Year {student.year}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${student.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                                            {student.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                        <button onClick={() => handleDelete(student.id)} className="text-red-600 hover:text-red-900">Delete</button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {showAddModal && <AddStudentModal onClose={() => setShowAddModal(false)} />}
            {showImportModal && <ImportStudentsModal onClose={() => setShowImportModal(false)} />}
        </div>
    );
}

// ═══════════════════════════
//  Modals (unchanged)
// ═══════════════════════════

function AddLecturerModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [formData, setFormData] = useState({ username: '', fullName: '', email: '', password: '' });
    const [generatedPassword, setGeneratedPassword] = useState('');

    const createMutation = useMutation({
        mutationFn: adminApi.createLecturer,
        onSuccess: (data) => { queryClient.invalidateQueries({ queryKey: ['lecturers'] }); setGeneratedPassword(data.generatedPassword); toast.success('Lecturer created!'); },
        onError: (error: any) => toast.error(error.response?.data?.error || 'Failed to create lecturer'),
    });

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-md">
                <h3 className="text-lg font-semibold mb-4">Add Lecturer</h3>
                {generatedPassword ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">Lecturer created successfully!</p>
                            <p className="text-sm font-semibold">Generated Password:</p>
                            <p className="text-lg font-mono bg-white px-3 py-2 rounded mt-1">{generatedPassword}</p>
                            <p className="text-xs text-green-700 mt-2">Please save this password. It won&apos;t be shown again.</p>
                        </div>
                        <button onClick={onClose} className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700">Close</button>
                    </div>
                ) : (
                    <form onSubmit={(e) => { e.preventDefault(); createMutation.mutate(formData); }} className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Username</label>
                            <input type="text" value={formData.username} onChange={(e) => setFormData({ ...formData, username: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500" required />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Full Name</label>
                            <input type="text" value={formData.fullName} onChange={(e) => setFormData({ ...formData, fullName: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500" required />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                            <input type="email" value={formData.email} onChange={(e) => setFormData({ ...formData, email: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500" required />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
                            <div className="flex gap-2">
                                <input type="text" value={formData.password} onChange={(e) => setFormData({ ...formData, password: e.target.value })} className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500" placeholder="Leave empty to auto-generate" />
                                <button type="button" onClick={() => setFormData({ ...formData, password: Math.random().toString(36).slice(-8) })} className="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700">Generate</button>
                            </div>
                        </div>
                        <div className="flex gap-3 pt-4">
                            <button type="button" onClick={onClose} className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50">Cancel</button>
                            <button type="submit" disabled={createMutation.isPending} className="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50">{createMutation.isPending ? 'Creating...' : 'Create'}</button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}

function AddStudentModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [formData, setFormData] = useState({ matricNumber: '', fullName: '', email: '', password: '' });
    const [generatedPassword, setGeneratedPassword] = useState('');

    const createMutation = useMutation({
        mutationFn: adminApi.createStudent,
        onSuccess: (data) => { queryClient.invalidateQueries({ queryKey: ['students'] }); setGeneratedPassword(data.generatedPassword); toast.success('Student created!'); },
        onError: (error: any) => toast.error(error.response?.data?.error || 'Failed to create student'),
    });

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-md">
                <h3 className="text-lg font-semibold mb-4">Add Student</h3>
                {generatedPassword ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">Student created successfully!</p>
                            <p className="text-sm font-semibold">Generated Password:</p>
                            <p className="text-lg font-mono bg-white px-3 py-2 rounded mt-1">{generatedPassword}</p>
                            <p className="text-xs text-green-700 mt-2">Please save this password. It won&apos;t be shown again.</p>
                        </div>
                        <button onClick={onClose} className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700">Close</button>
                    </div>
                ) : (
                    <form onSubmit={(e) => { e.preventDefault(); createMutation.mutate(formData); }} className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Matric Number</label>
                            <input type="text" value={formData.matricNumber} onChange={(e) => setFormData({ ...formData, matricNumber: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500" required />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Full Name</label>
                            <input type="text" value={formData.fullName} onChange={(e) => setFormData({ ...formData, fullName: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500" required />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Email (Optional)</label>
                            <input type="email" value={formData.email} onChange={(e) => setFormData({ ...formData, email: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500" />
                        </div>
                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
                            <p className="text-sm text-blue-800">Password will be auto-generated as the matric number</p>
                        </div>
                        <div className="flex gap-3 pt-4">
                            <button type="button" onClick={onClose} className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50">Cancel</button>
                            <button type="submit" disabled={createMutation.isPending} className="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50">{createMutation.isPending ? 'Creating...' : 'Create'}</button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}

function ImportLecturersModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [file, setFile] = useState<File | null>(null);
    const [results, setResults] = useState<any>(null);

    const importMutation = useMutation({
        mutationFn: adminApi.importLecturers,
        onSuccess: (data) => { queryClient.invalidateQueries({ queryKey: ['lecturers'] }); setResults(data); toast.success(`Imported ${data.success} lecturers!`); },
        onError: (error: any) => toast.error(error.response?.data?.error || 'Failed to import'),
    });

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[80vh] overflow-y-auto">
                <h3 className="text-lg font-semibold mb-4">Import Lecturers from Excel</h3>
                {results ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">Successfully imported {results.success} lecturers</p>
                            {results.failed > 0 && <p className="text-sm text-red-800">Failed: {results.failed} rows</p>}
                        </div>
                        {results.lecturers?.length > 0 && (
                            <div>
                                <h4 className="font-semibold mb-2">Generated Credentials:</h4>
                                <div className="bg-gray-50 rounded-lg p-4 max-h-60 overflow-y-auto">
                                    {results.lecturers.map((lec: any, idx: number) => (
                                        <div key={idx} className="mb-3 pb-3 border-b border-gray-200 last:border-0">
                                            <p className="text-sm"><span className="font-semibold">{lec.fullName}</span> ({lec.username})</p>
                                            <p className="text-xs text-gray-600">Password: <span className="font-mono">{lec.generatedPassword}</span></p>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                        <button onClick={onClose} className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700">Close</button>
                    </div>
                ) : (
                    <form onSubmit={(e) => { e.preventDefault(); if (file) importMutation.mutate(file); }} className="space-y-4">
                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                            <p className="text-sm text-blue-800 font-semibold mb-2">Excel Format:</p>
                            <p className="text-xs text-blue-700">Column 1: Full Name | Column 2: Email</p>
                            <p className="text-xs text-blue-700 mt-1">Username will be generated from email (before @)</p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Upload Excel File</label>
                            <input type="file" accept=".xlsx,.xls" onChange={(e) => setFile(e.target.files?.[0] || null)} className="w-full px-3 py-2 border border-gray-300 rounded-lg" required />
                        </div>
                        <div className="flex gap-3 pt-4">
                            <button type="button" onClick={onClose} className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50">Cancel</button>
                            <button type="submit" disabled={importMutation.isPending || !file} className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50">{importMutation.isPending ? 'Importing...' : 'Import'}</button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}

function ImportStudentsModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [file, setFile] = useState<File | null>(null);
    const [results, setResults] = useState<any>(null);

    const importMutation = useMutation({
        mutationFn: adminApi.importStudents,
        onSuccess: (data) => { queryClient.invalidateQueries({ queryKey: ['students'] }); setResults(data); toast.success(`Imported ${data.success} students!`); },
        onError: (error: any) => toast.error(error.response?.data?.error || 'Failed to import'),
    });

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[80vh] overflow-y-auto">
                <h3 className="text-lg font-semibold mb-4">Import Students from Excel</h3>
                {results ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">Successfully imported {results.success} students</p>
                            {results.failed > 0 && <p className="text-sm text-red-800">Failed: {results.failed} rows</p>}
                        </div>
                        {results.students?.length > 0 && (
                            <div>
                                <h4 className="font-semibold mb-2">Generated Credentials:</h4>
                                <div className="bg-gray-50 rounded-lg p-4 max-h-60 overflow-y-auto">
                                    {results.students.map((student: any, idx: number) => (
                                        <div key={idx} className="mb-3 pb-3 border-b border-gray-200 last:border-0">
                                            <p className="text-sm"><span className="font-semibold">{student.fullName}</span> ({student.matricNumber})</p>
                                            <p className="text-xs text-gray-600">Password: <span className="font-mono">{student.generatedPassword}</span></p>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                        <button onClick={onClose} className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700">Close</button>
                    </div>
                ) : (
                    <form onSubmit={(e) => { e.preventDefault(); if (file) importMutation.mutate(file); }} className="space-y-4">
                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                            <p className="text-sm text-blue-800 font-semibold mb-2">Excel Format:</p>
                            <p className="text-xs text-blue-700">Column 1: Full Name | Column 2: Matric Number</p>
                            <p className="text-xs text-blue-700 mt-1">Password will be set as the matric number</p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Upload Excel File</label>
                            <input type="file" accept=".xlsx,.xls" onChange={(e) => setFile(e.target.files?.[0] || null)} className="w-full px-3 py-2 border border-gray-300 rounded-lg" required />
                        </div>
                        <div className="flex gap-3 pt-4">
                            <button type="button" onClick={onClose} className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50">Cancel</button>
                            <button type="submit" disabled={importMutation.isPending || !file} className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50">{importMutation.isPending ? 'Importing...' : 'Import'}</button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}
