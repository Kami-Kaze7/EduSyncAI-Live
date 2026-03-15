using System;
using Microsoft.Data.Sqlite;

var dbPath = @"C:\EduSyncAI\Data\edusync.db";
System.Console.WriteLine("Inserting test record in: " + dbPath);

try {
    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    // Insert record linking Student 1 to WeeklySummary 8
    var sql = "INSERT INTO StudentWeeklySummaries (StudentId, WeeklySummaryId, SentAt) VALUES (1, 8, @sentAt);";
    using (var cmd = new SqliteCommand(sql, connection))
    {
        cmd.Parameters.AddWithValue("@sentAt", DateTime.UtcNow.ToString("O"));
        int rows = cmd.ExecuteNonQuery();
        System.Console.WriteLine($"✓ Inserted {rows} record(s). Student 1 linked to Summary 8.");
    }

} catch (Exception ex) {
    System.Console.WriteLine("Error: " + ex.Message);
}
