using System;
using System.Data.SQLite;
using System.IO;

namespace EduSyncAI.DatabaseMigration
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Data", "edusync.db");
            var connectionString = $"Data Source={dbPath};Version=3;";

            Console.WriteLine($"Database path: {dbPath}");
            Console.WriteLine("Creating missing tables...");

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Create CourseSyllabi table
                var createCourseSyllabi = @"
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
                    );";

                using (var cmd = new SQLiteCommand(createCourseSyllabi, connection))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("✓ CourseSyllabi table created");
                }

                // Create WeeklySummaries table
                var createWeeklySummaries = @"
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
                    );";

                using (var cmd = new SQLiteCommand(createWeeklySummaries, connection))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("✓ WeeklySummaries table created");
                }

                // Verify tables exist
                var verifyQuery = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' 
                    AND (name='CourseSyllabi' OR name='WeeklySummaries');";

                using (var cmd = new SQLiteCommand(verifyQuery, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\nVerifying tables:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"  ✓ {reader.GetString(0)}");
                    }
                }

                connection.Close();
            }

            Console.WriteLine("\n✅ Migration complete!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
