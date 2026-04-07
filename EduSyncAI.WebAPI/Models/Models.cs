using Microsoft.EntityFrameworkCore;

namespace EduSyncAI.WebAPI.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string? SyllabusPath { get; set; }
        
        public int? YearOfStudyId { get; set; }
        public virtual YearOfStudy? YearOfStudy { get; set; }
        
        // API compatibility properties
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string CourseName 
        { 
            get => CourseTitle; 
            set => CourseTitle = value; 
        }
        
        public int LecturerId { get; set; } = 1;
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int CreditHours { get; set; } = 3;
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? Description { get; set; }
        
        // Navigation properties
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public virtual ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public virtual ICollection<ClassSession> Sessions { get; set; } = new List<ClassSession>();
    }

    public class CourseEnrollment
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Course? Course { get; set; }
        public virtual Student? Student { get; set; }
    }

    public class Student
    {
        public int Id { get; set; }
        public string MatricNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int? Age { get; set; }
        public string? Hobbies { get; set; }
        public string? Bio { get; set; }
        
        public int? YearOfStudyId { get; set; }
        public virtual YearOfStudy? YearOfStudy { get; set; }
    }

    public class ClassSession
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int LectureId { get; set; }
        public int? LecturerId { get; set; }
        public string? SessionCode { get; set; }
        public string SessionState { get; set; } = "Scheduled";
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? AudioFilePath { get; set; }
        public string? VideoFilePath { get; set; }
        public string? BoardExportPath { get; set; }
        public string? BoardSnapshotFolder { get; set; }
        public int AttendanceCount { get; set; } = 0;
        public int Duration { get; set; } = 0;
        public string CreatedAt { get; set; } = string.Empty;
        
        // API compatibility properties
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DateTime ScheduledDate 
        { 
            get => string.IsNullOrEmpty(StartTime) ? DateTime.UtcNow : DateTime.Parse(StartTime);
            set => StartTime = value.ToString("O");
        }
        
        public string? Topic { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? Location { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int DurationMinutes 
        { 
            get => Duration; 
            set => Duration = value; 
        }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Status 
        { 
            get => SessionState; 
            set => SessionState = value; 
        }
        
        // Navigation properties
        public virtual Course? Course { get; set; }
        
        public virtual LectureNotes? Notes { get; set; }
        
        public virtual ICollection<LectureMaterial> Materials { get; set; } = new List<LectureMaterial>();
    }

    public class LectureNotes
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        
        // Navigation property
        public virtual ClassSession? Session { get; set; }
    }

    public class LectureMaterial
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        
        // Navigation property
        public virtual ClassSession? Session { get; set; }
    }

    public class Lecturer
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class Admin
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ClassSummary
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int LecturerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? KeyTopics { get; set; }
        public string? PreparationNotes { get; set; }
        public DateTime ClassDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CourseSyllabus
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int LecturerId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // 'pdf', 'docx', 'txt'
        public string? ExtractedText { get; set; }
        public int? TotalWeeks { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    public class WeeklySummary
    {
        public int Id { get; set; }
        public int SyllabusId { get; set; }
        public int CourseId { get; set; }
        public int LecturerId { get; set; }
        public int WeekNumber { get; set; }
        public int DayNumber { get; set; } = 1; // 1=Day1, 2=Day2, 3=Day3 within the week
        public string? WeekTitle { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? KeyTopics { get; set; } // JSON array
        public string? LearningObjectives { get; set; } // JSON array
        public string? PreparationNotes { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public bool SentToStudents { get; set; } = false;
        public DateTime? SentAt { get; set; }
    }

    public class StudentWeeklySummary
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int WeeklySummaryId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual WeeklySummary? WeeklySummary { get; set; }
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int StudentId { get; set; }
        public DateTime CheckInTime { get; set; }
        public string CheckInMethod { get; set; } = string.Empty;
        public int? VerifiedBy { get; set; }

        // API compatibility properties
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string StudentName { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string MatricNumber { get; set; } = string.Empty;
    }

    public class Faculty
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
    }

    public class Department
    {
        public int Id { get; set; }
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public virtual Faculty? Faculty { get; set; }
        public virtual ICollection<YearOfStudy> YearsOfStudy { get; set; } = new List<YearOfStudy>();
    }

    public class YearOfStudy
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        
        public virtual Department? Department { get; set; }
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    public class CourseVideo
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public int OrderIndex { get; set; } = 0;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        public virtual Course? Course { get; set; }
    }
}
