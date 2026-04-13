import paramiko, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def run(ssh, cmd, timeout=600):
    print(f"\n>>> {cmd}", flush=True)
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode('utf-8', errors='replace')
    err = stderr.read().decode('utf-8', errors='replace')
    ec = stdout.channel.recv_exit_status()
    # Print clean ASCII
    for line in out.strip().split('\n')[-30:]:
        clean = ''.join(c if ord(c) < 128 else '?' for c in line)
        if clean.strip():
            print(f"  {clean}", flush=True)
    for line in err.strip().split('\n')[-10:]:
        clean = ''.join(c if ord(c) < 128 else '?' for c in line)
        if clean.strip():
            print(f"  [ERR] {clean}", flush=True)
    print(f"  [EXIT: {ec}]", flush=True)
    return out, err, ec

# ===== Step 1: Remove old Node.js and install Node 20 =====
print("=" * 60, flush=True)
print("STEP 1: Upgrade Node.js to v20", flush=True)
print("=" * 60, flush=True)

# Stop the web service first
run(ssh, "systemctl stop edusyncai-web")

# Remove old nodejs
run(ssh, "apt-get remove -y nodejs npm 2>/dev/null; apt-get autoremove -y")

# Remove old nodesource list
run(ssh, "rm -f /etc/apt/sources.list.d/nodesource.list*")

# Install Node 20 via nodesource
run(ssh, "curl -fsSL https://deb.nodesource.com/setup_20.x | bash -", timeout=120)
run(ssh, "apt-get install -y nodejs", timeout=120)

# Verify
run(ssh, "node --version")
run(ssh, "npm --version")

# ===== Step 2: Rebuild Next.js =====
print("\n" + "=" * 60, flush=True)
print("STEP 2: Rebuild Next.js frontend", flush=True)
print("=" * 60, flush=True)

# Clean old build artifacts and node_modules
run(ssh, "cd /opt/edusyncai/edusync-web && rm -rf .next node_modules")

# Create .env.production for the build
run(ssh, """cat > /opt/edusyncai/edusync-web/.env.production << 'EOF'
NEXT_PUBLIC_API_URL=https://62-171-138-230.nip.io
NEXT_PUBLIC_SIGNALR_URL=https://62-171-138-230.nip.io
EOF""")

# Install dependencies
run(ssh, "cd /opt/edusyncai/edusync-web && npm install", timeout=300)

# Build
run(ssh, "cd /opt/edusyncai/edusync-web && npm run build", timeout=300)

# Verify build output
run(ssh, "ls -la /opt/edusyncai/edusync-web/.next/ | head -10")

# ===== Step 3: Update service to use node directly =====
print("\n" + "=" * 60, flush=True)
print("STEP 3: Update web service", flush=True)
print("=" * 60, flush=True)

# Use node_modules/.bin/next directly instead of npx
run(ssh, """cat > /etc/systemd/system/edusyncai-web.service << 'EOF'
[Unit]
Description=EduSyncAI Next.js Frontend
After=network.target

[Service]
WorkingDirectory=/opt/edusyncai/edusync-web
ExecStart=/usr/bin/node /opt/edusyncai/edusync-web/node_modules/.bin/next start -p 3000
Restart=always
RestartSec=10
Environment=NODE_ENV=production
Environment=PORT=3000

[Install]
WantedBy=multi-user.target
EOF""")

run(ssh, "systemctl daemon-reload")
run(ssh, "systemctl restart edusyncai-web")

# Wait for startup
import time
time.sleep(5)

# ===== Step 4: Verify web service =====
print("\n" + "=" * 60, flush=True)
print("STEP 4: Verify web service", flush=True)
print("=" * 60, flush=True)

run(ssh, "systemctl status edusyncai-web --no-pager | head -15")
run(ssh, "curl -s -o /dev/null -w 'Web HTTP: %{http_code}' http://localhost:3000")

# ===== Step 5: Fix API database path =====
print("\n" + "=" * 60, flush=True)
print("STEP 5: Fix API database path", flush=True) 
print("=" * 60, flush=True)

# Check where the API is looking for the DB
run(ssh, "grep -r 'edusync.db' /opt/edusyncai/publish/api/appsettings*.json 2>/dev/null || echo 'No appsettings match'")
run(ssh, "cat /opt/edusyncai/publish/api/appsettings.json 2>/dev/null | head -20")

# Ensure DB is accessible from the publish directory
run(ssh, "ls -lh /opt/edusyncai/Data/edusync.db")
run(ssh, "mkdir -p /opt/edusyncai/publish/api/Data")
run(ssh, "cp /opt/edusyncai/Data/edusync.db /opt/edusyncai/publish/api/Data/edusync.db")
run(ssh, "chmod 644 /opt/edusyncai/publish/api/Data/edusync.db")

# Also create symlink for recordings/uploads
run(ssh, "ln -sf /opt/edusyncai/Data/Recordings /opt/edusyncai/publish/api/Data/Recordings")
run(ssh, "ln -sf /opt/edusyncai/Data/WhiteboardImages /opt/edusyncai/publish/api/Data/WhiteboardImages")
run(ssh, "ln -sf /opt/edusyncai/Data/LectureMaterials /opt/edusyncai/publish/api/Data/LectureMaterials")

# Restart API to pick up DB
run(ssh, "systemctl restart edusyncai-api")
time.sleep(3)

# Test API
run(ssh, "curl -s http://localhost:5152/api/courses | head -c 300")

# ===== Step 6: SSL Certificate =====
print("\n" + "=" * 60, flush=True)
print("STEP 6: SSL Certificate", flush=True)
print("=" * 60, flush=True)

run(ssh, "certbot --nginx -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com 2>&1", timeout=120)

# ===== Step 7: Final verification =====
print("\n" + "=" * 60, flush=True)
print("STEP 7: Final verification", flush=True)
print("=" * 60, flush=True)

time.sleep(3)

out, _, _ = run(ssh, "systemctl is-active edusyncai-api")
out2, _, _ = run(ssh, "systemctl is-active edusyncai-web")
out3, _, _ = run(ssh, "systemctl is-active edusyncai-face")

run(ssh, "curl -s -o /dev/null -w 'API: %{http_code}' http://localhost:5152/api/courses && echo ''")
run(ssh, "curl -s -o /dev/null -w 'Web: %{http_code}' http://localhost:3000 && echo ''")

run(ssh, "ss -tlnp | grep -E '3000|5152|80|443'")

print("\n" + "=" * 60, flush=True)
print("DEPLOYMENT FIX COMPLETE!", flush=True)
print("=" * 60, flush=True)

ssh.close()
