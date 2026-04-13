import paramiko

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def get(cmd, t=30):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    return stdout.read().decode('utf-8', errors='replace').strip()

r = []

# Check connection string in appsettings
r.append("=== appsettings.json ===")
r.append(get("cat /opt/edusyncai/publish/api/appsettings.json"))

# Check appsettings.Production.json
r.append("\n=== appsettings.Production.json ===")
r.append(get("cat /opt/edusyncai/publish/api/appsettings.Production.json 2>&1"))

# Check what DB the API process is actually using
r.append("\n=== API process working dir ===")
r.append(get("ls /proc/$(pgrep -f EduSyncAI.WebAPI)/cwd -la 2>&1"))

# Check all .db files in the publish directory
r.append("\n=== All DB files ===")
r.append(get("find /opt/edusyncai/publish -name '*.db' -exec ls -lh {} \\;"))

# Check DbContext source code for connection string
r.append("\n=== DbContext connection ===")
r.append(get("grep -r 'Data Source\\|ConnectionString\\|edusync\\.db\\|SqliteConnection' /opt/edusyncai/EduSyncAI.WebAPI/Data/ 2>/dev/null | head -10"))

# Check Program.cs for DB config
r.append("\n=== Program.cs DB config ===")
r.append(get("grep -A5 -B2 'sqlite\\|DbContext\\|edusync\\|Data Source' /opt/edusyncai/EduSyncAI.WebAPI/Program.cs 2>/dev/null | head -20"))

# Check the service file
r.append("\n=== Service file ===")
r.append(get("cat /etc/systemd/system/edusyncai-api.service"))

# Test the API with verbose curl
r.append("\n=== API /api/courses verbose ===")
r.append(get("curl -s http://localhost:5152/api/courses"))

# Check sqlite3 content
r.append("\n=== Courses in DB ===")
r.append(get("sqlite3 /opt/edusyncai/publish/api/Data/edusync.db 'SELECT CourseId, CourseName FROM Courses LIMIT 5;'"))

r.append("\n=== Courses in source DB ===")
r.append(get("sqlite3 /opt/edusyncai/Data/edusync.db 'SELECT CourseId, CourseName FROM Courses LIMIT 5;' 2>&1"))

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\db_debug.txt", "w") as f:
    f.write(clean)
print("Written to db_debug.txt")
