import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5152/api';

// Student API
export const studentApi = {
    login: async (matricNumber: string, password: string) => {
        const response = await axios.post(`${API_BASE_URL}/students/login`, {
            username: matricNumber,
            password,
        });
        return response.data;
    },

    getProfile: async () => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.get(`${API_BASE_URL}/students/profile`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    updateProfile: async (formData: FormData) => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.post(`${API_BASE_URL}/students/profile`, formData, {
            headers: {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'multipart/form-data',
            },
        });
        return response.data;
    },

    getCourses: async () => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.get(`${API_BASE_URL}/students/courses`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getMyCourses: async () => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.get(`${API_BASE_URL}/students/my-courses`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    enrollInCourse: async (courseId: number) => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.post(
            `${API_BASE_URL}/students/enroll/${courseId}`,
            {},
            {
                headers: { Authorization: `Bearer ${token}` },
            }
        );
        return response.data;
    },

    unenrollFromCourse: async (courseId: number) => {
        const token = localStorage.getItem('studentToken');
        await axios.delete(`${API_BASE_URL}/students/unenroll/${courseId}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
    },

    getClassSummaries: async () => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.get(`${API_BASE_URL}/students/class-summaries`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getCourseSummariesByCode: async (courseCode: string) => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.get(`${API_BASE_URL}/students/course-summaries/${courseCode}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    askAI: async (summaryId: number | null, question: string) => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.post(
            `${API_BASE_URL}/chat/ask`,
            { summaryId, question },
            {
                headers: { Authorization: `Bearer ${token}` },
            }
        );
        return response.data;
    },

    getSessionMaterials: async (sessionId: number) => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.get(`${API_BASE_URL}/materials/session/${sessionId}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },
    getMyWhiteboards: async () => {
        const token = localStorage.getItem('studentToken');
        const user = JSON.parse(localStorage.getItem('studentUser') || '{}');
        const response = await axios.get(`${API_BASE_URL}/materials/student/${user.id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getMyAttendance: async () => {
        const token = localStorage.getItem('studentToken');
        const user = JSON.parse(localStorage.getItem('studentUser') || '{}');
        const response = await axios.get(`${API_BASE_URL}/attendance/student/${user.id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getActiveLiveStreams: async () => {
        const response = await axios.get(`${API_BASE_URL}/stream/active`);
        return response.data;
    },

    getCourseVideos: async (courseId: number) => {
        const token = localStorage.getItem('studentToken');
        const response = await axios.get(`${API_BASE_URL}/CourseVideos/course/${courseId}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },
};
