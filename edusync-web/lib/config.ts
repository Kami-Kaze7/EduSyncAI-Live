// Centralized API URL configuration
// In development: uses localhost:5152
// In production: uses NEXT_PUBLIC_API_URL environment variable

export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5152/api';

// Base URL without /api suffix (for direct file access like photos, material downloads)
export const API_SERVER_URL = process.env.NEXT_PUBLIC_API_URL
  ? process.env.NEXT_PUBLIC_API_URL.replace('/api', '')
  : 'http://localhost:5152';
