'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { courseApi } from '@/lib/api';
import { useParams, useRouter } from 'next/navigation';
import { useState, useRef } from 'react';
import Link from 'next/link';
import { ArrowLeftIcon, DocumentArrowUpIcon, DocumentArrowDownIcon, SparklesIcon, UsersIcon, UserPlusIcon, PaperAirplaneIcon, EyeIcon, XMarkIcon, TrashIcon } from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';

export default function CourseDetailsPage() {
    const params = useParams();
    const router = useRouter();
    const courseId = parseInt(params.id as string);
    const queryClient = useQueryClient();

    const [showStudentImport, setShowStudentImport] = useState(false);
    const [showManualAdd, setShowManualAdd] = useState(false);
    const [showSummarizeModal, setShowSummarizeModal] = useState(false);
    const [selectedWeek, setSelectedWeek] = useState(1);
    const [newStudent, setNewStudent] = useState({ fullName: '', matricNumber: '', email: '' });
    const [selectedSummary, setSelectedSummary] = useState<any>(null);
    const [showSummaryView, setShowSummaryView] = useState(false);
    const [showSendModal, setShowSendModal] = useState(false);
    const [summaryToSend, setSummaryToSend] = useState<any>(null);
    const [selectedStudentIds, setSelectedStudentIds] = useState<number[]>([]);

    const syllabusInputRef = useRef<HTMLInputElement>(null);
    const studentFileInputRef = useRef<HTMLInputElement>(null);

    const { data: course, isLoading } = useQuery({
        queryKey: ['course', courseId],
        queryFn: () => courseApi.getById(courseId),
    });

    const { data: enrollments } = useQuery({
        queryKey: ['enrollments', courseId],
        queryFn: () => courseApi.getEnrollments(courseId),
    });

    const { data: syllabusInfo } = useQuery({
        queryKey: ['syllabusInfo', courseId],
        queryFn: () => courseApi.getSyllabusInfo(courseId),
        enabled: !!courseId,
        retry: false,
    });

    const { data: summaries, refetch: refetchSummaries } = useQuery({
        queryKey: ['summaries', courseId],
        queryFn: () => courseApi.getCourseSummaries(courseId),
        enabled: !!courseId,
    });

    const analyzeSyllabusMutation = useMutation({
        mutationFn: (file: File) => courseApi.analyzeSyllabus(courseId, file),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['syllabusInfo', courseId] });
            toast.success(`Syllabus analyzed! ${data.totalWeeks} weeks detected.`);
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to analyze syllabus');
        },
    });

    const summarizeWeekMutation = useMutation({
        mutationFn: (weekNumber: number) => courseApi.summarizeWeek(courseId, weekNumber),
        onSuccess: () => {
            refetchSummaries();
            toast.success('Summary generated successfully!');
            setShowSummarizeModal(false);
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to generate summary');
        },
    });

    const sendSummaryMutation = useMutation({
        mutationFn: ({ summaryId, studentIds }: { summaryId: number, studentIds: number[] }) =>
            courseApi.sendSummaryToStudents(courseId, summaryId, studentIds),
        onSuccess: () => {
            refetchSummaries();
            toast.success('Summary sent to selected students!');
            setShowSendModal(false);
            setSelectedStudentIds([]);
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to send summary');
        },
    });

    const deleteSummaryMutation = useMutation({
        mutationFn: (summaryId: number) => courseApi.deleteSummary(courseId, summaryId),
        onSuccess: () => {
            refetchSummaries();
            toast.success('Summary deleted successfully!');
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to delete summary');
        },
    });

    const deleteSyllabusMutation = useMutation({
        mutationFn: () => courseApi.deleteSyllabus(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['syllabusInfo', courseId] });
            refetchSummaries();
            toast.success('Syllabus and all summaries deleted!');
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to delete syllabus');
        },
    });

    const importStudentsMutation = useMutation({
        mutationFn: (file: File) => courseApi.importStudents(courseId, file),
        onSuccess: (data: any) => {
            queryClient.invalidateQueries({ queryKey: ['enrollments', courseId] });
            toast.success(`Imported: ${data.created} created, ${data.enrolled} enrolled, ${data.skipped} skipped`);
            setShowStudentImport(false);
            if (data.errors && data.errors.length > 0) {
                console.error('Import errors:', data.errors);
            }
        },
        onError: () => {
            toast.error('Failed to import students');
        },
    });

    const addStudentMutation = useMutation({
        mutationFn: (student: { fullName: string, matricNumber: string, email: string }) =>
            courseApi.addStudent(courseId, student),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['enrollments', courseId] });
            toast.success('Student added successfully!');
            setShowManualAdd(false);
            setNewStudent({ fullName: '', matricNumber: '', email: '' });
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to add student');
        },
    });

    const handleSyllabusUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) {
            analyzeSyllabusMutation.mutate(file);
        }
    };

    const handleStudentFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) {
            importStudentsMutation.mutate(file);
        }
    };

    const handleDownloadSyllabus = async () => {
        try {
            const blob = await courseApi.downloadSyllabus(courseId);
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${course?.courseCode}_syllabus.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        } catch (error) {
            toast.error('Failed to download syllabus');
        }
    };

    const handleManualAdd = (e: React.FormEvent) => {
        e.preventDefault();
        addStudentMutation.mutate(newStudent);
    };

    const handleSummarize = () => {
        summarizeWeekMutation.mutate(selectedWeek);
    };

    const downloadTemplate = () => {
        const csvContent = "Full Name,Matric Number,Email\\nJohn Doe,MAT001,john@example.com\\nJane Smith,MAT002,jane@example.com";
        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'student_import_template.csv';
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
    };

    if (isLoading) {
        return (
            <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center">
                <div className="text-center">
                    <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
                    <p className="mt-4 text-gray-600">Loading course...</p>
                </div>
            </div>
        );
    }

    if (!course) {
        return (
            <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center">
                <div className="text-center">
                    <h2 className="text-2xl font-bold text-gray-900 mb-4">Course not found</h2>
                    <Link href="/dashboard" className="text-indigo-600 hover:text-indigo-800">
                        ← Back to dashboard
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
                    <div className="flex items-center space-x-4">
                        <Link href="/dashboard" className="text-gray-600 hover:text-gray-900">
                            <ArrowLeftIcon className="h-6 w-6" />
                        </Link>
                        <div>
                            <h1 className="text-2xl font-bold text-gray-900">{course.courseCode}</h1>
                            <p className="text-gray-600">{course.courseName}</p>
                        </div>
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                    {/* AI Syllabus Section */}
                    <div className="bg-white rounded-xl shadow-lg p-6">
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="text-xl font-bold text-gray-900">AI Syllabus Analysis</h2>
                            <SparklesIcon className="h-6 w-6 text-purple-600" />
                        </div>

                        <div className="space-y-4">
                            <input
                                ref={syllabusInputRef}
                                type="file"
                                accept=".pdf,.docx,.txt"
                                onChange={handleSyllabusUpload}
                                className="hidden"
                            />

                            <button
                                onClick={() => syllabusInputRef.current?.click()}
                                disabled={analyzeSyllabusMutation.isPending}
                                className="w-full flex items-center justify-center space-x-2 bg-purple-600 text-white px-4 py-3 rounded-lg hover:bg-purple-700 transition-colors disabled:opacity-50"
                            >
                                <DocumentArrowUpIcon className="h-5 w-5" />
                                <span>{analyzeSyllabusMutation.isPending ? 'Analyzing...' : 'Upload & Analyze Syllabus'}</span>
                            </button>

                            {syllabusInfo && (
                                <div className="p-4 bg-purple-50 rounded-lg border border-purple-200">
                                    <p className="text-sm font-medium text-gray-900">📄 {syllabusInfo.fileName}</p>
                                    <p className="text-sm text-gray-600 mt-1">
                                        📊 {syllabusInfo.totalWeeks} weeks detected
                                    </p>
                                    <p className="text-xs text-gray-500 mt-1">
                                        Uploaded: {new Date(syllabusInfo.uploadedAt).toLocaleDateString()}
                                    </p>

                                    <div className="flex space-x-2 mt-3">
                                        <button
                                            onClick={async () => {
                                                try {
                                                    const blob = await courseApi.downloadSyllabus(courseId);
                                                    const url = window.URL.createObjectURL(blob);
                                                    const a = document.createElement('a');
                                                    a.href = url;
                                                    a.download = syllabusInfo.fileName;
                                                    document.body.appendChild(a);
                                                    a.click();
                                                    window.URL.revokeObjectURL(url);
                                                    document.body.removeChild(a);
                                                    toast.success('Syllabus downloaded!');
                                                } catch (error) {
                                                    toast.error('Failed to download syllabus');
                                                }
                                            }}
                                            className="flex-1 flex items-center justify-center space-x-2 bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 transition-colors text-sm"
                                        >
                                            <DocumentArrowDownIcon className="h-4 w-4" />
                                            <span>Download</span>
                                        </button>


                                        <button
                                            onClick={() => {
                                                if (window.confirm('Are you sure you want to delete this syllabus? This will also delete ALL weekly summaries associated with it. This action cannot be undone.')) {
                                                    deleteSyllabusMutation.mutate();
                                                }
                                            }}
                                            disabled={deleteSyllabusMutation.isPending}
                                            className="flex items-center justify-center p-2 bg-red-100 text-red-600 rounded-lg hover:bg-red-200 transition-colors disabled:opacity-50"
                                            title="Delete Syllabus"
                                        >
                                            <TrashIcon className="h-5 w-5" />
                                        </button>
                                    </div>
                                </div>
                            )}

                            <p className="text-sm text-gray-500">
                                Accepted formats: .pdf, .docx, .txt (Max 10MB)
                            </p>
                        </div>
                    </div>

                    {/* Student Management Section */}
                    <div className="bg-white rounded-xl shadow-lg p-6">
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="text-xl font-bold text-gray-900">Student Enrollment</h2>
                            <UsersIcon className="h-6 w-6 text-indigo-600" />
                        </div>

                        <div className="space-y-3">
                            <button
                                onClick={() => setShowStudentImport(true)}
                                className="w-full flex items-center justify-center space-x-2 bg-indigo-600 text-white px-4 py-3 rounded-lg hover:bg-indigo-700 transition-colors"
                            >
                                <DocumentArrowUpIcon className="h-5 w-5" />
                                <span>Import from Excel</span>
                            </button>

                            <button
                                onClick={() => setShowManualAdd(true)}
                                className="w-full flex items-center justify-center space-x-2 bg-green-600 text-white px-4 py-3 rounded-lg hover:bg-green-700 transition-colors"
                            >
                                <UserPlusIcon className="h-5 w-5" />
                                <span>Add Student Manually</span>
                            </button>

                            <div className="pt-4 border-t border-gray-200">
                                <p className="text-sm font-medium text-gray-700 mb-2">
                                    Enrolled Students: {enrollments?.length || 0}
                                </p>
                                {enrollments && enrollments.length > 0 && (
                                    <div className="max-h-48 overflow-y-auto space-y-2">
                                        {enrollments.map((enrollment: any) => (
                                            <div key={enrollment.id} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                                                <div>
                                                    <p className="text-sm font-medium text-gray-900">{enrollment.student?.fullName}</p>
                                                    <p className="text-xs text-gray-500">{enrollment.student?.matricNumber}</p>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Enrolled Students Section - full width below the two panels */}
                <div className="mt-6 bg-white rounded-xl shadow-lg p-6">
                    <div className="flex items-center justify-between mb-4">
                        <div>
                            <h2 className="text-xl font-bold text-gray-900">Enrolled Students</h2>
                            <p className="text-sm text-gray-500 mt-0.5">{enrollments?.length || 0} student{(enrollments?.length || 0) !== 1 ? 's' : ''} enrolled in this course</p>
                        </div>
                        <UsersIcon className="h-6 w-6 text-indigo-500" />
                    </div>

                    {enrollments && enrollments.length > 0 ? (
                        <div className="overflow-x-auto">
                            <table className="min-w-full divide-y divide-gray-200">
                                <thead className="bg-gray-50">
                                    <tr>
                                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">#</th>
                                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Full Name</th>
                                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Matric Number</th>
                                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Enrolled On</th>
                                    </tr>
                                </thead>
                                <tbody className="bg-white divide-y divide-gray-100">
                                    {enrollments.map((enrollment: any, idx: number) => (
                                        <tr key={enrollment.id} className="hover:bg-indigo-50 transition-colors">
                                            <td className="px-4 py-3 text-sm text-gray-400">{idx + 1}</td>
                                            <td className="px-4 py-3 whitespace-nowrap">
                                                <div className="flex items-center gap-3">
                                                    <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
                                                        {enrollment.student?.fullName?.charAt(0)?.toUpperCase() || '?'}
                                                    </div>
                                                    <span className="text-sm font-semibold text-gray-900">{enrollment.student?.fullName || '—'}</span>
                                                </div>
                                            </td>
                                            <td className="px-4 py-3 whitespace-nowrap text-sm font-mono text-gray-700">{enrollment.student?.matricNumber || '—'}</td>
                                            <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{enrollment.student?.email || '—'}</td>
                                            <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                                                {enrollment.enrolledAt ? new Date(enrollment.enrolledAt).toLocaleDateString() : '—'}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    ) : (
                        <div className="text-center py-12 bg-gray-50 rounded-xl border-2 border-dashed border-gray-200">
                            <UsersIcon className="h-12 w-12 text-gray-300 mx-auto mb-3" />
                            <p className="text-gray-500 font-medium">No students enrolled yet</p>
                            <p className="text-xs text-gray-400 mt-1">Students enrolled by the admin will appear here.</p>
                        </div>
                    )}
                </div>

                {summaries && summaries.length > 0 && (
                    <div className="mt-6 bg-white rounded-xl shadow-lg p-6">
                        <div className="flex items-center justify-between mb-6">
                            <h2 className="text-xl font-bold text-gray-900">AI Weekly Teaching Summaries</h2>
                            <div className="flex items-center space-x-2 bg-green-50 text-green-700 px-3 py-1 rounded-full border border-green-100">
                                <span className="relative flex h-2 w-2">
                                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                                    <span className="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
                                </span>
                                <span className="text-xs font-semibold">Active & Shared with Students</span>
                            </div>
                        </div>

                        <div className="overflow-x-auto">
                            <table className="min-w-full divide-y divide-gray-200">
                                <thead className="bg-gray-50">
                                    <tr>
                                        <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Week</th>
                                        <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Topic / Teaching Focus</th>
                                        <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Generated</th>
                                        <th scope="col" className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                                    </tr>
                                </thead>
                                <tbody className="bg-white divide-y divide-gray-200">
                                    {summaries.map((summary: any) => (
                                        <tr key={summary.id} className="hover:bg-gray-50 transition-colors">
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                <div className="text-sm font-bold text-indigo-600">Week {summary.weekNumber}</div>
                                            </td>
                                            <td className="px-6 py-4">
                                                <div className="text-sm font-medium text-gray-900">{summary.weekTitle}</div>
                                                <div className="text-xs text-gray-500 truncate max-w-xs">{summary.summary.substring(0, 100)}...</div>
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                <div className="text-xs text-gray-500">{new Date(summary.generatedAt).toLocaleDateString()}</div>
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                                <div className="flex justify-end space-x-2">
                                                    <button
                                                        onClick={() => {
                                                            setSelectedSummary(summary);
                                                            setShowSummaryView(true);
                                                        }}
                                                        className="text-indigo-600 hover:text-indigo-900 bg-indigo-50 p-2 rounded-lg"
                                                        title="View Teaching Summary"
                                                    >
                                                        <EyeIcon className="h-5 w-5" />
                                                    </button>
                                                    <button
                                                        onClick={() => {
                                                            const content = `
${summary.weekTitle}
Week ${summary.weekNumber}

SUMMARY
${summary.summary}

KEY TOPICS
${JSON.parse(summary.keyTopics || '[]').map((topic: string, i: number) => `${i + 1}. ${topic}`).join('\n')}

LEARNING OBJECTIVES
${JSON.parse(summary.learningObjectives || '[]').map((obj: string, i: number) => `${i + 1}. ${obj}`).join('\n')}

PREPARATION NOTES
${summary.preparationNotes || 'None'}

Generated: ${new Date(summary.generatedAt).toLocaleString()}
                                                            `.trim();

                                                            const blob = new Blob([content], { type: 'text/plain' });
                                                            const url = window.URL.createObjectURL(blob);
                                                            const a = document.createElement('a');
                                                            a.href = url;
                                                            a.download = `Week_${summary.weekNumber}_Summary.txt`;
                                                            document.body.appendChild(a);
                                                            a.click();
                                                            window.URL.revokeObjectURL(url);
                                                            document.body.removeChild(a);
                                                            toast.success('Summary downloaded!');
                                                        }}
                                                        className="text-green-600 hover:text-green-900 bg-green-50 p-2 rounded-lg"
                                                        title="Download Summary"
                                                    >
                                                        <DocumentArrowDownIcon className="h-5 w-5" />
                                                    </button>
                                                    <button
                                                        onClick={() => {
                                                            if (window.confirm('Are you sure you want to delete this summary? This action cannot be undone.')) {
                                                                deleteSummaryMutation.mutate(summary.id);
                                                            }
                                                        }}
                                                        disabled={deleteSummaryMutation.isPending}
                                                        className="text-red-600 hover:text-red-900 bg-red-50 p-2 rounded-lg disabled:opacity-50"
                                                        title="Delete Summary"
                                                    >
                                                        <TrashIcon className="h-5 w-5" />
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}

                {/* Course Info */}
                <div className="mt-6 bg-white rounded-xl shadow-lg p-6">
                    <h2 className="text-xl font-bold text-gray-900 mb-4">Course Information</h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div>
                            <p className="text-sm text-gray-600">Course Code</p>
                            <p className="font-medium text-gray-900">{course.courseCode}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-600">Credit Hours</p>
                            <p className="font-medium text-gray-900">{course.creditHours}</p>
                        </div>
                        {course.description && (
                            <div className="md:col-span-2">
                                <p className="text-sm text-gray-600">Description</p>
                                <p className="font-medium text-gray-900">{course.description}</p>
                            </div>
                        )}
                    </div>
                </div>
            </main>

            {/* Summarize Week Modal */}
            {showSummarizeModal && syllabusInfo && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                    <div className="bg-white rounded-xl p-8 max-w-md w-full mx-4">
                        <h2 className="text-2xl font-bold text-gray-900 mb-4">Generate Week Summary</h2>

                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Select Week
                                </label>
                                <select
                                    value={selectedWeek}
                                    onChange={(e) => setSelectedWeek(parseInt(e.target.value))}
                                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                >
                                    {Array.from({ length: syllabusInfo.totalWeeks || 12 }, (_, i) => i + 1).map(week => (
                                        <option key={week} value={week}>Week {week}</option>
                                    ))}
                                </select>
                            </div>

                            <p className="text-sm text-gray-600">
                                AI will analyze the syllabus and generate a comprehensive summary for the selected week.
                            </p>

                            <div className="flex space-x-4">
                                <button
                                    onClick={handleSummarize}
                                    disabled={summarizeWeekMutation.isPending}
                                    className="flex-1 bg-indigo-600 text-white px-6 py-3 rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50"
                                >
                                    {summarizeWeekMutation.isPending ? 'Generating...' : 'Generate Summary'}
                                </button>
                                <button
                                    onClick={() => setShowSummarizeModal(false)}
                                    className="flex-1 bg-gray-200 text-gray-700 px-6 py-3 rounded-lg hover:bg-gray-300 transition-colors"
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Student Import Modal */}
            {showStudentImport && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                    <div className="bg-white rounded-xl p-8 max-w-md w-full mx-4">
                        <h2 className="text-2xl font-bold text-gray-900 mb-4">Import Students</h2>

                        <div className="space-y-4">
                            <button
                                onClick={downloadTemplate}
                                className="w-full text-indigo-600 hover:text-indigo-800 text-sm font-medium"
                            >
                                ↓ Download Excel Template
                            </button>

                            <input
                                ref={studentFileInputRef}
                                type="file"
                                accept=".xlsx,.csv"
                                onChange={handleStudentFileUpload}
                                className="hidden"
                            />

                            <button
                                onClick={() => studentFileInputRef.current?.click()}
                                disabled={importStudentsMutation.isPending}
                                className="w-full bg-indigo-600 text-white px-4 py-3 rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50"
                            >
                                {importStudentsMutation.isPending ? 'Importing...' : 'Choose Excel File'}
                            </button>

                            <p className="text-sm text-gray-500">
                                Excel file should have columns: Full Name, Matric Number, Email
                            </p>

                            <button
                                onClick={() => setShowStudentImport(false)}
                                className="w-full bg-gray-200 text-gray-700 px-4 py-3 rounded-lg hover:bg-gray-300 transition-colors"
                            >
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Manual Add Student Modal */}
            {showManualAdd && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                    <div className="bg-white rounded-xl p-8 max-w-md w-full mx-4">
                        <h2 className="text-2xl font-bold text-gray-900 mb-6">Add Student</h2>

                        <form onSubmit={handleManualAdd} className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Full Name
                                </label>
                                <input
                                    type="text"
                                    required
                                    value={newStudent.fullName}
                                    onChange={(e) => setNewStudent({ ...newStudent, fullName: e.target.value })}
                                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                    placeholder="John Doe"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Matric Number
                                </label>
                                <input
                                    type="text"
                                    required
                                    value={newStudent.matricNumber}
                                    onChange={(e) => setNewStudent({ ...newStudent, matricNumber: e.target.value })}
                                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                    placeholder="MAT001"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Email
                                </label>
                                <input
                                    type="email"
                                    required
                                    value={newStudent.email}
                                    onChange={(e) => setNewStudent({ ...newStudent, email: e.target.value })}
                                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                                    placeholder="john@example.com"
                                />
                            </div>

                            <div className="flex space-x-4 pt-4">
                                <button
                                    type="submit"
                                    disabled={addStudentMutation.isPending}
                                    className="flex-1 bg-indigo-600 text-white px-6 py-3 rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50"
                                >
                                    {addStudentMutation.isPending ? 'Adding...' : 'Add Student'}
                                </button>
                                <button
                                    type="button"
                                    onClick={() => setShowManualAdd(false)}
                                    className="flex-1 bg-gray-200 text-gray-700 px-6 py-3 rounded-lg hover:bg-gray-300 transition-colors"
                                >
                                    Cancel
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
            {/* Send to Students Modal */}
            {showSendModal && summaryToSend && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white rounded-xl shadow-2xl max-w-md w-full max-h-[80vh] flex flex-col">
                        <div className="p-6 border-b border-gray-200 flex items-center justify-between">
                            <div>
                                <h2 className="text-xl font-bold text-gray-900">Send to Students</h2>
                                <p className="text-sm text-gray-500">Week {summaryToSend.weekNumber}: {summaryToSend.weekTitle}</p>
                            </div>
                            <button onClick={() => setShowSendModal(false)} className="text-gray-400 hover:text-gray-600">
                                <XMarkIcon className="h-6 w-6" />
                            </button>
                        </div>

                        <div className="flex-1 overflow-y-auto p-6">
                            <div className="mb-4 flex items-center justify-between bg-gray-50 p-3 rounded-lg">
                                <span className="text-sm font-medium text-gray-900">Enrolled Students ({enrollments?.length || 0})</span>
                                <label className="flex items-center space-x-2 cursor-pointer">
                                    <input
                                        type="checkbox"
                                        checked={selectedStudentIds.length === (enrollments?.length || 0) && (enrollments?.length || 0) > 0}
                                        onChange={(e) => {
                                            if (e.target.checked) {
                                                setSelectedStudentIds(enrollments?.map((e: any) => e.studentId) || []);
                                            } else {
                                                setSelectedStudentIds([]);
                                            }
                                        }}
                                        className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                                    />
                                    <span className="text-xs text-gray-600">Select All</span>
                                </label>
                            </div>

                            <div className="space-y-2">
                                {enrollments?.map((enrollment: any) => (
                                    <label key={enrollment.id} className="flex items-center justify-between p-3 border border-gray-100 rounded-lg hover:bg-blue-50 transition-colors cursor-pointer">
                                        <div className="flex items-center space-x-3">
                                            <input
                                                type="checkbox"
                                                checked={selectedStudentIds.includes(enrollment.studentId)}
                                                onChange={(e) => {
                                                    if (e.target.checked) {
                                                        setSelectedStudentIds([...selectedStudentIds, enrollment.studentId]);
                                                    } else {
                                                        setSelectedStudentIds(selectedStudentIds.filter(id => id !== enrollment.studentId));
                                                    }
                                                }}
                                                className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                                            />
                                            <div className="text-left">
                                                <p className="text-sm font-medium text-gray-900">{enrollment.student?.fullName}</p>
                                                <p className="text-xs text-gray-500">{enrollment.student?.matricNumber}</p>
                                            </div>
                                        </div>
                                    </label>
                                ))}
                                {(!enrollments || enrollments.length === 0) && (
                                    <p className="text-center text-gray-500 py-4">No students enrolled in this course.</p>
                                )}
                            </div>
                        </div>

                        <div className="p-6 border-t border-gray-200 bg-gray-50 flex space-x-3">
                            <button
                                onClick={() => setShowSendModal(false)}
                                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-100 transition-colors"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={() => sendSummaryMutation.mutate({
                                    summaryId: summaryToSend.id,
                                    studentIds: selectedStudentIds
                                })}
                                disabled={selectedStudentIds.length === 0 || sendSummaryMutation.isPending}
                                className="flex-1 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2"
                            >
                                <PaperAirplaneIcon className="h-4 w-4" />
                                <span>{sendSummaryMutation.isPending ? 'Sending...' : `Send to ${selectedStudentIds.length} Students`}</span>
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* View Summary Modal */}
            {showSummaryView && selectedSummary && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] flex flex-col">
                        <div className="p-6 border-b border-gray-200 flex items-center justify-between bg-indigo-50 rounded-t-xl">
                            <div>
                                <h2 className="text-xl font-bold text-gray-900">Weekly Summary</h2>
                                <p className="text-sm text-indigo-600 font-medium">Week {selectedSummary.weekNumber}: {selectedSummary.weekTitle}</p>
                            </div>
                            <button
                                onClick={() => {
                                    setShowSummaryView(false);
                                    setSelectedSummary(null);
                                }}
                                className="text-gray-400 hover:text-gray-600 p-1 hover:bg-gray-100 rounded-full transition-colors"
                            >
                                <XMarkIcon className="h-6 w-6" />
                            </button>
                        </div>

                        <div className="flex-1 overflow-y-auto p-6 space-y-6">
                            <section>
                                <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-2">Detailed Summary</h3>
                                <div className="bg-gray-50 p-4 rounded-lg border border-gray-100">
                                    <p className="text-gray-700 whitespace-pre-wrap leading-relaxed">{selectedSummary.summary}</p>
                                </div>
                            </section>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <section>
                                    <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-2">Key Topics</h3>
                                    <div className="flex flex-wrap gap-2">
                                        {(() => {
                                            try {
                                                const topics = typeof selectedSummary.keyTopics === 'string'
                                                    ? JSON.parse(selectedSummary.keyTopics)
                                                    : selectedSummary.keyTopics;
                                                return Array.isArray(topics)
                                                    ? topics.map((topic: string, i: number) => (
                                                        <span key={i} className="px-2 py-1 bg-blue-50 text-blue-700 text-xs font-medium rounded border border-blue-100 italic">
                                                            #{topic}
                                                        </span>
                                                    ))
                                                    : <p className="text-gray-600">{selectedSummary.keyTopics}</p>;
                                            } catch {
                                                return <p className="text-gray-600">{selectedSummary.keyTopics}</p>;
                                            }
                                        })()}
                                    </div>
                                </section>

                                <section>
                                    <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-2">Learning Objectives</h3>
                                    <ul className="space-y-1">
                                        {(() => {
                                            try {
                                                const objs = typeof selectedSummary.learningObjectives === 'string'
                                                    ? JSON.parse(selectedSummary.learningObjectives)
                                                    : selectedSummary.learningObjectives;
                                                return Array.isArray(objs)
                                                    ? objs.map((obj: string, i: number) => (
                                                        <li key={i} className="text-sm text-gray-600 flex items-start">
                                                            <span className="text-green-500 mr-2">✓</span>
                                                            {obj}
                                                        </li>
                                                    ))
                                                    : <li className="text-sm text-gray-600">{selectedSummary.learningObjectives}</li>;
                                            } catch {
                                                return <li className="text-sm text-gray-600">{selectedSummary.learningObjectives}</li>;
                                            }
                                        })()}
                                    </ul>
                                </section>
                            </div>

                            {selectedSummary.preparationNotes && (
                                <section>
                                    <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-2">Preparation Notes</h3>
                                    <div className="bg-amber-50 p-4 rounded-lg border border-amber-100 text-amber-800 text-sm italic">
                                        {selectedSummary.preparationNotes}
                                    </div>
                                </section>
                            )}
                        </div>

                        <div className="p-4 border-t border-gray-100 bg-gray-50 rounded-b-xl flex justify-end">
                            <button
                                onClick={() => {
                                    setShowSummaryView(false);
                                    setSelectedSummary(null);
                                }}
                                className="px-6 py-2 bg-gray-900 text-white rounded-lg hover:bg-gray-800 transition-colors font-medium"
                            >
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
