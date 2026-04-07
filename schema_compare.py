import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

# First, get the exact schemas of both databases
cmd = """
echo "=== OLD DB SCHEMA ==="
sqlite3 /opt/edusyncai/Data/edusync.db ".schema Students"
echo "---"
sqlite3 /opt/edusyncai/Data/edusync.db ".schema Courses"
echo "---"
sqlite3 /opt/edusyncai/Data/edusync.db ".schema ClassSessions"
echo "---"
sqlite3 /opt/edusyncai/Data/edusync.db ".schema Attendance"
echo "---"
sqlite3 /opt/edusyncai/Data/edusync.db ".schema CourseEnrollments"

echo ""
echo "=== NEW DB SCHEMA ==="
sqlite3 /opt/edusyncai/publish/Data/edusync.db ".schema Students"
echo "---"
sqlite3 /opt/edusyncai/publish/Data/edusync.db ".schema Courses"
echo "---"
sqlite3 /opt/edusyncai/publish/Data/edusync.db ".schema ClassSessions"
echo "---"
sqlite3 /opt/edusyncai/publish/Data/edusync.db ".schema Attendance"
echo "---"
sqlite3 /opt/edusyncai/publish/Data/edusync.db ".schema CourseEnrollments"
echo "DONE"
"""

_, stdout, _ = client.exec_command(cmd, timeout=10)
result = stdout.read().decode('utf-8', 'ignore')
with open("schema_compare.txt", "w", encoding="utf-8") as f:
    f.write(result)
print("SAVED")
client.close()
