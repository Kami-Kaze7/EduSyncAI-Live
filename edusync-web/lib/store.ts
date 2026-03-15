import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface LecturerInfo {
    id: number;
    username: string;
    fullName: string;
    email: string;
    department?: string;
}

interface AuthStore {
    lecturer: LecturerInfo | null;
    isAuthenticated: boolean;
    login: (lecturer: LecturerInfo) => void;
    logout: () => void;
}

export const useAuthStore = create<AuthStore>()(
    persist(
        (set) => ({
            lecturer: null,
            isAuthenticated: false,
            login: (lecturer) => set({ lecturer, isAuthenticated: true }),
            logout: () => {
                if (typeof window !== 'undefined') {
                    localStorage.removeItem('auth_token');
                }
                set({ lecturer: null, isAuthenticated: false });
            },
        }),
        {
            name: 'auth-storage',
        }
    )
);
