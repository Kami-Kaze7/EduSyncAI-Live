# PowerShell script to check StudentWeeklySummaries table
$dbPath = "C:\EduSyncAI\Data\edusync.db"

Write-Host "Checking StudentWeeklySummaries table..." -ForegroundColor Cyan

# Load SQLite assembly
Add-Type -Path "C:\Users\Hp\.nuget\packages\microsoft.data.sqlite.core\9.0.0\lib\net9.0\Microsoft.Data.Sqlite.dll"

$connectionString = "Data Source=$dbPath"
$connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)

try {
    $connection.Open()
    
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM StudentWeeklySummaries;"
    $count = $cmd.ExecuteScalar()
    Write-Host "Total records in StudentWeeklySummaries: $count" -ForegroundColor Yellow

    if ($count -gt 0) {
        $cmd.CommandText = "SELECT * FROM StudentWeeklySummaries LIMIT 5;"
        $reader = $cmd.ExecuteReader()
        Write-Host "`nSample records:" -ForegroundColor Cyan
        while ($reader.Read()) {
            Write-Host "Id: $($reader.GetInt32(0)), StudentId: $($reader.GetInt32(1)), WeeklySummaryId: $($reader.GetInt32(2)), SentAt: $($reader.GetString(3))"
        }
        $reader.Close()
    }

} catch {
    Write-Host "Error: $_" -ForegroundColor Red
} finally {
    $connection.Close()
}
