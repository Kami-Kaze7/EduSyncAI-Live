import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmd = r"""
echo "=== MERGING WITH CORRECT SCHEMA ==="
sqlite3 /opt/edusyncai/publish/Data/edusync.db << 'SQLEOF'
ATTACH DATABASE '/opt/edusyncai/Data/edusync.db' AS olddb;

-- Merge Students (correct columns from schema)
INSERT OR IGNORE INTO Students (Id, MatricNumber, FullName, Email, PhotoPath, PasswordHash, IsActive, Age, Hobbies, Bio)
SELECT Id, MatricNumber, FullName, Email, PhotoPath, PasswordHash, IsActive, Age, Hobbies, Bio FROM olddb.Students;

-- Merge Courses (correct columns from schema)
INSERT OR IGNORE INTO Courses (Id, CourseCode, CourseTitle, SyllabusPath, LecturerId, CreatedAt, CreditHours, CourseName)
SELECT Id, CourseCode, CourseTitle, SyllabusPath, LecturerId, CreatedAt, CreditHours, CourseName FROM olddb.Courses;

-- Merge CourseEnrollments
INSERT OR IGNORE INTO CourseEnrollments (Id, CourseId, StudentId)
SELECT Id, CourseId, StudentId FROM olddb.CourseEnrollments;

-- Merge ClassSessions (correct columns)
INSERT OR IGNORE INTO ClassSessions (Id, CourseId, LectureId, LecturerId, SessionCode, SessionState, StartTime, EndTime, AudioFilePath, VideoFilePath, BoardExportPath, BoardSnapshotFolder, AttendanceCount, Duration, CreatedAt, Topic)
SELECT Id, CourseId, LectureId, LecturerId, SessionCode, SessionState, StartTime, EndTime, AudioFilePath, VideoFilePath, BoardExportPath, BoardSnapshotFolder, AttendanceCount, Duration, CreatedAt, Topic FROM olddb.ClassSessions;

-- Merge Attendance
INSERT OR IGNORE INTO Attendance (Id, SessionId, StudentId, CheckInTime, CheckInMethod, VerifiedBy)
SELECT Id, SessionId, StudentId, CheckInTime, CheckInMethod, VerifiedBy FROM olddb.Attendance;

-- Merge CourseSyllabi (if exists in old)
INSERT OR IGNORE INTO CourseSyllabi (Id, CourseId, LecturerId, FileName, FilePath, FileType, ExtractedText, TotalWeeks, UploadedAt)
SELECT Id, CourseId, LecturerId, FileName, FilePath, FileType, ExtractedText, TotalWeeks, UploadedAt FROM olddb.CourseSyllabi;

-- Merge WeeklySummaries (if exists)
INSERT OR IGNORE INTO WeeklySummaries (Id, SyllabusId, CourseId, LecturerId, WeekNumber, WeekTitle, Summary, KeyTopics, LearningObjectives, PreparationNotes, GeneratedAt, SentToStudents, SentAt)
SELECT Id, SyllabusId, CourseId, LecturerId, WeekNumber, WeekTitle, Summary, KeyTopics, LearningObjectives, PreparationNotes, GeneratedAt, SentToStudents, SentAt FROM olddb.WeeklySummaries;

-- Merge Admins
INSERT OR IGNORE INTO Admins (Id, Username, PasswordHash, FullName, CreatedAt)
SELECT Id, Username, PasswordHash, FullName, CreatedAt FROM olddb.Admins;

-- Merge Lectures table
INSERT OR IGNORE INTO Lectures (Id, CourseId, LecturerId, Topic, ScheduledDate, StartTime, EndTime, Status, JitsiRoomName, RecordingPath, WhiteboardData, Notes, CreatedAt)
SELECT Id, CourseId, LecturerId, Topic, ScheduledDate, StartTime, EndTime, Status, JitsiRoomName, RecordingPath, WhiteboardData, Notes, CreatedAt FROM olddb.Lectures;

DETACH DATABASE olddb;
SQLEOF

echo ""
echo "=== FINAL COUNTS ==="
echo -n "Lecturers: "; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM Lecturers;"
echo -n "Students: "; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM Students;"
echo -n "Courses: "; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM Courses;"
echo -n "ModelAssets: "; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM ModelAssets;"
echo -n "Admins: "; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT COUNT(*) FROM Admins;"

echo ""
echo "=== DETAIL ==="
echo "Lecturers:"; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT Id, Username, FullName FROM Lecturers;"
echo "Students:"; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT Id, MatricNumber, FullName FROM Students;"
echo "Courses:"; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT Id, CourseCode, CourseTitle FROM Courses;"
echo "ModelAssets:"; sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT Id, Title, Discipline FROM ModelAssets;"

echo ""
systemctl restart edusyncai-api.service
echo "API RESTARTED"
echo "MERGE COMPLETE"
"""

_, stdout, stderr = client.exec_command(cmd, timeout=20)
result = stdout.read().decode('utf-8', 'ignore')
err = stderr.read().decode('utf-8', 'ignore')
with open("merge2_result.txt", "w", encoding="utf-8") as f:
    f.write(result)
    if err:
        f.write("\nSTDERR:\n" + err)
print("SAVED")
client.close()
