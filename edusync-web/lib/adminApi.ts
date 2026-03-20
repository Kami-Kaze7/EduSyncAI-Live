import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5152/api';

// Admin API
export const adminApi = {
    login: async (username: string, password: string) => {
        const response = await axios.post(`${API_BASE_URL}/admin/login`, {
            username,
            password,
        });
        return response.data;
    },

    // Lecturer management
    getLecturers: async () => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.get(`${API_BASE_URL}/admin/lecturers`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    createLecturer: async (data: {
        username: string;
        fullName: string;
        email: string;
        password?: string;
    }) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.post(`${API_BASE_URL}/admin/lecturers`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    importLecturers: async (file: File) => {
        const token = localStorage.getItem('adminToken');
        const formData = new FormData();
        formData.append('file', file);
        const response = await axios.post(
            `${API_BASE_URL}/admin/lecturers/import`,
            formData,
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                    'Content-Type': 'multipart/form-data',
                },
            }
        );
        return response.data;
    },

    deleteLecturer: async (id: number) => {
        const token = localStorage.getItem('adminToken');
        await axios.delete(`${API_BASE_URL}/admin/lecturers/${id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
    },

    // Student management
    getStudents: async () => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.get(`${API_BASE_URL}/admin/students`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    createStudent: async (data: {
        matricNumber: string;
        fullName: string;
        email?: string;
        password?: string;
    }) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.post(`${API_BASE_URL}/admin/students`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    importStudents: async (file: File) => {
        const token = localStorage.getItem('adminToken');
        const formData = new FormData();
        formData.append('file', file);
        const response = await axios.post(
            `${API_BASE_URL}/admin/students/import`,
            formData,
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                    'Content-Type': 'multipart/form-data',
                },
            }
        );
        return response.data;
    },

    deleteStudent: async (id: number) => {
        const token = localStorage.getItem('adminToken');
        await axios.delete(`${API_BASE_URL}/admin/students/${id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
    },

    enrollStudentInAllCourses: async (id: number) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.post(`${API_BASE_URL}/admin/students/${id}/enroll-all`, {}, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    // Course management
    getCourses: async () => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.get(`${API_BASE_URL}/admin/courses`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    createCourse: async (data: {
        courseCode: string;
        courseName: string;
        description?: string;
        creditHours: number;
        lecturerId: number;
    }) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.post(`${API_BASE_URL}/admin/courses`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    deleteCourse: async (id: number) => {
        const token = localStorage.getItem('adminToken');
        await axios.delete(`${API_BASE_URL}/admin/courses/${id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
    },
};
