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
        public DbSet<Model3DAsset> ModelAssets { get; set; }
        
        // Academic Hierarchy
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<YearOfStudy> YearsOfStudy { get; set; }
        public DbSet<CourseVideo> CourseVideos { get; set; }

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
            modelBuilder.Entity<Model3DAsset>().ToTable("ModelAssets");

            // Ignore navigation properties to prevent EF from trying to load them
            modelBuilder.Entity<Course>().Ignore(c => c.Enrollments);
            modelBuilder.Entity<Course>().Ignore(c => c.Sessions);
            
            // New Hierarchy Entities
            modelBuilder.Entity<Faculty>().ToTable("Faculties");
            modelBuilder.Entity<Department>().ToTable("Departments");
            modelBuilder.Entity<YearOfStudy>().ToTable("YearsOfStudy");
            modelBuilder.Entity<CourseVideo>().ToTable("CourseVideos");
            
            // Explicit Foreign Keys
            modelBuilder.Entity<YearOfStudy>()
                .HasMany(y => y.Courses)
                .WithOne(c => c.YearOfStudy)
                .HasForeignKey(c => c.YearOfStudyId)
                .IsRequired(false);
                
            modelBuilder.Entity<YearOfStudy>()
                .HasMany<Student>()
                .WithOne(s => s.YearOfStudy)
                .HasForeignKey(s => s.YearOfStudyId)
                .IsRequired(false);
        }
    }
}
