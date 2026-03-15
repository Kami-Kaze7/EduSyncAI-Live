# PowerShell script to add StudentWeeklySummaries table to edusync.db
$dbPath = "C:\EduSyncAI\Data\edusync.db"

Write-Host "Adding StudentWeeklySummaries table to database..."

# Load SQLite assembly
Add-Type -Path "C:\Users\Hp\.nuget\packages\microsoft.data.sqlite.core\9.0.0\lib\net9.0\Microsoft.Data.Sqlite.dll"

$connectionString = "Data Source=$dbPath"
$connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)

try {
    $connection.Open()
    Write-Host "Connected to database"

    # Create StudentWeeklySummaries table
    $createTable = "CREATE TABLE IF NOT EXISTS StudentWeeklySummaries (Id INTEGER PRIMARY KEY AUTOINCREMENT, StudentId INTEGER NOT NULL, WeeklySummaryId INTEGER NOT NULL, SentAt TEXT NOT NULL, FOREIGN KEY (StudentId) REFERENCES Students(Id), FOREIGN KEY (WeeklySummaryId) REFERENCES WeeklySummaries(Id));"

    $cmd = $connection.CreateCommand()
    $cmd.CommandText = $createTable
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "StudentWeeklySummaries table created"

    Write-Host "Migration complete!"

} catch {
    Write-Host "Error: $_"
} finally {
    if ($null -ne $connection) {
        $connection.Close()
    }
}
