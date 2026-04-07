import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

# Strategy: Copy the old data into the new DB while preserving ModelAssets
cmd = """
echo "=== STEP 1: BACKUP CURRENT NEW DB ==="
cp /opt/edusyncai/publish/Data/edusync.db /opt/edusyncai/publish/Data/edusync.db.pre_merge_backup
echo "Backup created"

echo ""
echo "=== STEP 2: CHECK OLD DB DATA ==="
echo "Old Lecturers:"
sqlite3 /opt/edusyncai/Data/edusync.db "SELECT Id, Username, FullName FROM Lecturers;"
echo ""
echo "Old Students:"
sqlite3 /opt/edusyncai/Data/edusync.db "SELECT Id, MatricNumber, FullName FROM Students;"
echo ""
echo "Old Courses:"
sqlite3 /opt/edusyncai/Data/edusync.db "SELECT Id, CourseCode, CourseTitle FROM Courses;"

echo ""
echo "=== STEP 3: CHECK NEW DB DATA ==="
echo "New Lecturers:"
sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT Id, Username, FullName FROM Lecturers;"
echo ""
echo "New ModelAssets:"
sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT Id, Title, Discipline FROM ModelAssets;"

echo ""
echo "=== STEP 4: MERGE OLD DATA INTO NEW DB ==="
# Attach old DB and merge data
sqlite3 /opt/edusyncai/publish/Data/edusync.db << 'SQLEOF'
ATTACH DATABASE '/opt/edusyncai/Data/edusync.db' AS olddb;

-- Merge Lecturers (INSERT OR IGNORE to avoid conflicts)
INSERT OR IGNORE INTO Lecturers (Id, Username, Email, FullName, PasswordHash, IsActive)
SELECT Id, Username, Email, FullName, PasswordHash, IsActive FROM olddb.Lecturers;

-- Merge Students
INSERT OR IGNORE INTO Students (Id, MatricNumber, FullName, Email, PhotoPath, WindowsUsername, PasswordHash, PIN, IsActive, CreatedAt, Age, Hobbies, Bio)
SELECT Id, MatricNumber, FullName, Email, PhotoPath, WindowsUsername, PasswordHash, PIN, IsActive, CreatedAt, Age, Hobbies, Bio FROM olddb.Students;

-- Merge Courses
INSERT OR IGNORE INTO Courses (Id, CourseCode, CourseTitle, LecturerId, Description, Credits)
SELECT Id, CourseCode, CourseTitle, LecturerId, Description, Credits FROM olddb.Courses;

-- Merge CourseEnrollments  
INSERT OR IGNORE INTO CourseEnrollments (Id, StudentId, CourseId)
SELECT Id, StudentId, CourseId FROM olddb.CourseEnrollments;

-- Merge ClassSessions
INSERT OR IGNORE INTO ClassSessions (Id, CourseId, LecturerId, Topic, ScheduledDate, StartTime, EndTime, Status, SessionCode, JitsiRoomName, WhiteboardData, Notes, RecordingPath, CreatedAt)
SELECT Id, CourseId, LecturerId, Topic, ScheduledDate, StartTime, EndTime, Status, SessionCode, JitsiRoomName, WhiteboardData, Notes, RecordingPath, CreatedAt FROM olddb.ClassSessions;

-- Merge Attendance
INSERT OR IGNORE INTO Attendance (Id, SessionId, StudentId, CheckInTime, CheckInMethod, IsVerified, CreatedAt)
SELECT Id, SessionId, StudentId, CheckInTime, CheckInMethod, IsVerified, CreatedAt FROM olddb.Attendance;

-- Merge CourseSyllabi
INSERT OR IGNORE INTO CourseSyllabi (Id, CourseId, LecturerId, FileName, FilePath, FileType, ExtractedText, TotalWeeks, UploadedAt)
SELECT Id, CourseId, LecturerId, FileName, FilePath, FileType, ExtractedText, TotalWeeks, UploadedAt FROM olddb.CourseSyllabi;

-- Merge WeeklySummaries
INSERT OR IGNORE INTO WeeklySummaries (Id, SyllabusId, CourseId, LecturerId, WeekNumber, WeekTitle, Summary, KeyTopics, LearningObjectives, PreparationNotes, GeneratedAt, SentToStudents, SentAt)
SELECT Id, SyllabusId, CourseId, LecturerId, WeekNumber, WeekTitle, Summary, KeyTopics, LearningObjectives, PreparationNotes, GeneratedAt, SentToStudents, SentAt FROM olddb.WeeklySummaries;

-- Merge Admins
INSERT OR IGNORE INTO Admins (Id, Username, PasswordHash, FullName, CreatedAt)
SELECT Id, Username, PasswordHash, FullName, CreatedAt FROM olddb.Admins;

DETACH DATABASE olddb;
SQLEOF

echo ""
echo "=== STEP 5: VERIFY MERGED DATA ==="
echo "Lecturers after merge:"
sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM Lecturers;"
echo "Students after merge:"
sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM Students;"
echo "Courses after merge:"
sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM Courses;"
echo "ModelAssets after merge:"
sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM ModelAssets;"

echo ""
echo "=== STEP 6: RESTART API ==="
systemctl restart edusyncai-api.service
sleep 3
echo "API restarted"

echo ""
echo "=== STEP 7: VERIFY API SERVES DATA ==="
curl -s --connect-timeout 3 http://127.0.0.1:5152/api/admin/lecturers 2>/dev/null | head -200
echo ""
echo "MERGE COMPLETE"
"""

_, stdout, stderr = client.exec_command(cmd, timeout=30)
result = stdout.read().decode('utf-8', 'ignore')
err = stderr.read().decode('utf-8', 'ignore')
with open("merge_result.txt", "w", encoding="utf-8") as f:
    f.write(result)
    if err:
        f.write("\n\nSTDERR:\n" + err)
print("SAVED to merge_result.txt")
client.close()
