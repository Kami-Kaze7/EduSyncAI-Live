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

r.append("=== FILES ON SERVER ===")
r.append(get("find /opt/edusyncai/Data/uploads -type f -exec ls -lh {} \\;"))
r.append(get("find /opt/edusyncai/publish/Data/uploads -type f -exec ls -lh {} \\;"))

r.append("\n=== STUDENTS WITH PHOTOS ===")
r.append(get("sqlite3 /opt/edusyncai/publish/Data/edusync.db 'SELECT Id, FullName, PhotoPath FROM Students;'"))

r.append("\n=== IMAGE ACCESS TEST ===")
https_code = get("curl -sk -o /dev/null -w '%%{http_code}' https://62-171-138-230.nip.io/uploads/students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg")
api_code = get("curl -s -o /dev/null -w '%%{http_code}' http://localhost:5152/uploads/students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg")
r.append("Via HTTPS: " + https_code)
r.append("Via API: " + api_code)

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\upload_verify.txt", "w") as f:
    f.write(clean)
print("Written to upload_verify.txt")
