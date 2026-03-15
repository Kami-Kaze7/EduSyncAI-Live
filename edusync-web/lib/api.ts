import axios from 'axios';
import type { Course, ClassSession, LectureMaterial, CourseEnrollment } from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5152/api';

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Add request interceptor to include auth token
api.interceptors.request.use(
    (config) => {
        if (typeof window !== 'undefined') {
            const token = localStorage.getItem('auth_token');
            if (token) {
                config.headers.Authorization = `Bearer ${token}`;
            }
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Course API
export const courseApi = {
    getAll: async (lecturerId?: number) => {
        const params = lecturerId ? { lecturerId } : {};
        const { data } = await api.get<Course[]>('/courses', { params });
        return data;
    },

    getById: async (id: number) => {
        const { data } = await api.get<Course>(`/courses/${id}`);
        return data;
    },

    create: async (course: Omit<Course, 'id' | 'createdAt'>) => {
        const { data } = await api.post<Course>('/courses', course);
        return data;
    },

    update: async (id: number, course: Partial<Course>) => {
        await api.put(`/courses/${id}`, { ...course, id });
    },

    delete: async (id: number) => {
        await api.delete(`/courses/${id}`);
    },

    uploadSyllabus: async (courseId: number, file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        const { data } = await api.post(`/courses/${courseId}/syllabus`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return data;
    },

    downloadSyllabus: async (courseId: number) => {
        const response = await api.get(`/courses/${courseId}/syllabus/download`, {
            responseType: 'blob'
        });
        return response.data;
    },

    importStudents: async (courseId: number, file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        const { data } = await api.post(`/courses/${courseId}/students/import`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return data;
    },

    addStudent: async (courseId: number, student: { fullName: string, matricNumber: string, email: string }) => {
        const { data } = await api.post(`/courses/${courseId}/students`, student);
        return data;
    },

    getEnrollments: async (courseId: number) => {
        const { data } = await api.get<CourseEnrollment[]>(`/courses/${courseId}/enrollments`);
        return data;
    },

    enrollStudents: async (courseId: number, studentIds: number[]) => {
        await api.post(`/courses/${courseId}/enrollments`, studentIds);
    },

    // AI Syllabus Summarization
    analyzeSyllabus: async (courseId: number, file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        const { data } = await api.post(`/courses/${courseId}/syllabus/analyze`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return data;
    },

    getSyllabusInfo: async (courseId: number) => {
        const { data } = await api.get(`/courses/${courseId}/syllabus/info`);
        return data;
    },

    deleteSyllabus: async (courseId: number) => {
        const { data } = await api.delete(`/courses/${courseId}/syllabus`);
        return data;
    },

    summarizeWeek: async (courseId: number, weekNumber: number) => {
        const { data } = await api.post(`/courses/${courseId}/syllabus/summarize`, { weekNumber });
        return data;
    },

    getCourseSummaries: async (courseId: number) => {
        const { data } = await api.get(`/courses/${courseId}/summaries`);
        return data;
    },

    sendSummaryToStudents: async (courseId: number, summaryId: number, studentIds: number[]) => {
        const { data } = await api.post(`/courses/${courseId}/summaries/${summaryId}/send`, { studentIds });
        return data;
    },

    deleteSummary: async (courseId: number, summaryId: number) => {
        const { data } = await api.delete(`/courses/${courseId}/summaries/${summaryId}`);
        return data;
    },
};

// Session API
export const sessionApi = {
    getAll: async (filters?: { courseId?: number; lecturerId?: number; startDate?: string; endDate?: string }) => {
        const { data } = await api.get<ClassSession[]>('/sessions', { params: filters });
        return data;
    },

    getById: async (id: number) => {
        const { data } = await api.get<ClassSession>(`/sessions/${id}`);
        return data;
    },

    create: async (session: Omit<ClassSession, 'id'>) => {
        const { data } = await api.post<ClassSession>('/sessions', session);
        return data;
    },

    update: async (id: number, session: Partial<ClassSession>) => {
        await api.put(`/sessions/${id}`, { ...session, id });
    },

    delete: async (id: number) => {
        await api.delete(`/sessions/${id}`);
    },

    updateNotes: async (sessionId: number, content: string) => {
        await api.put(`/sessions/${sessionId}/notes`, JSON.stringify(content), {
            headers: { 'Content-Type': 'application/json' },
        });
    },
};

// Materials API
export const materialsApi = {
    getBySession: async (sessionId: number) => {
        const { data } = await api.get<LectureMaterial[]>(`/materials/session/${sessionId}`);
        return data;
    },

    getByLecturer: async (lecturerId: number) => {
        const { data } = await api.get<any[]>(`/materials/lecturer/${lecturerId}`);
        return data;
    },

    upload: async (sessionId: number, file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        const { data } = await api.post<LectureMaterial>(`/materials/session/${sessionId}`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        });
        return data;
    },

    download: async (materialId: number) => {
        const response = await api.get(`/materials/${materialId}/download`, {
            responseType: 'blob',
        });
        return response.data;
    },

    delete: async (materialId: number) => {
        await api.delete(`/materials/${materialId}`);
    },
};

export const attendanceApi = {
    getBySession: async (sessionId: number) => {
        const { data } = await api.get<any[]>(`/attendance/session/${sessionId}`);
        return data;
    },
    getByStudent: async (studentId: number) => {
        const { data } = await api.get<any[]>(`/attendance/student/${studentId}`);
        return data;
    },
    upload: async (sessionId: number, records: any[]) => {
        const { data } = await api.post(`/attendance/session/${sessionId}`, records);
        return data;
    }
};

export default api;
