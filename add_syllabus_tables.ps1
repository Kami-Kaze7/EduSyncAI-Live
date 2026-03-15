# PowerShell script to add missing tables to edusync.db
# This script uses System.Data.SQLite to create the CourseSyllabi and WeeklySummaries tables

$dbPath = "C:\EduSyncAI\Data\edusync.db"

Write-Host "Adding missing tables to database..." -ForegroundColor Cyan
Write-Host "Database: $dbPath" -ForegroundColor Gray

# Load SQLite assembly
Add-Type -Path "C:\Users\Hp\.nuget\packages\microsoft.data.sqlite.core\9.0.0\lib\net9.0\Microsoft.Data.Sqlite.dll"

$connectionString = "Data Source=$dbPath"
$connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)

try {
    $connection.Open()
    Write-Host "✓ Connected to database" -ForegroundColor Green

    # Create CourseSyllabi table
    $createCourseSyllabi = @"
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
"@

    $cmd = $connection.CreateCommand()
    $cmd.CommandText = $createCourseSyllabi
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "✓ CourseSyllabi table created" -ForegroundColor Green

    # Create WeeklySummaries table
    $createWeeklySummaries = @"
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
"@

    $cmd.CommandText = $createWeeklySummaries
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "✓ WeeklySummaries table created" -ForegroundColor Green

    # Verify tables exist
    $cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND (name='CourseSyllabi' OR name='WeeklySummaries');"
    $reader = $cmd.ExecuteReader()
    
    Write-Host "`nVerifying tables:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "  ✓ $($reader.GetString(0))" -ForegroundColor Green
    }
    $reader.Close()

    Write-Host "`n✅ Migration complete!" -ForegroundColor Green
    Write-Host "You can now upload syllabi and use the AI summarization feature." -ForegroundColor Yellow

} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
} finally {
    $connection.Close()
}
