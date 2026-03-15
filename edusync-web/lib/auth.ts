import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5152/api';

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

export interface LoginCredentials {
    username: string;
    password: string;
}

export interface LecturerInfo {
    id: number;
    username: string;
    fullName: string;
    email: string;
    department?: string;
}

export interface LoginResponse {
    token: string;
    user: LecturerInfo;
}

export const authApi = {
    login: async (credentials: LoginCredentials) => {
        const { data } = await api.post<LoginResponse>('/auth/login', credentials);
        // Store token in localStorage
        if (typeof window !== 'undefined') {
            localStorage.setItem('auth_token', data.token);
        }
        return data.user;
    },

    logout: () => {
        if (typeof window !== 'undefined') {
            localStorage.removeItem('auth_token');
        }
    },

    getToken: () => {
        if (typeof window !== 'undefined') {
            return localStorage.getItem('auth_token');
        }
        return null;
    },

    register: async (data: {
        username: string;
        password: string;
        fullName: string;
        email: string;
        department?: string;
    }) => {
        const response = await api.post('/auth/register', data);
        return response.data;
    },
};

export default api;
