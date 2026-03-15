using System;
using Microsoft.Data.Sqlite;

var dbPath = @"C:\EduSyncAI\Data\edusync.db";
var connectionString = $"Data Source={dbPath}";

System.Console.WriteLine("=== Creating Missing Database Tables ===");
System.Console.WriteLine($"Database: {dbPath}");
System.Console.WriteLine();

using var connection = new SqliteConnection(connectionString);
connection.Open();

// Create CourseSyllabi table
var sql1 = @"CREATE TABLE IF NOT EXISTS CourseSyllabi (
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
);";

using (var cmd = new SqliteCommand(sql1, connection))
{
    cmd.ExecuteNonQuery();
    System.Console.WriteLine("✓ CourseSyllabi table created");
}

// Create WeeklySummaries table
var sql2 = @"CREATE TABLE IF NOT EXISTS WeeklySummaries (
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
);";

using (var cmd = new SqliteCommand(sql2, connection))
{
    cmd.ExecuteNonQuery();
    System.Console.WriteLine("✓ WeeklySummaries table created");
}

System.Console.WriteLine();
System.Console.WriteLine("✅ Tables created successfully!");
System.Console.WriteLine("You can now upload syllabi.");
