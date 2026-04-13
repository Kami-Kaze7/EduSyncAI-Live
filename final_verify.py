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
r.append(f"NODE: {get('node --version')}")
r.append(f"API svc: {get('systemctl is-active edusyncai-api')}")
r.append(f"WEB svc: {get('systemctl is-active edusyncai-web')}")
r.append(f"FACE svc: {get('systemctl is-active edusyncai-face')}")
r.append(f"API localhost: {get('curl -s -o /dev/null -w %{{http_code}} http://localhost:5152/api/courses')}")
r.append(f"WEB localhost: {get('curl -s -o /dev/null -w %{{http_code}} http://localhost:3000')}")
r.append(f"HTTPS web: {get('curl -sk -o /dev/null -w %{{http_code}} https://62-171-138-230.nip.io')}")
r.append(f"HTTPS api: {get('curl -sk -o /dev/null -w %{{http_code}} https://62-171-138-230.nip.io/api/courses')}")
r.append(f"HTTPS swagger: {get('curl -sk -o /dev/null -w %{{http_code}} https://62-171-138-230.nip.io/swagger')}")

# API data check
api_data = get("curl -s http://localhost:5152/api/courses")
r.append(f"API courses data: {api_data[:300]}")

# DB check
r.append(f"DB: {get('ls -lh /opt/edusyncai/publish/api/Data/edusync.db 2>&1')}")
r.append(f"DB tables: {get('sqlite3 /opt/edusyncai/publish/api/Data/edusync.db .tables 2>&1')}")
r.append(f"Course count: {get('sqlite3 /opt/edusyncai/publish/api/Data/edusync.db \"SELECT COUNT(*) FROM Courses;\" 2>&1')}")

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\final_verify.txt", "w") as f:
    f.write(clean)
print("Written to final_verify.txt")
