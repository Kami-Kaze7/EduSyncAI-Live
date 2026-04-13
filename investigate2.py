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

# Check all API routes
r.append("=== ALL API ENDPOINTS ===")
o, e = get("curl -s http://localhost:5152/swagger/v1/swagger.json | python3 -c \"import sys,json; d=json.load(sys.stdin); [print(f'{m.upper()} {p}') for p,v in sorted(d.get('paths',{}).items()) for m in v.keys()]\" 2>&1")
r.append(o)

# Try login with verbose output
r.append("\n=== ADMIN LOGIN - VERBOSE ===")
o, e = get("""curl -sv -X POST http://localhost:5152/api/auth/admin/login -H 'Content-Type: application/json' -d '{"email":"admin@edusync.com","password":"admin123"}' 2>&1""")
r.append(o[:500])

# Try with username field
r.append("\n=== ADMIN LOGIN - USERNAME ===")
o, e = get("""curl -sv -X POST http://localhost:5152/api/auth/admin/login -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin123"}' 2>&1""")
r.append(o[:500])

# Try other login paths
r.append("\n=== TRY /api/admin/login ===")
o, e = get("""curl -sv -X POST http://localhost:5152/api/admin/login -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin123"}' 2>&1""")
r.append(o[:500])

# Check the auth controller source
r.append("\n=== AUTH CONTROLLER ===")
o, e = get("find /opt/edusyncai/EduSyncAI.WebAPI -name '*Auth*Controller*' -o -name '*Login*' 2>/dev/null | head -5")
r.append(o)

r.append("\n=== AUTH CONTROLLER SOURCE ===")
o, e = get("grep -n 'admin\\|Admin\\|login\\|Login\\|HttpPost\\|Route' /opt/edusyncai/EduSyncAI.WebAPI/Controllers/AuthController.cs 2>/dev/null | head -30")
r.append(o)

# Check what the frontend sends for login
r.append("\n=== FRONTEND LOGIN API ===")
o, e = get("grep -rn 'login\\|Login\\|auth\\|Auth' /opt/edusyncai/edusync-web/app/admin/login/ 2>/dev/null | head -20")
r.append(o)

r.append("\n=== FRONTEND API FILE ===")
o, e = get("grep -rn 'login\\|auth\\|admin' /opt/edusyncai/edusync-web/lib/api.ts 2>/dev/null | head -20")
r.append(o)

# Check certbot error logs
r.append("\n=== CERTBOT DEBUG ===")
o, e = get("certbot --nginx -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com --dry-run 2>&1")
r.append(o[:500])

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\investigate2.txt", "w") as f:
    f.write(clean)
print("Written to investigate2.txt")
