import paramiko, time

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def run(cmd, t=30):
    print(f">>> {cmd}", flush=True)
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    out = stdout.read().decode('utf-8', errors='replace').strip()
    err = stderr.read().decode('utf-8', errors='replace').strip()
    ec = stdout.channel.recv_exit_status()
    if out:
        for line in out.split('\n')[-10:]:
            c = ''.join(ch if ord(ch) < 128 else '?' for ch in line)
            if c.strip(): print(f"  {c}", flush=True)
    if err:
        for line in err.split('\n')[-5:]:
            c = ''.join(ch if ord(ch) < 128 else '?' for ch in line)
            if c.strip(): print(f"  [E] {c}", flush=True)
    print(f"  [exit:{ec}]", flush=True)
    return out

# Stop API to safely copy DB
run("systemctl stop edusyncai-api")

# The API resolves DB path as: WorkingDirectory/../Data/edusync.db
# = /opt/edusyncai/publish/api/../Data/edusync.db
# = /opt/edusyncai/publish/Data/edusync.db
# We need to copy the REAL DB (596K) from /opt/edusyncai/Data/ to /opt/edusyncai/publish/Data/

run("ls -lh /opt/edusyncai/Data/edusync.db")
run("ls -lh /opt/edusyncai/publish/Data/edusync.db")

# Backup the empty one
run("mv /opt/edusyncai/publish/Data/edusync.db /opt/edusyncai/publish/Data/edusync.db.empty.bak")

# Copy the real DB
run("cp /opt/edusyncai/Data/edusync.db /opt/edusyncai/publish/Data/edusync.db")
run("chmod 666 /opt/edusyncai/publish/Data/edusync.db")
run("ls -lh /opt/edusyncai/publish/Data/edusync.db")

# Also set up symlinks for uploads/recordings so they resolve from publish/Data/
run("ln -sf /opt/edusyncai/Data/Recordings /opt/edusyncai/publish/Data/Recordings")
run("ln -sf /opt/edusyncai/Data/WhiteboardImages /opt/edusyncai/publish/Data/WhiteboardImages")
run("ln -sf /opt/edusyncai/Data/LectureMaterials /opt/edusyncai/publish/Data/LectureMaterials")

# Start API
run("systemctl start edusyncai-api")
time.sleep(3)

# Test
out = run("curl -s http://localhost:5152/api/courses | head -c 500")
print(f"\n>>> API response: {out[:300]}", flush=True)

out = run("curl -s http://localhost:5152/api/courses | python3 -c 'import sys,json; d=json.load(sys.stdin); print(f\"Course count: {len(d)}\"); [print(f\"  - {c.get(\"courseName\",\"?\")} ({c.get(\"courseCode\",\"?\")})\" ) for c in d[:5]]' 2>&1")

ssh.close()
print("\nDONE!", flush=True)
