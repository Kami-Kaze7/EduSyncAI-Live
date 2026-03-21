using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EduSyncAI.WebAPI
{
    public class DatabaseSeeder
    {
        public static void SeedTestUser(EduSyncDbContext context)
        {
            // First ensure all tables exist (EnsureCreated won't add tables to existing DB)
            EnsureTablesCreated(context);

            // Check if test user already exists
            var testUser = context.Lecturers.FirstOrDefault(l => l.Username == "testlecturer");
            
            if (testUser == null)
            {
                // Create test lecturer
                var lecturer = new Lecturer
                {
                    Username = "testlecturer",
                    FullName = "Test Lecturer",
                    Email = "test@edusync.ai",
                    PasswordHash = HashPassword("password123"),
                    IsActive = true
                };

                context.Lecturers.Add(lecturer);
                context.SaveChanges();

                Console.WriteLine("✅ Test lecturer created:");
                Console.WriteLine($"   Username: testlecturer");
                Console.WriteLine($"   Password: password123");
            }
            
            // Check if admin already exists
            var admin = context.Admins.FirstOrDefault(a => a.Username == "admin");
            
            if (admin == null)
            {
                // Create default admin
                var defaultAdmin = new Admin
                {
                    Username = "admin",
                    FullName = "System Administrator",
                    PasswordHash = HashPassword("admin123"),
                    CreatedAt = DateTime.UtcNow
                };

                context.Admins.Add(defaultAdmin);
                context.SaveChanges();

                Console.WriteLine("✅ Admin user created:");
                Console.WriteLine($"   Username: admin");
                Console.WriteLine($"   Password: admin123");
            }

            // Check if test student already exists
            var testStudent = context.Students.FirstOrDefault(s => s.MatricNumber == "2017/123456");

            if (testStudent == null)
            {
                var student = new Student
                {
                    MatricNumber = "2017/123456",
                    FullName = "Test Student",
                    Email = "student@edusync.ai",
                    PasswordHash = HashPassword("password123"),
                    IsActive = true
                };

                context.Students.Add(student);
                context.SaveChanges();

                Console.WriteLine("✅ Test student created:");
                Console.WriteLine($"   Matric Number: 2017/123456");
                Console.WriteLine($"   Password: password123");
            }
            else
            {
                // Ensure password hash matches web API format (SHA256 base64)
                var expectedHash = HashPassword("password123");
                if (testStudent.PasswordHash != expectedHash)
                {
                    testStudent.PasswordHash = expectedHash;
                    testStudent.IsActive = true;
                    context.SaveChanges();
                    Console.WriteLine("✅ Test student password updated to web format");
                }
            }
        }

        private static void EnsureTablesCreated(EduSyncDbContext context)
        {
            var conn = context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) conn.Open();

            try
            {
                using var cmd = conn.CreateCommand();
                
                // 1. Admins Table
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Admins (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        FullName TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );";
                cmd.ExecuteNonQuery();

                // 2. Lecturers Table (ensure it exists)
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Lecturers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL,
                        FullName TEXT NOT NULL,
                        Email TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );";
                cmd.ExecuteNonQuery();

                // 3. Students Table (ensure it exists)
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Students (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MatricNumber TEXT NOT NULL,
                        FullName TEXT NOT NULL,
                        Email TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        PhotoPath TEXT,
                        Age INTEGER,
                        Hobbies TEXT,
                        Bio TEXT
                    );";
                cmd.ExecuteNonQuery();

                // 3b. Migrate existing Students table: add missing profile columns
                // SQLite does not support "ADD COLUMN IF NOT EXISTS", so we catch the error if column already exists
                var studentColumnsToAdd = new[]
                {
                    ("PhotoPath", "TEXT"),
                    ("Age", "INTEGER"),
                    ("Hobbies", "TEXT"),
                    ("Bio", "TEXT"),
                    ("PasswordHash", "TEXT NOT NULL DEFAULT ''"),
                    ("IsActive", "INTEGER NOT NULL DEFAULT 1"),
                };
                foreach (var (colName, colType) in studentColumnsToAdd)
                {
                    try
                    {
                        cmd.CommandText = $"ALTER TABLE Students ADD COLUMN {colName} {colType};";
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"  ✅ Added column Students.{colName}");
                    }
                    catch
                    {
                        // Column already exists — safe to ignore
                    }
                }

                // 4. ClassSummaries Table
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ClassSummaries (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseId INTEGER NOT NULL,
                        LecturerId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        Summary TEXT NOT NULL,
                        KeyTopics TEXT,
                        PreparationNotes TEXT,
                        ClassDate TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );";
                cmd.ExecuteNonQuery();

                // 6. Courses Table (ensure it exists and has LecturerId)
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Courses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseCode TEXT NOT NULL,
                        CourseTitle TEXT NOT NULL,
                        LecturerId INTEGER NOT NULL DEFAULT 1,
                        SyllabusPath TEXT,
                        CreatedAt TEXT
                    );";
                cmd.ExecuteNonQuery();

                try
                {
                    cmd.CommandText = "ALTER TABLE Courses ADD COLUMN LecturerId INTEGER NOT NULL DEFAULT 1;";
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("  ✅ Added column Courses.LecturerId");
                }
                catch { /* Column already exists */ }

                // 7. ClassSessions Table
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ClassSessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseId INTEGER NOT NULL,
                        LectureId INTEGER NOT NULL,
                        LecturerId INTEGER,
                        Topic TEXT,
                        SessionCode TEXT,
                        SessionState TEXT NOT NULL,
                        StartTime TEXT,
                        EndTime TEXT,
                        AudioFilePath TEXT,
                        VideoFilePath TEXT,
                        BoardExportPath TEXT,
                        BoardSnapshotFolder TEXT,
                        AttendanceCount INTEGER DEFAULT 0,
                        Duration INTEGER DEFAULT 0,
                        CreatedAt TEXT NOT NULL
                    );";
                cmd.ExecuteNonQuery();

                try
                {
                    cmd.CommandText = "ALTER TABLE ClassSessions ADD COLUMN Topic TEXT;";
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("  ✅ Added column ClassSessions.Topic");
                }
                catch { /* Column already exists */ }

                // 8. Attendance Table
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Attendance (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SessionId INTEGER NOT NULL,
                        StudentId INTEGER NOT NULL,
                        CheckInTime TEXT NOT NULL,
                        CheckInMethod TEXT NOT NULL,
                        VerifiedBy INTEGER,
                        UNIQUE(SessionId, StudentId)
                    );";
                cmd.ExecuteNonQuery();

                // 9. CourseEnrollments Table
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CourseEnrollments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseId INTEGER NOT NULL,
                        StudentId INTEGER NOT NULL,
                        EnrolledAt TEXT NOT NULL,
                        UNIQUE(CourseId, StudentId)
                    );";
                cmd.ExecuteNonQuery();

                // 10. LectureMaterials Table
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS LectureMaterials (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SessionId INTEGER NOT NULL,
                        FileName TEXT NOT NULL,
                        FilePath TEXT NOT NULL,
                        FileType TEXT,
                        FileSize INTEGER DEFAULT 0,
                        UploadedAt TEXT NOT NULL
                    );";
                cmd.ExecuteNonQuery();

                // Migrate Courses table: add missing columns
                var courseColumnsToAdd = new[]
                {
                    ("CreatedAt", "TEXT"),
                    ("CreditHours", "INTEGER NOT NULL DEFAULT 3"),
                    ("CourseName", "TEXT"),
                };
                foreach (var (colName, colType) in courseColumnsToAdd)
                {
                    try
                    {
                        cmd.CommandText = $"ALTER TABLE Courses ADD COLUMN {colName} {colType};";
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"  ✅ Added column Courses.{colName}");
                    }
                    catch
                    {
                        // Column already exists — safe to ignore
                    }
                }

                Console.WriteLine("🔍 Database schema verification complete (checked all tables including CourseEnrollments and LectureMaterials).");
            }
            finally
            {
                if (!wasOpen) conn.Close();
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
