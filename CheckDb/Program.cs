using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

var dbPath = @"C:\EduSyncAI\Data\edusync.db";
Console.WriteLine("Checking Lecturers in: " + dbPath);
Console.WriteLine();

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// Check Lecturers table schema
Console.WriteLine("=== LECTURERS TABLE SCHEMA ===");
var schemaCmd = new SqliteCommand("PRAGMA table_info(Lecturers);", connection);
using var schemaReader = schemaCmd.ExecuteReader();
while (schemaReader.Read())
{
    Console.WriteLine($"  Col {schemaReader.GetInt32(0)}: {schemaReader.GetString(1)} ({schemaReader.GetString(2)})");
}
schemaReader.Close();

// Check all lecturer records
Console.WriteLine();
Console.WriteLine("=== ALL LECTURERS ===");
var cmd = new SqliteCommand("SELECT * FROM Lecturers", connection);
using var reader = cmd.ExecuteReader();
int count = 0;
while (reader.Read())
{
    count++;
    Console.WriteLine($"--- Lecturer #{count} ---");
    for (int i = 0; i < reader.FieldCount; i++)
    {
        var name = reader.GetName(i);
        var val = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
        // Truncate long hash values
        if (val != null && val.Length > 80)
            val = val.Substring(0, 80) + "...";
        Console.WriteLine($"  {name} = {val}");
    }
}
reader.Close();

if (count == 0)
{
    Console.WriteLine("  *** NO LECTURERS FOUND IN DATABASE ***");
}

// Also test password hashing
Console.WriteLine();
Console.WriteLine("=== PASSWORD HASH TEST ===");
string testPassword = "password123";

// Hex format (desktop)
using var sha256 = SHA256.Create();
byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(testPassword));
StringBuilder hexBuilder = new StringBuilder();
foreach (byte b in hashBytes) hexBuilder.Append(b.ToString("x2"));
Console.WriteLine($"  '{testPassword}' hex hash:    {hexBuilder}");

// Base64 format (web admin)
using var sha256b = SHA256.Create();
byte[] hashBytes2 = sha256b.ComputeHash(Encoding.UTF8.GetBytes(testPassword));
string base64Hash = Convert.ToBase64String(hashBytes2);
Console.WriteLine($"  '{testPassword}' base64 hash: {base64Hash}");
