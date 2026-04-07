// @ts-nocheck
'use client';

import { useState } from 'react';
import { adminApi } from '@/lib/adminApi';
import { courseApi } from '@/lib/api';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

export default function CoursesTab() {
    const queryClient = useQueryClient();
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [assigningCourseId, setAssigningCourseId] = useState<number | null>(null);
    const [collapsedFaculties, setCollapsedFaculties] = useState<Record<string, boolean>>({});
    const [collapsedDepts, setCollapsedDepts] = useState<Record<string, boolean>>({});

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

    const assignMutation = useMutation({
        mutationFn: ({ courseId, lecturerId }: { courseId: number; lecturerId: number }) =>
            adminApi.assignLecturerToCourse(courseId, lecturerId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['admin-courses'] });
            setAssigningCourseId(null);
            toast.success('Lecturer assigned successfully');
        },
        onError: () => toast.error('Failed to assign lecturer'),
    });

    const handleDelete = (id: number) => {
        if (confirm('Are you sure you want to delete this course?')) {
            deleteMutation.mutate(id);
        }
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

    // Group courses by Faculty → Department → Year
    const grouped: Record<string, Record<string, Record<string, any[]>>> = {};
    let totalCourses = 0;
    let unassignedCount = 0;

    (courses || []).forEach((course: any) => {
        totalCourses++;
        const faculty = course.facultyName || 'Unassigned';
        const dept = course.departmentName || 'Unassigned';
        const year = course.yearName || `Year ${course.yearLevel || '?'}`;

        if (!grouped[faculty]) grouped[faculty] = {};
        if (!grouped[faculty][dept]) grouped[faculty][dept] = {};
        if (!grouped[faculty][dept][year]) grouped[faculty][dept][year] = [];
        grouped[faculty][dept][year].push(course);

        if (!course.lecturerName || course.lecturerName === 'Unassigned') unassignedCount++;
    });

    const toggleFaculty = (f: string) => setCollapsedFaculties(prev => ({ ...prev, [f]: !prev[f] }));
    const toggleDept = (d: string) => setCollapsedDepts(prev => ({ ...prev, [d]: !prev[d] }));

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">Course Management</h2>
                    <p className="text-sm text-gray-400 mt-0.5">
                        {totalCourses} courses{unassignedCount > 0 && <span className="text-amber-500 font-medium"> · {unassignedCount} unassigned</span>}
                    </p>
                </div>
                <button
                    onClick={() => setShowCreateModal(true)}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium"
                >
                    + Create Course
                </button>
            </div>

            {isLoading ? (
                <div className="text-center py-12"><div className="inline-block animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600" /><p className="mt-3 text-gray-500">Loading courses...</p></div>
            ) : totalCourses === 0 ? (
                <div className="text-center py-16 bg-white rounded-2xl border border-gray-100">
                    <div className="text-5xl mb-4">🎓</div>
                    <h3 className="text-xl font-bold text-gray-900 mb-2">No courses yet</h3>
                    <p className="text-gray-500 mb-6">Create your first course or add courses through Academic Structure</p>
                    <button onClick={() => setShowCreateModal(true)} className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium">
                        + Create Course
                    </button>
                </div>
            ) : (
                <div className="space-y-4">
                    {Object.entries(grouped).map(([faculty, departments]) => {
                        const facCourseCount = Object.values(departments).flatMap(d => Object.values(d).flat()).length;
                        const isFacCollapsed = collapsedFaculties[faculty];

                        return (
                            <div key={faculty} className="bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-sm">
                                {/* Faculty Header */}
                                <div
                                    className="px-6 py-4 bg-gradient-to-r from-indigo-50 to-blue-50 border-b border-gray-100 cursor-pointer hover:from-indigo-100 hover:to-blue-100 transition-all"
                                    onClick={() => toggleFaculty(faculty)}
                                >
                                    <div className="flex items-center justify-between">
                                        <h3 className="text-sm font-bold text-gray-900 flex items-center gap-2">
                                            <svg className={`w-4 h-4 text-gray-400 transition-transform ${isFacCollapsed ? '' : 'rotate-90'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                            </svg>
                                            <span className="w-7 h-7 rounded-lg bg-blue-600 text-white flex items-center justify-center text-xs">🏛️</span>
                                            {faculty}
                                        </h3>
                                        <span className="text-xs text-gray-500 font-medium bg-white px-2.5 py-1 rounded-full border border-gray-200">
                                            {facCourseCount} courses
                                        </span>
                                    </div>
                                </div>

                                {!isFacCollapsed && (
                                    <div className="divide-y divide-gray-50">
                                        {Object.entries(departments).map(([dept, years]) => {
                                            const deptKey = `${faculty}::${dept}`;
                                            const isDeptCollapsed = collapsedDepts[deptKey];
                                            const deptCourseCount = Object.values(years).flat().length;

                                            return (
                                                <div key={dept}>
                                                    {/* Department Header */}
                                                    <div
                                                        className="px-6 py-3 bg-gray-50/50 cursor-pointer hover:bg-gray-100/50 transition-colors border-b border-gray-50"
                                                        onClick={() => toggleDept(deptKey)}
                                                    >
                                                        <div className="flex items-center gap-2">
                                                            <svg className={`w-3.5 h-3.5 text-gray-400 transition-transform ${isDeptCollapsed ? '' : 'rotate-90'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                                            </svg>
                                                            <span className="text-xs font-bold text-blue-600 bg-blue-50 px-2.5 py-1 rounded-full">{dept}</span>
                                                            <span className="text-[10px] text-gray-400">{deptCourseCount} courses</span>
                                                        </div>
                                                    </div>

                                                    {!isDeptCollapsed && (
                                                        <div className="px-6 py-3 space-y-4">
                                                            {Object.entries(years).map(([year, yearCourses]) => (
                                                                <div key={year}>
                                                                    <div className="flex items-center gap-2 mb-2">
                                                                        <span className="text-[10px] font-bold text-gray-500 bg-gray-100 px-2 py-0.5 rounded-full uppercase tracking-wider">{year}</span>
                                                                        <span className="text-[10px] text-gray-400">{yearCourses.length} courses</span>
                                                                    </div>

                                                                    <div className="space-y-1.5">
                                                                        {yearCourses.map((course: any) => (
                                                                            <div key={course.id} className="group flex items-center gap-3 py-2.5 px-3 rounded-lg hover:bg-gray-50 transition-all border border-transparent hover:border-gray-100">
                                                                                {/* Course Code */}
                                                                                <span className="text-xs font-bold text-blue-700 bg-blue-50 px-2 py-1 rounded-md min-w-[70px] text-center">
                                                                                    {course.courseCode}
                                                                                </span>

                                                                                {/* Course Name */}
                                                                                <span className="text-sm font-medium text-gray-900 flex-1 truncate">
                                                                                    {course.courseName}
                                                                                </span>

                                                                                {/* Lecturer Assignment */}
                                                                                <div className="relative flex-shrink-0">
                                                                                    {assigningCourseId === course.id ? (
                                                                                        <select
                                                                                            autoFocus
                                                                                            className="text-xs border border-blue-300 rounded-lg px-2 py-1.5 bg-white ring-2 ring-blue-200 outline-none min-w-[160px]"
                                                                                            defaultValue={course.lecturerId || 0}
                                                                                            onChange={(e) => {
                                                                                                const lecId = parseInt(e.target.value);
                                                                                                if (lecId > 0) {
                                                                                                    assignMutation.mutate({ courseId: course.id, lecturerId: lecId });
                                                                                                }
                                                                                            }}
                                                                                            onBlur={() => setAssigningCourseId(null)}
                                                                                        >
                                                                                            <option value={0} disabled>-- Select Lecturer --</option>
                                                                                            {(lecturers || []).map((l: any) => (
                                                                                                <option key={l.id} value={l.id}>{l.fullName}</option>
                                                                                            ))}
                                                                                        </select>
                                                                                    ) : (
                                                                                        <button
                                                                                            onClick={() => setAssigningCourseId(course.id)}
                                                                                            className={`text-xs font-medium px-2.5 py-1 rounded-lg flex items-center gap-1.5 transition-all ${
                                                                                                course.lecturerName && course.lecturerName !== 'Unassigned'
                                                                                                    ? 'text-gray-700 bg-gray-100 hover:bg-gray-200'
                                                                                                    : 'text-amber-600 bg-amber-50 hover:bg-amber-100 border border-amber-200'
                                                                                            }`}
                                                                                            title="Click to assign/change lecturer"
                                                                                        >
                                                                                            <span className={`w-5 h-5 rounded-full flex items-center justify-center text-[9px] font-bold ${
                                                                                                course.lecturerName && course.lecturerName !== 'Unassigned'
                                                                                                    ? 'bg-indigo-500 text-white'
                                                                                                    : 'bg-amber-200 text-amber-700'
                                                                                            }`}>
                                                                                                {(course.lecturerName || 'U').charAt(0).toUpperCase()}
                                                                                            </span>
                                                                                            {course.lecturerName || 'Unassigned'}
                                                                                            <svg className="w-3 h-3 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                                                                                            </svg>
                                                                                        </button>
                                                                                    )}
                                                                                </div>

                                                                                {/* Syllabus */}
                                                                                <div className="flex-shrink-0">
                                                                                    {course.syllabusPath ? (
                                                                                        <button
                                                                                            onClick={() => handleDownloadSyllabus(course.id, course.courseCode)}
                                                                                            className="text-[10px] font-medium px-2 py-1 bg-green-50 text-green-600 hover:bg-green-100 rounded-md border border-green-200 transition-colors"
                                                                                        >
                                                                                            📄 PDF
                                                                                        </button>
                                                                                    ) : (
                                                                                        <span className="text-[10px] text-gray-300 italic">No syllabus</span>
                                                                                    )}
                                                                                </div>

                                                                                {/* Delete */}
                                                                                <button
                                                                                    onClick={() => handleDelete(course.id)}
                                                                                    className="text-red-300 hover:text-red-600 opacity-0 group-hover:opacity-100 transition-all p-1"
                                                                                    title="Delete course"
                                                                                >
                                                                                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                                                                    </svg>
                                                                                </button>
                                                                            </div>
                                                                        ))}
                                                                    </div>
                                                                </div>
                                                            ))}
                                                        </div>
                                                    )}
                                                </div>
                                            );
                                        })}
                                    </div>
                                )}
                            </div>
                        );
                    })}
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
                                onChange={(e) => setFormData({ ...formData, courseCode: e.target.value.toUpperCase() })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-300 focus:border-transparent"
                                placeholder="e.g. CSC301"
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
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-300 focus:border-transparent"
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
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-300 focus:border-transparent"
                            placeholder="e.g. Data Structures"
                            required
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                        <textarea
                            value={formData.description}
                            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                            rows={2}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-300 focus:border-transparent"
                            placeholder="Brief course description..."
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Assign to Lecturer *</label>
                        <select
                            value={formData.lecturerId}
                            onChange={(e) => setFormData({ ...formData, lecturerId: parseInt(e.target.value) })}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-300 focus:border-transparent"
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
                            <p className="text-xs text-blue-600 mt-1">No lecturers available. Add lecturers first.</p>
                        )}
                    </div>

                    <div className="flex gap-3 pt-3">
                        <button type="button" onClick={onClose} className="flex-1 px-4 py-2.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors">
                            Cancel
                        </button>
                        <button type="submit" disabled={createMutation.isPending} className="flex-1 px-4 py-2.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors font-medium">
                            {createMutation.isPending ? 'Creating...' : 'Create & Assign'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
