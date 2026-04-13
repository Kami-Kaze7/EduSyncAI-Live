import paramiko

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def get(cmd, t=30):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    out = stdout.read().decode('utf-8', errors='replace').strip()
    err = stderr.read().decode('utf-8', errors='replace').strip()
    return out, err

r = []

# 1. Check admin credentials in deployed DB
r.append("=== ADMINS TABLE (deployed DB) ===")
o, e = get("sqlite3 /opt/edusyncai/publish/Data/edusync.db 'SELECT * FROM Admins;'")
r.append(f"OUT: {o}")
r.append(f"ERR: {e}")

r.append("\n=== ADMINS SCHEMA ===")
o, e = get("sqlite3 /opt/edusyncai/publish/Data/edusync.db '.schema Admins'")
r.append(f"OUT: {o}")
r.append(f"ERR: {e}")

# 2. Check admin count
r.append("\n=== ADMIN COUNT ===")
o, e = get("sqlite3 /opt/edusyncai/publish/Data/edusync.db 'SELECT COUNT(*) FROM Admins;'")
r.append(f"Count: {o}")

# 3. Check the source DB for comparison
r.append("\n=== ADMINS in SOURCE DB ===")
o, e = get("sqlite3 /opt/edusyncai/Data/edusync.db 'SELECT * FROM Admins;' 2>&1")
r.append(f"OUT: {o}")

r.append("\n=== SOURCE DB SCHEMA ===")
o, e = get("sqlite3 /opt/edusyncai/Data/edusync.db '.schema Admins' 2>&1")
r.append(f"OUT: {o}")

# 4. Check all table row counts in deployed DB
r.append("\n=== ROW COUNTS (deployed) ===")
tables = ["Admins", "Lecturers", "Students", "Courses", "Faculties", "Departments", 
          "YearsOfStudy", "ClassSessions", "Attendance", "CourseEnrollments",
          "LectureMaterials", "LectureNotes", "Lectures", "CourseSyllabi",
          "CourseVideos", "ClassSummaries", "WeeklySummaries", "StudentWeeklySummaries",
          "ModelAssets", "LecturePreps"]
for t in tables:
    o, e = get(f"sqlite3 /opt/edusyncai/publish/Data/edusync.db 'SELECT COUNT(*) FROM {t};' 2>&1")
    r.append(f"  {t}: {o}")

# 5. Same for source DB
r.append("\n=== ROW COUNTS (source) ===")
for t in tables:
    o, e = get(f"sqlite3 /opt/edusyncai/Data/edusync.db 'SELECT COUNT(*) FROM {t};' 2>&1")
    r.append(f"  {t}: {o}")

# 6. Check SSL cert status
r.append("\n=== SSL CERT ===")
o, e = get("certbot certificates 2>&1")
r.append(o[:500])

# 7. Check nginx config for SSL
r.append("\n=== NGINX SSL CONFIG ===")
o, e = get("grep -A3 'ssl_certificate' /etc/nginx/sites-enabled/edusyncai")
r.append(o)

# 8. Try admin login API
r.append("\n=== ADMIN LOGIN TEST ===")
o, e = get("""curl -s -X POST http://localhost:5152/api/auth/admin/login -H 'Content-Type: application/json' -d '{"email":"admin@edusync.com","password":"admin123"}'""")
r.append(f"Response: {o[:300]}")

# 9. Try different common credentials
r.append("\n=== ADMIN LOGIN TEST 2 ===")
o, e = get("""curl -s -X POST http://localhost:5152/api/auth/admin/login -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin123"}'""")
r.append(f"Response: {o[:300]}")

# 10. Check the login endpoint
r.append("\n=== API ROUTES ===")
o, e = get("curl -s http://localhost:5152/swagger/v1/swagger.json 2>/dev/null | python3 -c \"import sys,json; d=json.load(sys.stdin); [print(p) for p in d.get('paths',{}).keys() if 'auth' in p.lower() or 'login' in p.lower() or 'admin' in p.lower()]\" 2>&1")
r.append(o[:500])

# 11. Check the local DB file we have
r.append("\n=== LOCAL GIT DB ===")
o, e = get("ls -lh /opt/edusyncai/Data/edusync.db /opt/edusyncai/publish/Data/edusync.db")
r.append(o)

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\investigate.txt", "w") as f:
    f.write(clean)
print("Written to investigate.txt")
