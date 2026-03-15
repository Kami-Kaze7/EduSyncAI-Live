# Load the SQLite assembly from the project's output
Add-Type -Path "c:\EduSyncAI\bin\Debug\net9.0-windows10.0.19041.0\Microsoft.Data.Sqlite.dll"

$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=c:\EduSyncAI\Data\edusync.db")
$conn.Open()

# Check table schema
Write-Host "=== LECTURERS TABLE SCHEMA ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "PRAGMA table_info(Lecturers)"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "  Column $($reader.GetInt32(0)): $($reader.GetString(1)) ($($reader.GetString(2)))"
}
$reader.Close()

# Check all lecturer records
Write-Host "`n=== ALL LECTURERS ==="
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "SELECT * FROM Lecturers"
$reader2 = $cmd2.ExecuteReader()
$colCount = $reader2.FieldCount
Write-Host "  Total columns: $colCount"
while ($reader2.Read()) {
    $row = ""
    for ($i = 0; $i -lt $colCount; $i++) {
        $name = $reader2.GetName($i)
        $val = if ($reader2.IsDBNull($i)) { "NULL" } else { $reader2.GetValue($i).ToString() }
        $row += "  $name=$val`n"
    }
    Write-Host $row
    Write-Host "---"
}
$reader2.Close()
$conn.Close()
