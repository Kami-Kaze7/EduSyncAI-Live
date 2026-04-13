import paramiko, time

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def get(cmd, t=60):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    out = stdout.read().decode('utf-8', errors='replace').strip()
    err = stderr.read().decode('utf-8', errors='replace').strip()
    return out, err

def run(cmd, t=120):
    print(f">>> {cmd}", flush=True)
    o, e = get(cmd, t)
    if o:
        for line in o.split('\n')[-10:]:
            c = ''.join(ch if ord(ch) < 128 else '?' for ch in line)
            if c.strip(): print(f"  {c}", flush=True)
    if e:
        for line in e.split('\n')[-5:]:
            c = ''.join(ch if ord(ch) < 128 else '?' for ch in line)
            if c.strip(): print(f"  [E] {c}", flush=True)
    return o

r = []

# 1. Check how the frontend calls the admin API
r.append("=== ADMIN API MODULE ===")
o, _ = get("cat /opt/edusyncai/edusync-web/lib/adminApi.ts 2>/dev/null || cat /opt/edusyncai/edusync-web/lib/api.ts 2>/dev/null")
r.append(o[:2000])

# 2. Check .env.production
r.append("\n=== ENV PRODUCTION ===")
o, _ = get("cat /opt/edusyncai/edusync-web/.env.production")
r.append(o)

# 3. Check the built next.js for the API URL  
r.append("\n=== BUILT API URL ===")
o, _ = get("grep -r 'NEXT_PUBLIC_API_URL\\|62-171-138-230\\|62.171.138.230\\|localhost:5152' /opt/edusyncai/edusync-web/.next/server/ 2>/dev/null | head -5")
r.append(o[:300])

# 4. Check if Swagger is enabled in production
r.append("\n=== SWAGGER CHECK ===")
o, _ = get("curl -s -o /dev/null -w '%{http_code}' http://localhost:5152/swagger/index.html")
r.append(f"Swagger status: {o}")

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\investigate3.txt", "w") as f:
    f.write(clean)
print("Written to investigate3.txt")
