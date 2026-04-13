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

# Services
r.append(f"API: {get('systemctl is-active edusyncai-api')}")
r.append(f"WEB: {get('systemctl is-active edusyncai-web')}")
r.append(f"FACE: {get('systemctl is-active edusyncai-face')}")

# HTTP checks
r.append(f"\nHTTPS frontend (nip.io): {get('curl -sk -o /dev/null -w %{{http_code}} https://62-171-138-230.nip.io')}")
r.append(f"HTTPS API (nip.io): {get('curl -sk -o /dev/null -w %{{http_code}} https://62-171-138-230.nip.io/api/courses')}")

# API data
r.append(f"\nAPI courses: {get('curl -s http://localhost:5152/api/courses')[:500]}")
r.append(f"\nAPI lecturers: {get('curl -s http://localhost:5152/api/lecturers')[:300]}")
r.append(f"\nAPI faculties: {get('curl -s http://localhost:5152/api/faculties')[:300]}")

# DB
r.append(f"\nDB file: {get('ls -lh /opt/edusyncai/publish/Data/edusync.db')}")

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\final_check.txt", "w") as f:
    f.write(clean)
print("Written to final_check.txt")
