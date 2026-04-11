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
        yearOfStudyId?: number;
    }) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.post(`${API_BASE_URL}/admin/students`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    importStudents: async (file: File, yearOfStudyId?: number) => {
        const token = localStorage.getItem('adminToken');
        const formData = new FormData();
        formData.append('file', file);
        if (yearOfStudyId) {
            formData.append('yearOfStudyId', yearOfStudyId.toString());
        }
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
        const response = await axios.get(`${API_BASE_URL}/admin/course-list`, {
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
        const response = await axios.post(`${API_BASE_URL}/admin/course-create`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    deleteCourse: async (id: number) => {
        const token = localStorage.getItem('adminToken');
        await axios.delete(`${API_BASE_URL}/admin/course-delete/${id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
    },

    assignLecturerToCourse: async (courseId: number, lecturerId: number) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.put(`${API_BASE_URL}/admin/course-assign-lecturer`, { courseId, lecturerId }, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    // 3D Repository
    upload3DModelAsset: async (data: {
        title: string;
        description: string;
        discipline: string;
        modelFile: File;
        thumbnailFile?: File;
    }) => {
        const token = localStorage.getItem('adminToken');
        const formData = new FormData();
        formData.append('title', data.title);
        formData.append('description', data.description || '');
        formData.append('discipline', data.discipline);
        formData.append('modelFile', data.modelFile);
        if (data.thumbnailFile) {
            formData.append('thumbnailFile', data.thumbnailFile);
        }

        const response = await axios.post(
            `${API_BASE_URL}/ModelAssets`,
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

    // Academic Hierarchy
    getFaculties: async () => {
        const response = await axios.get(`${API_BASE_URL}/AcademicHierarchy/faculties`);
        return response.data;
    },
    createFaculty: async (name: string) => {
        const response = await axios.post(`${API_BASE_URL}/AcademicHierarchy/faculties`, { name });
        return response.data;
    },
    getDepartments: async (facultyId: number) => {
        const response = await axios.get(`${API_BASE_URL}/AcademicHierarchy/faculties/${facultyId}/departments`);
        return response.data;
    },
    createDepartment: async (facultyId: number, name: string) => {
        const response = await axios.post(`${API_BASE_URL}/AcademicHierarchy/faculties/${facultyId}/departments`, { name });
        return response.data;
    },
    getYears: async (departmentId: number) => {
        const response = await axios.get(`${API_BASE_URL}/AcademicHierarchy/departments/${departmentId}/years`);
        return response.data;
    },
    createYear: async (departmentId: number, name: string, level: number) => {
        const response = await axios.post(`${API_BASE_URL}/AcademicHierarchy/departments/${departmentId}/years`, { name, level });
        return response.data;
    },
    assignCourseToYear: async (yearId: number, courseId: number) => {
        const response = await axios.post(`${API_BASE_URL}/AcademicHierarchy/years/${yearId}/courses/${courseId}`);
        return response.data;
    },
    getHierarchyTree: async () => {
        const response = await axios.get(`${API_BASE_URL}/AcademicHierarchy/tree`);
        return response.data;
    },

    // Inline course management within Academic Structure
    updateCourse: async (courseId: number, data: { courseCode?: string; courseTitle?: string }) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.put(`${API_BASE_URL}/AcademicHierarchy/courses/${courseId}`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },
    addCourseToYear: async (yearId: number, data?: { courseCode?: string; courseTitle?: string }) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.post(`${API_BASE_URL}/AcademicHierarchy/years/${yearId}/courses`, data || {}, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },
    deleteCourseFromYear: async (courseId: number) => {
        const token = localStorage.getItem('adminToken');
        const response = await axios.delete(`${API_BASE_URL}/AcademicHierarchy/courses/${courseId}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    // Course Videos
    getCourseVideos: async (courseId: number) => {
        const response = await axios.get(`${API_BASE_URL}/CourseVideos/course/${courseId}`);
        return response.data;
    },
    addCourseVideo: async (courseId: number, data: { title: string; description: string; videoUrl: string }) => {
        const response = await axios.post(`${API_BASE_URL}/CourseVideos/course/${courseId}`, data);
        return response.data;
    },
    deleteCourseVideo: async (videoId: number) => {
        const response = await axios.delete(`${API_BASE_URL}/CourseVideos/${videoId}`);
        return response.data;
    },

    // Wasabi Video Upload
    getUploadUrl: async (fileName: string, contentType: string, courseId: number) => {
        const response = await axios.post(`${API_BASE_URL}/CourseVideos/upload-url`, {
            fileName,
            contentType,
            courseId
        });
        return response.data;
    },
    confirmVideoUpload: async (courseId: number, data: {
        title: string;
        description?: string;
        objectKey: string;
        originalFileName: string;
        fileSizeBytes: number;
    }) => {
        const response = await axios.post(`${API_BASE_URL}/CourseVideos/course/${courseId}/confirm`, data);
        return response.data;
    },
    getVideoStreamUrl: async (videoId: number) => {
        const response = await axios.get(`${API_BASE_URL}/CourseVideos/${videoId}/stream-url`);
        return response.data;
    },
    getVideoDownloadUrl: async (videoId: number) => {
        const response = await axios.get(`${API_BASE_URL}/CourseVideos/${videoId}/download-url`);
        return response.data;
    }
};
