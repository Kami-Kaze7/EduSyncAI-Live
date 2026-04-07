// @ts-nocheck
'use client';

import { useState, useEffect, useRef } from 'react';
import { adminApi } from '@/lib/adminApi';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

// Inline editable course row
function EditableCourseRow({ course, onSave, onDelete }: {
    course: { id: number; courseCode: string; courseTitle: string };
    onSave: (id: number, data: { courseCode: string; courseTitle: string }) => void;
    onDelete: (id: number) => void;
}) {
    const [editingField, setEditingField] = useState<'code' | 'title' | null>(null);
    const [code, setCode] = useState(course.courseCode || course.CourseCode || '');
    const [title, setTitle] = useState(course.courseTitle || course.CourseTitle || '');
    const inputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        setCode(course.courseCode || course.CourseCode || '');
        setTitle(course.courseTitle || course.CourseTitle || '');
    }, [course]);

    useEffect(() => {
        if (editingField && inputRef.current) {
            inputRef.current.focus();
            inputRef.current.select();
        }
    }, [editingField]);

    const commitEdit = () => {
        const origCode = course.courseCode || course.CourseCode || '';
        const origTitle = course.courseTitle || course.CourseTitle || '';
        if (code !== origCode || title !== origTitle) {
            onSave(course.id || course.Id, { courseCode: code, courseTitle: title });
        }
        setEditingField(null);
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') commitEdit();
        if (e.key === 'Escape') {
            setCode(course.courseCode || course.CourseCode || '');
            setTitle(course.courseTitle || course.CourseTitle || '');
            setEditingField(null);
        }
    };

    return (
        <div className="group flex items-center gap-2 bg-gray-50 hover:bg-blue-50/50 border border-gray-200 hover:border-blue-200 rounded-lg px-3 py-2 transition-all">
            {/* Course Code */}
            {editingField === 'code' ? (
                <input
                    ref={inputRef}
                    value={code}
                    onChange={(e) => setCode(e.target.value.toUpperCase())}
                    onBlur={commitEdit}
                    onKeyDown={handleKeyDown}
                    className="w-24 text-xs font-bold text-blue-700 bg-white border border-blue-300 rounded px-2 py-1 outline-none ring-2 ring-blue-200"
                />
            ) : (
                <button
                    onClick={() => setEditingField('code')}
                    className="text-xs font-bold text-blue-700 bg-blue-100 hover:bg-blue-200 px-2 py-1 rounded cursor-text transition-colors min-w-[60px] text-left"
                    title="Click to edit course code"
                >
                    {code}
                </button>
            )}

            {/* Course Title */}
            {editingField === 'title' ? (
                <input
                    ref={inputRef}
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    onBlur={commitEdit}
                    onKeyDown={handleKeyDown}
                    className="flex-1 text-xs font-medium text-gray-900 bg-white border border-blue-300 rounded px-2 py-1 outline-none ring-2 ring-blue-200"
                />
            ) : (
                <button
                    onClick={() => setEditingField('title')}
                    className="flex-1 text-xs font-medium text-gray-700 hover:text-gray-900 cursor-text text-left truncate"
                    title="Click to edit course title"
                >
                    {title}
                </button>
            )}

            {/* Edit hint icon */}
            <svg className="w-3.5 h-3.5 text-gray-300 group-hover:text-blue-400 flex-shrink-0 transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
            </svg>

            {/* Delete button */}
            <button
                onClick={() => {
                    if (confirm(`Delete ${code}?`)) onDelete(course.id || course.Id);
                }}
                className="text-red-300 hover:text-red-600 opacity-0 group-hover:opacity-100 transition-all p-0.5"
                title="Remove course"
            >
                <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
            </button>
        </div>
    );
}

export default function FacultiesTab() {
    const queryClient = useQueryClient();
    
    // State for selected items
    const [selectedFaculty, setSelectedFaculty] = useState<number | null>(null);
    const [selectedDepartment, setSelectedDepartment] = useState<number | null>(null);
    const [selectedYear, setSelectedYear] = useState<number | null>(null);

    // Queries
    const { data: faculties, isLoading: loadingFac} = useQuery({
        queryKey: ['faculties'],
        queryFn: adminApi.getFaculties
    });

    const { data: departments } = useQuery({
        queryKey: ['departments', selectedFaculty],
        queryFn: () => adminApi.getDepartments(selectedFaculty!),
        enabled: !!selectedFaculty
    });

    const { data: years } = useQuery({
        queryKey: ['years', selectedDepartment],
        queryFn: () => adminApi.getYears(selectedDepartment!),
        enabled: !!selectedDepartment
    });

    // Mutations
    const facMutation = useMutation({
        mutationFn: adminApi.createFaculty,
        onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['faculties'] }); toast.success('Faculty added'); },
        onError: (err: any) => { toast.error('Failed to add: ' + err.message); }
    });

    const deptMutation = useMutation({
        mutationFn: (name: string) => adminApi.createDepartment(selectedFaculty!, name),
        onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['departments'] }); toast.success('Department added'); },
        onError: (err: any) => { toast.error('Failed to add: ' + err.message); }
    });

    const yearMutation = useMutation({
        mutationFn: (data: {name: string; level: number}) => adminApi.createYear(selectedDepartment!, data.name, data.level),
        onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['years'] }); toast.success('Year of Study added'); },
        onError: (err: any) => { toast.error('Failed to add: ' + err.message); }
    });

    const updateCourseMutation = useMutation({
        mutationFn: ({ courseId, data }: { courseId: number; data: { courseCode: string; courseTitle: string } }) =>
            adminApi.updateCourse(courseId, data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['years'] });
            queryClient.invalidateQueries({ queryKey: ['admin-courses'] });
            toast.success('Course updated');
        },
        onError: (err: any) => toast.error('Update failed: ' + err.message),
    });

    const addCourseMutation = useMutation({
        mutationFn: (yearId: number) => adminApi.addCourseToYear(yearId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['years'] });
            queryClient.invalidateQueries({ queryKey: ['admin-courses'] });
            toast.success('Course added');
        },
        onError: (err: any) => toast.error('Failed to add course: ' + err.message),
    });

    const deleteCourseMutation = useMutation({
        mutationFn: (courseId: number) => adminApi.deleteCourseFromYear(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['years'] });
            queryClient.invalidateQueries({ queryKey: ['admin-courses'] });
            toast.success('Course removed');
        },
        onError: (err: any) => toast.error('Failed to remove: ' + err.message),
    });

    // Form handlers
    const onAddFaculty = () => {
        const name = prompt('Enter Faculty Name:');
        if (name) facMutation.mutate(name);
    };

    const onAddDept = () => {
        const name = prompt('Enter Department Name:');
        if (name) deptMutation.mutate(name);
    };

    const onAddYear = () => {
        const name = prompt("Enter Year Name (e.g. 'Year 1' or 'Year 2'):");
        if (!name) return;
        
        let level = parseInt(name.replace(/[^0-9]/g, ''), 10);
        if (isNaN(level)) {
            level = years ? years.length + 1 : 1; 
        }
        
        yearMutation.mutate({ name, level });
    };

    const handleSaveCourse = (courseId: number, data: { courseCode: string; courseTitle: string }) => {
        updateCourseMutation.mutate({ courseId, data });
    };

    const handleDeleteCourse = (courseId: number) => {
        deleteCourseMutation.mutate(courseId);
    };

    const handleAddCourse = (yearId: number) => {
        addCourseMutation.mutate(yearId);
    };

    if (loadingFac) return <div className="p-10 text-center text-gray-500 animate-pulse">Loading Academic Structure...</div>;

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">Academic Hierarchy Settings</h2>
                    <p className="text-sm text-gray-400 mt-0.5">Scaffold the university structure from Faculties down to Courses.</p>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {/* Faculties Column */}
                <div className="bg-white rounded-lg border border-gray-200 p-5 shadow-sm">
                    <div className="flex justify-between items-center mb-4">
                        <h3 className="font-bold text-gray-800">1. Faculties</h3>
                        <button onClick={onAddFaculty} className="text-xs bg-blue-100 text-blue-700 px-2.5 py-1.5 rounded-lg hover:bg-blue-200 font-bold transition-colors">
                            + Add
                        </button>
                    </div>
                    <div className="space-y-2 max-h-[500px] overflow-y-auto pr-1">
                        {faculties?.length === 0 ? <p className="text-sm text-gray-400 italic text-center py-4">No faculties yet</p> : null}
                        {faculties?.map((f: any) => (
                            <button
                                key={f.id}
                                onClick={() => { setSelectedFaculty(f.id); setSelectedDepartment(null); setSelectedYear(null); }}
                                className={`w-full text-left px-4 py-3 rounded-lg border transition-all ${selectedFaculty === f.id ? 'border-blue-300 bg-blue-50 ring-1 ring-blue-500/20' : 'border-gray-200 bg-gray-50 hover:bg-gray-100'}`}
                            >
                                <span className={`text-sm font-semibold ${selectedFaculty === f.id ? 'text-blue-600' : 'text-gray-700'}`}>{f.name}</span>
                            </button>
                        ))}
                    </div>
                </div>

                {/* Departments Column */}
                <div className={`bg-white rounded-lg border p-5 shadow-sm transition-opacity ${!selectedFaculty ? 'opacity-50 pointer-events-none' : 'border-gray-200'}`}>
                    <div className="flex justify-between items-center mb-4">
                        <h3 className="font-bold text-gray-800">2. Departments</h3>
                        <button onClick={onAddDept} className="text-xs bg-blue-100 text-blue-700 px-2.5 py-1.5 rounded-lg hover:bg-blue-200 font-bold transition-colors">
                            + Add
                        </button>
                    </div>
                    <div className="space-y-2 max-h-[500px] overflow-y-auto pr-1">
                        {!selectedFaculty ? (
                             <p className="text-sm text-gray-400 italic text-center py-4">Select a faculty first</p>
                        ) : departments?.length === 0 ? (
                            <p className="text-sm text-gray-400 italic text-center py-4">No departments yet</p>
                        ) : null}
                        
                        {departments?.map((d: any) => (
                            <button
                                key={d.id}
                                onClick={() => { setSelectedDepartment(d.id); setSelectedYear(null); }}
                                className={`w-full text-left px-4 py-3 rounded-lg border transition-all ${selectedDepartment === d.id ? 'border-blue-300 bg-blue-50 ring-1 ring-blue-500/20' : 'border-gray-200 bg-gray-50 hover:bg-gray-100'}`}
                            >
                                <span className={`text-sm font-semibold ${selectedDepartment === d.id ? 'text-blue-600' : 'text-gray-700'}`}>{d.name}</span>
                            </button>
                        ))}
                    </div>
                </div>

                {/* Years & Courses Column */}
                <div className={`bg-white rounded-lg border p-5 shadow-sm transition-opacity ${!selectedDepartment ? 'opacity-50 pointer-events-none' : 'border-gray-200'}`}>
                    <div className="flex justify-between items-center mb-4">
                        <h3 className="font-bold text-gray-800">3. Years & Courses</h3>
                        <button onClick={onAddYear} className="text-xs bg-blue-100 text-blue-700 px-2.5 py-1.5 rounded-lg hover:bg-blue-200 font-bold transition-colors">
                            + Add Year
                        </button>
                    </div>
                    <div className="space-y-4 max-h-[500px] overflow-y-auto pr-1">
                        {!selectedDepartment ? (
                             <p className="text-sm text-gray-400 italic text-center py-4">Select a department first</p>
                        ) : years?.length === 0 ? (
                            <p className="text-sm text-gray-400 italic text-center py-4">No years of study yet</p>
                        ) : null}
                        
                        {years?.map((y: any) => {
                            const yId = y.id || y.Id;
                            const isOpen = selectedYear === yId;
                            const courseList = y.courses || y.Courses || [];

                            return (
                                <div key={yId} className="border border-gray-200 rounded-lg overflow-hidden">
                                    <div 
                                        className={`px-4 py-2.5 border-b border-gray-200 flex justify-between items-center cursor-pointer transition-colors ${isOpen ? 'bg-blue-50' : 'bg-gray-50 hover:bg-blue-50/50'}`}
                                        onClick={() => setSelectedYear(isOpen ? null : yId)}
                                    >
                                        <div className="flex items-center gap-2">
                                            <svg className={`w-3.5 h-3.5 text-gray-400 transition-transform ${isOpen ? 'rotate-90' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                            </svg>
                                            <span className="text-sm font-bold text-gray-800">{y.name || y.Name}</span>
                                        </div>
                                        <span className="text-[10px] font-bold text-gray-400 bg-gray-200 px-2 py-0.5 rounded-full">
                                            {courseList.length} courses
                                        </span>
                                    </div>

                                    {isOpen && (
                                        <div className="p-3 bg-white space-y-2">
                                            {courseList.length === 0 && (
                                                <p className="text-xs text-gray-400 italic text-center py-3">No courses yet. Click + to add.</p>
                                            )}

                                            {courseList.map((c: any) => (
                                                <EditableCourseRow
                                                    key={c.id || c.Id}
                                                    course={c}
                                                    onSave={handleSaveCourse}
                                                    onDelete={handleDeleteCourse}
                                                />
                                            ))}

                                            {/* Add course button */}
                                            <button
                                                onClick={() => handleAddCourse(yId)}
                                                disabled={addCourseMutation.isPending}
                                                className="w-full flex items-center justify-center gap-1.5 py-2 border-2 border-dashed border-gray-200 hover:border-blue-300 rounded-lg text-xs font-bold text-gray-400 hover:text-blue-600 transition-all hover:bg-blue-50/30 disabled:opacity-50"
                                            >
                                                <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                                </svg>
                                                {addCourseMutation.isPending ? 'Adding...' : 'Add Course'}
                                            </button>
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            </div>
        </div>
    );
}
