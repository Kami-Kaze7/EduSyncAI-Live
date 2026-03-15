// API Types
export interface Course {
  id: number;
  courseCode: string;
  courseName: string;
  courseTitle?: string;
  description?: string;
  lecturerId: number;
  creditHours: number;
  syllabusPath?: string;
  createdAt: string;
}

export interface Student {
  id: number;
  matricNumber: string;
  fullName: string;
  email: string;
  photoPath?: string;
}

export interface CourseEnrollment {
  id: number;
  courseId: number;
  studentId: number;
  enrolledAt: string;
  student?: Student;
}

export interface ClassSession {
  id: number;
  courseId: number;
  scheduledDate: string;
  topic?: string;
  location?: string;
  durationMinutes: number;
  startTime?: string;
  endTime?: string;
  status?: string;
  attendanceCount?: number;
  course?: Course;
  notes?: LectureNotes;
  materials?: LectureMaterial[];
}

export interface LectureNotes {
  id: number;
  sessionId: number;
  content: string;
  lastModified: string;
}

export interface LectureMaterial {
  id: number;
  sessionId: number;
  fileName: string;
  filePath: string;
  fileType: string;
  fileSize: number;
  uploadedAt: string;
}
