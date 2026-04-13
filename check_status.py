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

# API data check
api_data = get("curl -s http://localhost:5152/api/courses | head -c 300")
r.append(f"API courses: {api_data[:300]}")

# HTTPS check
r.append(f"HTTPS web: {get('curl -sk -o /dev/null -w %{{http_code}} https://62-171-138-230.nip.io')}")
r.append(f"HTTPS api: {get('curl -sk -o /dev/null -w %{{http_code}} https://62-171-138-230.nip.io/api/courses')}")

# DB
r.append(f"DB: {get('ls -lh /opt/edusyncai/publish/Data/edusync.db')}")

# Uptime
r.append(f"Uptime: {get('uptime')}")

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\current_status.txt", "w") as f:
    f.write(clean)
print("Written to current_status.txt")
