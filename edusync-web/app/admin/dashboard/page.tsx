'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { adminApi } from '@/lib/adminApi';
import toast from 'react-hot-toast';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

export default function AdminDashboard() {
    const router = useRouter();
    const queryClient = useQueryClient();
    const [activeTab, setActiveTab] = useState<'lecturers' | 'students'>('lecturers');
    const [showAddModal, setShowAddModal] = useState(false);

    // Check authentication
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

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900">Admin Dashboard</h1>
                        <p className="text-sm text-gray-600">Manage lecturers and students</p>
                    </div>
                    <button
                        onClick={handleLogout}
                        className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                    >
                        Logout
                    </button>
                </div>
            </header>

            {/* Tabs */}
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-8">
                <div className="border-b border-gray-200">
                    <nav className="-mb-px flex space-x-8">
                        <button
                            onClick={() => setActiveTab('lecturers')}
                            className={`${activeTab === 'lecturers'
                                    ? 'border-indigo-500 text-indigo-600'
                                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
                        >
                            Lecturers
                        </button>
                        <button
                            onClick={() => setActiveTab('students')}
                            className={`${activeTab === 'students'
                                    ? 'border-indigo-500 text-indigo-600'
                                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
                        >
                            Students
                        </button>
                    </nav>
                </div>

                {/* Content */}
                <div className="mt-8">
                    {activeTab === 'lecturers' ? <LecturersTab /> : <StudentsTab />}
                </div>
            </div>
        </div>
    );
}

// Lecturers Tab Component
function LecturersTab() {
    const queryClient = useQueryClient();
    const [showAddModal, setShowAddModal] = useState(false);
    const [showImportModal, setShowImportModal] = useState(false);

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
        onError: () => {
            toast.error('Failed to delete lecturer');
        },
    });

    const handleDelete = (id: number) => {
        if (confirm('Are you sure you want to delete this lecturer?')) {
            deleteMutation.mutate(id);
        }
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <h2 className="text-xl font-semibold text-gray-900">Lecturers</h2>
                <div className="flex gap-3">
                    <button
                        onClick={() => setShowImportModal(true)}
                        className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
                    >
                        Import Excel
                    </button>
                    <button
                        onClick={() => setShowAddModal(true)}
                        className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
                    >
                        + Add Lecturer
                    </button>
                </div>
            </div>

            {isLoading ? (
                <div className="text-center py-12">Loading...</div>
            ) : (
                <div className="bg-white shadow-sm rounded-lg overflow-hidden">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Username
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Full Name
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Email
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Status
                                </th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Actions
                                </th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {lecturers?.map((lecturer: any) => (
                                <tr key={lecturer.id}>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                        {lecturer.username}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                        {lecturer.fullName}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                        {lecturer.email}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <span
                                            className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${lecturer.isActive
                                                    ? 'bg-green-100 text-green-800'
                                                    : 'bg-red-100 text-red-800'
                                                }`}
                                        >
                                            {lecturer.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                        <button
                                            onClick={() => handleDelete(lecturer.id)}
                                            className="text-red-600 hover:text-red-900"
                                        >
                                            Delete
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {showAddModal && (
                <AddLecturerModal onClose={() => setShowAddModal(false)} />
            )}
            {showImportModal && (
                <ImportLecturersModal onClose={() => setShowImportModal(false)} />
            )}
        </div>
    );
}

// Students Tab Component
function StudentsTab() {
    const queryClient = useQueryClient();
    const [showAddModal, setShowAddModal] = useState(false);
    const [showImportModal, setShowImportModal] = useState(false);

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
        onError: () => {
            toast.error('Failed to delete student');
        },
    });

    const handleDelete = (id: number) => {
        if (confirm('Are you sure you want to delete this student?')) {
            deleteMutation.mutate(id);
        }
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <h2 className="text-xl font-semibold text-gray-900">Students</h2>
                <div className="flex gap-3">
                    <button
                        onClick={() => setShowImportModal(true)}
                        className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
                    >
                        Import Excel
                    </button>
                    <button
                        onClick={() => setShowAddModal(true)}
                        className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
                    >
                        + Add Student
                    </button>
                </div>
            </div>

            {isLoading ? (
                <div className="text-center py-12">Loading...</div>
            ) : (
                <div className="bg-white shadow-sm rounded-lg overflow-hidden">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Matric Number
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Full Name
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Email
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Status
                                </th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Actions
                                </th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {students?.map((student: any) => (
                                <tr key={student.id}>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                        {student.matricNumber}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                        {student.fullName}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                        {student.email}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <span
                                            className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${student.isActive
                                                    ? 'bg-green-100 text-green-800'
                                                    : 'bg-red-100 text-red-800'
                                                }`}
                                        >
                                            {student.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                        <button
                                            onClick={() => handleDelete(student.id)}
                                            className="text-red-600 hover:text-red-900"
                                        >
                                            Delete
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {showAddModal && <AddStudentModal onClose={() => setShowAddModal(false)} />}
            {showImportModal && (
                <ImportStudentsModal onClose={() => setShowImportModal(false)} />
            )}
        </div>
    );
}

// Add Lecturer Modal
function AddLecturerModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [formData, setFormData] = useState({
        username: '',
        fullName: '',
        email: '',
        password: '',
    });
    const [generatedPassword, setGeneratedPassword] = useState('');

    const createMutation = useMutation({
        mutationFn: adminApi.createLecturer,
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['lecturers'] });
            setGeneratedPassword(data.generatedPassword);
            toast.success('Lecturer created successfully!');
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to create lecturer');
        },
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        createMutation.mutate(formData);
    };

    const handleGenerate = () => {
        const randomPassword = Math.random().toString(36).slice(-8);
        setFormData({ ...formData, password: randomPassword });
    };

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-md">
                <h3 className="text-lg font-semibold mb-4">Add Lecturer</h3>

                {generatedPassword ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">Lecturer created successfully!</p>
                            <p className="text-sm font-semibold">Generated Password:</p>
                            <p className="text-lg font-mono bg-white px-3 py-2 rounded mt-1">
                                {generatedPassword}
                            </p>
                            <p className="text-xs text-green-700 mt-2">
                                Please save this password. It won't be shown again.
                            </p>
                        </div>
                        <button
                            onClick={onClose}
                            className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
                        >
                            Close
                        </button>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Username
                            </label>
                            <input
                                type="text"
                                value={formData.username}
                                onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500"
                                required
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Full Name
                            </label>
                            <input
                                type="text"
                                value={formData.fullName}
                                onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500"
                                required
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Email
                            </label>
                            <input
                                type="email"
                                value={formData.email}
                                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500"
                                required
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Password
                            </label>
                            <div className="flex gap-2">
                                <input
                                    type="text"
                                    value={formData.password}
                                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                                    className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500"
                                    placeholder="Leave empty to auto-generate"
                                />
                                <button
                                    type="button"
                                    onClick={handleGenerate}
                                    className="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700"
                                >
                                    Generate
                                </button>
                            </div>
                        </div>

                        <div className="flex gap-3 pt-4">
                            <button
                                type="button"
                                onClick={onClose}
                                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                disabled={createMutation.isPending}
                                className="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
                            >
                                {createMutation.isPending ? 'Creating...' : 'Create'}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}

// Add Student Modal
function AddStudentModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [formData, setFormData] = useState({
        matricNumber: '',
        fullName: '',
        email: '',
        password: '',
    });
    const [generatedPassword, setGeneratedPassword] = useState('');

    const createMutation = useMutation({
        mutationFn: adminApi.createStudent,
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['students'] });
            setGeneratedPassword(data.generatedPassword);
            toast.success('Student created successfully!');
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to create student');
        },
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        createMutation.mutate(formData);
    };

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-md">
                <h3 className="text-lg font-semibold mb-4">Add Student</h3>

                {generatedPassword ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">Student created successfully!</p>
                            <p className="text-sm font-semibold">Generated Password:</p>
                            <p className="text-lg font-mono bg-white px-3 py-2 rounded mt-1">
                                {generatedPassword}
                            </p>
                            <p className="text-xs text-green-700 mt-2">
                                Please save this password. It won't be shown again.
                            </p>
                        </div>
                        <button
                            onClick={onClose}
                            className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
                        >
                            Close
                        </button>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Matric Number
                            </label>
                            <input
                                type="text"
                                value={formData.matricNumber}
                                onChange={(e) => setFormData({ ...formData, matricNumber: e.target.value })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500"
                                required
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Full Name
                            </label>
                            <input
                                type="text"
                                value={formData.fullName}
                                onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500"
                                required
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Email (Optional)
                            </label>
                            <input
                                type="email"
                                value={formData.email}
                                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500"
                            />
                        </div>

                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
                            <p className="text-sm text-blue-800">
                                Password will be auto-generated as the matric number
                            </p>
                        </div>

                        <div className="flex gap-3 pt-4">
                            <button
                                type="button"
                                onClick={onClose}
                                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                disabled={createMutation.isPending}
                                className="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
                            >
                                {createMutation.isPending ? 'Creating...' : 'Create'}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}

// Import Lecturers Modal
function ImportLecturersModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [file, setFile] = useState<File | null>(null);
    const [results, setResults] = useState<any>(null);

    const importMutation = useMutation({
        mutationFn: adminApi.importLecturers,
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['lecturers'] });
            setResults(data);
            toast.success(`Imported ${data.success} lecturers successfully!`);
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to import lecturers');
        },
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (file) {
            importMutation.mutate(file);
        }
    };

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[80vh] overflow-y-auto">
                <h3 className="text-lg font-semibold mb-4">Import Lecturers from Excel</h3>

                {results ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">
                                Successfully imported {results.success} lecturers
                            </p>
                            {results.failed > 0 && (
                                <p className="text-sm text-red-800">
                                    Failed: {results.failed} rows
                                </p>
                            )}
                        </div>

                        {results.lecturers && results.lecturers.length > 0 && (
                            <div>
                                <h4 className="font-semibold mb-2">Generated Credentials:</h4>
                                <div className="bg-gray-50 rounded-lg p-4 max-h-60 overflow-y-auto">
                                    {results.lecturers.map((lec: any, idx: number) => (
                                        <div key={idx} className="mb-3 pb-3 border-b border-gray-200 last:border-0">
                                            <p className="text-sm">
                                                <span className="font-semibold">{lec.fullName}</span> ({lec.username})
                                            </p>
                                            <p className="text-xs text-gray-600">
                                                Password: <span className="font-mono">{lec.generatedPassword}</span>
                                            </p>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}

                        <button
                            onClick={onClose}
                            className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
                        >
                            Close
                        </button>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                            <p className="text-sm text-blue-800 font-semibold mb-2">Excel Format:</p>
                            <p className="text-xs text-blue-700">
                                Column 1: Full Name | Column 2: Email
                            </p>
                            <p className="text-xs text-blue-700 mt-1">
                                Username will be generated from email (before @)
                            </p>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Upload Excel File
                            </label>
                            <input
                                type="file"
                                accept=".xlsx,.xls"
                                onChange={(e) => setFile(e.target.files?.[0] || null)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                                required
                            />
                        </div>

                        <div className="flex gap-3 pt-4">
                            <button
                                type="button"
                                onClick={onClose}
                                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                disabled={importMutation.isPending || !file}
                                className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                            >
                                {importMutation.isPending ? 'Importing...' : 'Import'}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}

// Import Students Modal
function ImportStudentsModal({ onClose }: { onClose: () => void }) {
    const queryClient = useQueryClient();
    const [file, setFile] = useState<File | null>(null);
    const [results, setResults] = useState<any>(null);

    const importMutation = useMutation({
        mutationFn: adminApi.importStudents,
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['students'] });
            setResults(data);
            toast.success(`Imported ${data.success} students successfully!`);
        },
        onError: (error: any) => {
            toast.error(error.response?.data?.error || 'Failed to import students');
        },
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (file) {
            importMutation.mutate(file);
        }
    };

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[80vh] overflow-y-auto">
                <h3 className="text-lg font-semibold mb-4">Import Students from Excel</h3>

                {results ? (
                    <div className="space-y-4">
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                            <p className="text-sm text-green-800 mb-2">
                                Successfully imported {results.success} students
                            </p>
                            {results.failed > 0 && (
                                <p className="text-sm text-red-800">
                                    Failed: {results.failed} rows
                                </p>
                            )}
                        </div>

                        {results.students && results.students.length > 0 && (
                            <div>
                                <h4 className="font-semibold mb-2">Generated Credentials:</h4>
                                <div className="bg-gray-50 rounded-lg p-4 max-h-60 overflow-y-auto">
                                    {results.students.map((student: any, idx: number) => (
                                        <div key={idx} className="mb-3 pb-3 border-b border-gray-200 last:border-0">
                                            <p className="text-sm">
                                                <span className="font-semibold">{student.fullName}</span> ({student.matricNumber})
                                            </p>
                                            <p className="text-xs text-gray-600">
                                                Password: <span className="font-mono">{student.generatedPassword}</span>
                                            </p>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}

                        <button
                            onClick={onClose}
                            className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
                        >
                            Close
                        </button>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                            <p className="text-sm text-blue-800 font-semibold mb-2">Excel Format:</p>
                            <p className="text-xs text-blue-700">
                                Column 1: Full Name | Column 2: Matric Number
                            </p>
                            <p className="text-xs text-blue-700 mt-1">
                                Password will be set as the matric number
                            </p>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Upload Excel File
                            </label>
                            <input
                                type="file"
                                accept=".xlsx,.xls"
                                onChange={(e) => setFile(e.target.files?.[0] || null)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                                required
                            />
                        </div>

                        <div className="flex gap-3 pt-4">
                            <button
                                type="button"
                                onClick={onClose}
                                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                disabled={importMutation.isPending || !file}
                                className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                            >
                                {importMutation.isPending ? 'Importing...' : 'Import'}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}
