using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EduSyncAI.WebAPI.Models;

namespace EduSyncAI.WebAPI.Data
{
    public class EduSyncDbContext : DbContext
    {
        public EduSyncDbContext(DbContextOptions<EduSyncDbContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<ClassSession> ClassSessions { get; set; }
        public DbSet<LectureNotes> LectureNotes { get; set; }
        public DbSet<LectureMaterial> LectureMaterials { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<ClassSummary> ClassSummaries { get; set; }
        public DbSet<CourseSyllabus> CourseSyllabi { get; set; }
        public DbSet<WeeklySummary> WeeklySummaries { get; set; }
        public DbSet<StudentWeeklySummary> StudentWeeklySummaries { get; set; }
        public DbSet<AttendanceRecord> Attendance { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match existing database
            modelBuilder.Entity<Course>().ToTable("Courses");
            modelBuilder.Entity<CourseEnrollment>().ToTable("CourseEnrollments");
            modelBuilder.Entity<Student>().ToTable("Students");
            modelBuilder.Entity<ClassSession>().ToTable("ClassSessions");
            modelBuilder.Entity<LectureNotes>().ToTable("LectureNotes");
            modelBuilder.Entity<LectureMaterial>().ToTable("LectureMaterials");
            modelBuilder.Entity<Lecturer>().ToTable("Lecturers");
            modelBuilder.Entity<Admin>().ToTable("Admins");
            modelBuilder.Entity<ClassSummary>().ToTable("ClassSummaries");
            modelBuilder.Entity<CourseSyllabus>().ToTable("CourseSyllabi");
            modelBuilder.Entity<WeeklySummary>().ToTable("WeeklySummaries");
            modelBuilder.Entity<StudentWeeklySummary>().ToTable("StudentWeeklySummaries");
            modelBuilder.Entity<AttendanceRecord>().ToTable("Attendance");
            
            // Ignore navigation properties to prevent EF from trying to load them
            modelBuilder.Entity<Course>().Ignore(c => c.Enrollments);
            modelBuilder.Entity<Course>().Ignore(c => c.Sessions);
        }
    }
}
