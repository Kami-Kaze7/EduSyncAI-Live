using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace EduSyncAI
{
    public class DatabaseService
    {
        private static readonly string DbPath;
        private static readonly string ConnString;

        static DatabaseService()
        {
            DbPath = Path.Combine(AppConfig.DataDir, "edusync.db");
            ConnString = "Data Source=" + DbPath;
            Console.WriteLine($"[DB] Using database: {DbPath}");
            Console.WriteLine($"[DB] File exists: {File.Exists(DbPath)}");
        }

        public DatabaseService()
        {
            InitializeDatabase();
        }

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnString);
        }

        private void InitializeDatabase()
        {
            var dataDir = Path.GetDirectoryName(DbPath)!;
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Students (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MatricNumber TEXT UNIQUE NOT NULL,
                FullName TEXT NOT NULL,
                Email TEXT,
                PhotoPath TEXT,
                WindowsUsername TEXT,
                PasswordHash TEXT,
                PIN TEXT,
                IsActive INTEGER DEFAULT 1,
                CreatedAt TEXT,
                Age INTEGER,
                Hobbies TEXT,
                Bio TEXT
            );

            CREATE TABLE IF NOT EXISTS Courses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseCode TEXT,
                CourseTitle TEXT,
                SyllabusPath TEXT
            );

            CREATE TABLE IF NOT EXISTS Lectures (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId INTEGER,
                LectureDate TEXT,
                Topic TEXT
            );

            CREATE TABLE IF NOT EXISTS LecturePreps (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LectureId INTEGER,
                CoreIdeas TEXT,
                KeyTerms TEXT,
                SimpleExample TEXT,
                WhatToListenFor TEXT,
                CreatedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS CourseEnrollments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId INTEGER,
                CourseId INTEGER,
                FOREIGN KEY (StudentId) REFERENCES Students(Id),
                FOREIGN KEY (CourseId) REFERENCES Courses(Id)
            );

            CREATE TABLE IF NOT EXISTS ClassSessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId INTEGER NOT NULL,
                LectureId INTEGER NOT NULL,
                LecturerId INTEGER,
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
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (CourseId) REFERENCES Courses(Id),
                FOREIGN KEY (LectureId) REFERENCES Lectures(Id)
            );

            CREATE TABLE IF NOT EXISTS Lecturers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT UNIQUE NOT NULL,
                Email TEXT UNIQUE NOT NULL,
                FullName TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                IsActive INTEGER DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Admins (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL,
                FullName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ClassSummaries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId INTEGER NOT NULL,
                LecturerId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Summary TEXT NOT NULL,
                KeyTopics TEXT,
                PreparationNotes TEXT,
                ClassDate TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (CourseId) REFERENCES Courses(Id),
                FOREIGN KEY (LecturerId) REFERENCES Lecturers(Id)
            );

            CREATE TABLE IF NOT EXISTS CourseSyllabi (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId INTEGER NOT NULL,
                LecturerId INTEGER NOT NULL,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                FileType TEXT NOT NULL,
                ExtractedText TEXT,
                TotalWeeks INTEGER,
                UploadedAt TEXT NOT NULL,
                FOREIGN KEY (CourseId) REFERENCES Courses(Id),
                FOREIGN KEY (LecturerId) REFERENCES Lecturers(Id)
            );

            CREATE TABLE IF NOT EXISTS WeeklySummaries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SyllabusId INTEGER NOT NULL,
                CourseId INTEGER NOT NULL,
                LecturerId INTEGER NOT NULL,
                WeekNumber INTEGER NOT NULL,
                WeekTitle TEXT,
                Summary TEXT NOT NULL,
                KeyTopics TEXT,
                LearningObjectives TEXT,
                PreparationNotes TEXT,
                GeneratedAt TEXT NOT NULL,
                SentToStudents INTEGER DEFAULT 0,
                SentAt TEXT,
                FOREIGN KEY (SyllabusId) REFERENCES CourseSyllabi(Id),
                FOREIGN KEY (CourseId) REFERENCES Courses(Id),
                FOREIGN KEY (LecturerId) REFERENCES Lecturers(Id)
            );

            CREATE TABLE IF NOT EXISTS Attendance (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionId INTEGER NOT NULL,
                StudentId INTEGER NOT NULL,
                CheckInTime TEXT NOT NULL,
                CheckInMethod TEXT NOT NULL,
                VerifiedBy INTEGER,
                FOREIGN KEY (SessionId) REFERENCES ClassSessions(Id),
                FOREIGN KEY (StudentId) REFERENCES Students(Id),
                FOREIGN KEY (VerifiedBy) REFERENCES Lecturers(Id),
                UNIQUE(SessionId, StudentId)
            );

            CREATE TABLE IF NOT EXISTS ModelAssets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                Discipline TEXT NOT NULL,
                ModelUrl TEXT NOT NULL,
                ThumbnailUrl TEXT,
                UploadedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";

            command.ExecuteNonQuery();

            // Patch existing databases that were created before the UploadedAt column was added
            try {
                var patchCommand = connection.CreateCommand();
                patchCommand.CommandText = "ALTER TABLE ModelAssets ADD COLUMN UploadedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP;";
                patchCommand.ExecuteNonQuery();
            } catch { /* Ignore if column exists */ }
        }

        public int CreateCourse(Course course)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Courses (CourseCode, CourseTitle, SyllabusPath)
            VALUES (@code, @title, @path);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@code", course.CourseCode);
            command.Parameters.AddWithValue("@title", course.CourseTitle);
            command.Parameters.AddWithValue("@path", course.SyllabusPath ?? "");
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Inserts or updates a course with a specific ID (to sync remote courses into local DB).
        /// Uses INSERT OR REPLACE to preserve the remote server's ID.
        /// </summary>
        public void UpsertCourseWithId(Course course)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT OR REPLACE INTO Courses (Id, CourseCode, CourseTitle, SyllabusPath)
            VALUES (@id, @code, @title, @path);
        ";
            command.Parameters.AddWithValue("@id", course.Id);
            command.Parameters.AddWithValue("@code", course.CourseCode ?? "");
            command.Parameters.AddWithValue("@title", course.CourseTitle ?? "");
            command.Parameters.AddWithValue("@path", course.SyllabusPath ?? "");
            command.ExecuteNonQuery();
        }

        public List<Course> GetAllCourses()
        {
            var courses = new List<Course>();
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Courses";
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                courses.Add(new Course
                {
                    Id = reader.GetInt32(0),
                    CourseCode = reader.GetString(1),
                    CourseTitle = reader.GetString(2),
                    SyllabusPath = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return courses;
        }

        public List<Course> GetEnrolledCourses(int studentId)
        {
            var courses = new List<Course>();
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT c.* FROM Courses c
                INNER JOIN CourseEnrollments ce ON c.Id = ce.CourseId
                WHERE ce.StudentId = @studentId
                ORDER BY c.CourseTitle";
            command.Parameters.AddWithValue("@studentId", studentId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                courses.Add(new Course
                {
                    Id = reader.GetInt32(0),
                    CourseCode = reader.GetString(1),
                    CourseTitle = reader.GetString(2),
                    SyllabusPath = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return courses;
        }

        public int CreateLecture(Lecture lecture)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Lectures (CourseId, LectureDate, Topic)
            VALUES (@courseId, @date, @topic);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@courseId", lecture.CourseId);
            command.Parameters.AddWithValue("@date", lecture.LectureDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@topic", lecture.Topic);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public List<Lecture> GetLecturesByCourse(int courseId)
        {
            var lectures = new List<Lecture>();
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Lectures WHERE CourseId = @courseId ORDER BY LectureDate";
            command.Parameters.AddWithValue("@courseId", courseId);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lectures.Add(new Lecture
                {
                    Id = reader.GetInt32(0),
                    CourseId = reader.GetInt32(1),
                    LectureDate = DateTime.Parse(reader.GetString(2)),
                    Topic = reader.GetString(3)
                });
            }
            return lectures;
        }

        public int SaveLecturePrep(LecturePrep prep)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO LecturePreps (LectureId, CoreIdeas, KeyTerms, SimpleExample, WhatToListenFor, CreatedAt)
            VALUES (@lectureId, @coreIdeas, @keyTerms, @example, @listenFor, @createdAt);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@lectureId", prep.LectureId);
            command.Parameters.AddWithValue("@coreIdeas", prep.CoreIdeas);
            command.Parameters.AddWithValue("@keyTerms", prep.KeyTerms);
            command.Parameters.AddWithValue("@example", prep.SimpleExample);
            command.Parameters.AddWithValue("@listenFor", prep.WhatToListenFor);
            command.Parameters.AddWithValue("@createdAt", prep.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public List<LecturePrep> GetLecturePrepsByStudent(int studentId)
        {
            var preps = new List<LecturePrep>();
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT lp.* FROM LecturePreps lp
            INNER JOIN Lectures l ON lp.LectureId = l.Id
            INNER JOIN CourseEnrollments ce ON l.CourseId = ce.CourseId
            WHERE ce.StudentId = @studentId
            ORDER BY lp.CreatedAt DESC
        ";
            command.Parameters.AddWithValue("@studentId", studentId);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                preps.Add(new LecturePrep
                {
                    Id = reader.GetInt32(0),
                    LectureId = reader.GetInt32(1),
                    CoreIdeas = reader.GetString(2),
                    KeyTerms = reader.GetString(3),
                    SimpleExample = reader.GetString(4),
                    WhatToListenFor = reader.GetString(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6))
                });
            }
            return preps;
        }

        public List<Student> GetStudentsByCourse(int courseId)
        {
            var students = new List<Student>();
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT s.* FROM Students s
            INNER JOIN CourseEnrollments ce ON s.Id = ce.StudentId
            WHERE ce.CourseId = @courseId
        ";
            command.Parameters.AddWithValue("@courseId", courseId);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                students.Add(MapStudent(reader));
            }
            return students;
        }

        public List<Student> GetAllStudents()
        {
            var students = new List<Student>();
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Students ORDER BY FullName";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                students.Add(MapStudent(reader));
            }
            return students;
        }

        public int CreateStudent(Student student)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Students (MatricNumber, FullName, Email, WindowsUsername, PasswordHash, PIN, IsActive, CreatedAt)
                VALUES (@matric, @name, @email, @windowsUser, @passwordHash, @pin, @isActive, @createdAt);
                SELECT last_insert_rowid();
            ";
            command.Parameters.AddWithValue("@matric", student.MatricNumber);
            command.Parameters.AddWithValue("@name", student.FullName);
            command.Parameters.AddWithValue("@email", student.Email);
            command.Parameters.AddWithValue("@windowsUser", student.WindowsUsername ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@passwordHash", student.PasswordHash ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@pin", student.PIN ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@isActive", student.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@createdAt", student.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public void UpdateStudentPhoto(int studentId, string photoPath)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Students SET PhotoPath = @photoPath WHERE Id = @id";
            command.Parameters.AddWithValue("@photoPath", photoPath);
            command.Parameters.AddWithValue("@id", studentId);
            command.ExecuteNonQuery();
        }

        public Student? GetStudentByMatricNumber(string matricNumber)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Students WHERE MatricNumber = @matric";
            command.Parameters.AddWithValue("@matric", matricNumber);
            
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapStudent(reader);
            }
            return null;
        }

        public Student? GetStudentByPIN(string pin)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Students WHERE PIN = @pin";
            command.Parameters.AddWithValue("@pin", pin);
            
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapStudent(reader);
            }
            return null;
        }

        public List<Student> GetEnrolledStudents(int courseId)
        {
            return GetStudentsByCourse(courseId);
        }

        public void EnrollStudent(int studentId, int courseId)
        {
            using var connection = GetConnection();
            connection.Open();
            
            // Check if already enrolled
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM CourseEnrollments WHERE StudentId = @studentId AND CourseId = @courseId";
            checkCommand.Parameters.AddWithValue("@studentId", studentId);
            checkCommand.Parameters.AddWithValue("@courseId", courseId);
            
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());
            if (count > 0)
            {
                throw new InvalidOperationException("Student is already enrolled in this course");
            }
            
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO CourseEnrollments (StudentId, CourseId) VALUES (@studentId, @courseId)";
            command.Parameters.AddWithValue("@studentId", studentId);
            command.Parameters.AddWithValue("@courseId", courseId);
            command.ExecuteNonQuery();
        }

        public void UnenrollStudent(int studentId, int courseId)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CourseEnrollments WHERE StudentId = @studentId AND CourseId = @courseId";
            command.Parameters.AddWithValue("@studentId", studentId);
            command.Parameters.AddWithValue("@courseId", courseId);
            command.ExecuteNonQuery();
        }

        public bool IsStudentEnrolled(int studentId, int courseId)
        {
            using var connection = GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM CourseEnrollments WHERE StudentId = @studentId AND CourseId = @courseId";
            command.Parameters.AddWithValue("@studentId", studentId);
            command.Parameters.AddWithValue("@courseId", courseId);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        // ==================== CLASS SESSION METHODS ====================

        public int CreateClassSession(ClassSession session)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ClassSessions (CourseId, LectureId, LecturerId, SessionCode, SessionState, AttendanceCount, Duration, CreatedAt)
            VALUES (@courseId, @lectureId, @lecturerId, @sessionCode, @state, @attendanceCount, @duration, @createdAt);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("@courseId", session.CourseId);
        command.Parameters.AddWithValue("@lectureId", session.LectureId);
        command.Parameters.AddWithValue("@lecturerId", session.LecturerId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@sessionCode", session.SessionCode ?? "");
        command.Parameters.AddWithValue("@state", session.State.ToString());
        command.Parameters.AddWithValue("@attendanceCount", session.AttendanceCount);
        command.Parameters.AddWithValue("@duration", session.Duration);
        command.Parameters.AddWithValue("@createdAt", session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void UpdateClassSession(ClassSession session)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE ClassSessions SET
                SessionState = @state,
                StartTime = @startTime,
                EndTime = @endTime,
                AudioFilePath = @audioPath,
                VideoFilePath = @videoPath,
                BoardExportPath = @boardPath,
                BoardSnapshotFolder = @snapshotFolder,
                AttendanceCount = @attendanceCount,
                Duration = @duration
            WHERE Id = @id
        ";
        command.Parameters.AddWithValue("@id", session.Id);
        command.Parameters.AddWithValue("@state", session.State.ToString());
        command.Parameters.AddWithValue("@startTime", session.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@endTime", session.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@audioPath", session.AudioFilePath ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@videoPath", session.VideoFilePath ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@boardPath", session.BoardExportPath ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@snapshotFolder", session.BoardSnapshotFolder ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@attendanceCount", session.AttendanceCount);
        command.Parameters.AddWithValue("@duration", session.Duration);
        command.ExecuteNonQuery();
    }

    public ClassSession GetClassSessionById(int id)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT s.*, c.CourseTitle, l.Topic
            FROM ClassSessions s
            LEFT JOIN Courses c ON s.CourseId = c.Id
            LEFT JOIN Lectures l ON s.LectureId = l.Id
            WHERE s.Id = @id
        ";
        command.Parameters.AddWithValue("@id", id);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapClassSession(reader);
        }
        return null;
    }

    public List<ClassSession> GetAllClassSessions()
    {
        var sessions = new List<ClassSession>();
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT s.*, c.CourseTitle, l.Topic
            FROM ClassSessions s
            LEFT JOIN Courses c ON s.CourseId = c.Id
            LEFT JOIN Lectures l ON s.LectureId = l.Id
            ORDER BY s.CreatedAt DESC
        ";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(MapClassSession(reader));
        }
        return sessions;
    }

    public ClassSession GetActiveClassSession()
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT s.*, c.CourseTitle, l.Topic
            FROM ClassSessions s
            LEFT JOIN Courses c ON s.CourseId = c.Id
            LEFT JOIN Lectures l ON s.LectureId = l.Id
            WHERE s.SessionState = 'Live'
            LIMIT 1
        ";
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapClassSession(reader);
        }
        return null;
    }

    private ClassSession MapClassSession(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new ClassSession
        {
            Id = reader.GetInt32(0),
            CourseId = reader.GetInt32(1),
            LectureId = reader.GetInt32(2),
            LecturerId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            SessionCode = reader.IsDBNull(4) ? null : reader.GetString(4),
            State = Enum.Parse<SessionState>(reader.GetString(5)),
            StartTime = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
            EndTime = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
            AudioFilePath = reader.IsDBNull(8) ? null : reader.GetString(8),
            VideoFilePath = reader.IsDBNull(9) ? null : reader.GetString(9),
            BoardExportPath = reader.IsDBNull(10) ? null : reader.GetString(10),
            BoardSnapshotFolder = reader.IsDBNull(11) ? null : reader.GetString(11),
            AttendanceCount = reader.GetInt32(12),
            Duration = reader.GetInt32(13),
            CreatedAt = DateTime.Parse(reader.GetString(14)),
            CourseName = reader.IsDBNull(15) ? "" : reader.GetString(15),
            LectureTopic = reader.IsDBNull(16) ? "" : reader.GetString(16)
        };
    }

    // ==================== LECTURER METHODS ====================

    public int CreateLecturer(Lecturer lecturer)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Lecturers (Username, Email, FullName, PasswordHash, IsActive)
            VALUES (@username, @email, @fullName, @passwordHash, @isActive);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("@username", lecturer.Username);
        command.Parameters.AddWithValue("@email", lecturer.Email);
        command.Parameters.AddWithValue("@fullName", lecturer.FullName);
        command.Parameters.AddWithValue("@passwordHash", lecturer.PasswordHash);
        command.Parameters.AddWithValue("@isActive", lecturer.IsActive ? 1 : 0);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Lecturer GetLecturerByUsername(string username)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Lecturers WHERE Username = @username AND IsActive = 1";
        command.Parameters.AddWithValue("@username", username);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapLecturer(reader);
        }
        return null;
    }

    public Lecturer GetLecturerByPIN(string pin)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Lecturers WHERE PIN = @pin AND IsActive = 1";
        command.Parameters.AddWithValue("@pin", pin);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapLecturer(reader);
        }
        return null;
    }

    public Lecturer GetLecturerById(int id)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Lecturers WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapLecturer(reader);
        }
        return null;
    }

    private Lecturer MapLecturer(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new Lecturer
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            PIN = null,
            IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1
        };
    }

    // ==================== ATTENDANCE METHODS ====================

    public int MarkAttendance(AttendanceRecord attendance)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Attendance (SessionId, StudentId, CheckInTime, CheckInMethod, VerifiedBy)
            VALUES (@sessionId, @studentId, @checkInTime, @method, @verifiedBy);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("@sessionId", attendance.SessionId);
        command.Parameters.AddWithValue("@studentId", attendance.StudentId);
        command.Parameters.AddWithValue("@checkInTime", attendance.CheckInTime.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@method", attendance.CheckInMethod);
        command.Parameters.AddWithValue("@verifiedBy", attendance.VerifiedBy ?? (object)DBNull.Value);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<AttendanceRecord> GetSessionAttendance(int sessionId)
    {
        var records = new List<AttendanceRecord>();
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT a.*, s.FullName, s.MatricNumber
            FROM Attendance a
            LEFT JOIN Students s ON a.StudentId = s.Id
            WHERE a.SessionId = @sessionId
            ORDER BY a.CheckInTime
        ";
        command.Parameters.AddWithValue("@sessionId", sessionId);
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new AttendanceRecord
            {
                Id = reader.GetInt32(0),
                SessionId = reader.GetInt32(1),
                StudentId = reader.GetInt32(2),
                CheckInTime = DateTime.Parse(reader.GetString(3)),
                CheckInMethod = reader.GetString(4),
                VerifiedBy = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                StudentName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                MatricNumber = reader.IsDBNull(7) ? "" : reader.GetString(7)
            });
        }
        return records;
    }

    public bool IsStudentPresent(int sessionId, int studentId)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Attendance WHERE SessionId = @sessionId AND StudentId = @studentId";
        command.Parameters.AddWithValue("@sessionId", sessionId);
        command.Parameters.AddWithValue("@studentId", studentId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public int GetAttendanceCount(int sessionId)
    {
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Attendance WHERE SessionId = @sessionId";
        command.Parameters.AddWithValue("@sessionId", sessionId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<Student> GetAbsentStudents(int sessionId, int courseId)
    {
        var absentStudents = new List<Student>();
        using var connection = GetConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT s.*
            FROM Students s
            INNER JOIN CourseEnrollments ce ON s.Id = ce.StudentId
            WHERE ce.CourseId = @courseId
            AND s.Id NOT IN (
                SELECT StudentId FROM Attendance WHERE SessionId = @sessionId
            )
            ORDER BY s.FullName
        ";
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@sessionId", sessionId);
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            absentStudents.Add(MapStudent(reader));
        }
        return absentStudents;
    }

    private Student MapStudent(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new Student
        {
            Id = GetSafeInt(reader, "Id"),
            MatricNumber = GetSafeString(reader, "MatricNumber"),
            FullName = GetSafeString(reader, "FullName"),
            Email = GetSafeString(reader, "Email"),
            PhotoPath = GetSafeString(reader, "PhotoPath"),
            WindowsUsername = GetSafeString(reader, "WindowsUsername"),
            PasswordHash = GetSafeString(reader, "PasswordHash"),
            PIN = GetSafeString(reader, "PIN"),
            IsActive = GetSafeInt(reader, "IsActive") == 1,
            CreatedAt = DateTime.TryParse(GetSafeString(reader, "CreatedAt"), out var date) ? date : DateTime.Now
        };
    }

    private string GetSafeString(Microsoft.Data.Sqlite.SqliteDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
        }
        catch { return ""; }
    }

    private int GetSafeInt(Microsoft.Data.Sqlite.SqliteDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }
        catch { return 0; }
    }
    }
}
