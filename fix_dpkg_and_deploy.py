import paramiko, sys, io, time
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

# 1. Fix dpkg
print("=" * 60, flush=True)
print("Fixing dpkg and btrfs hook", flush=True)
print("=" * 60, flush=True)
run(ssh, "echo '#!/bin/sh' > /usr/share/initramfs-tools/hooks/btrfs")
run(ssh, "echo 'exit 0' >> /usr/share/initramfs-tools/hooks/btrfs")
run(ssh, "chmod +x /usr/share/initramfs-tools/hooks/btrfs")
run(ssh, "dpkg --configure -a")
run(ssh, "apt-get install -f -y")

# 2. Upgrade Node.js
print("=" * 60, flush=True)
print("Upgrading Node.js", flush=True)
print("=" * 60, flush=True)
run(ssh, "systemctl stop edusyncai-web")
run(ssh, "apt-get remove -y nodejs npm libnode-dev; apt-get autoremove -y")
run(ssh, "rm -rf /etc/apt/sources.list.d/nodesource.list*")
run(ssh, "curl -fsSL https://deb.nodesource.com/setup_20.x | bash -")
run(ssh, "apt-get install -y nodejs")
run(ssh, "node --version && npm --version")

# 3. Clean and build next.js
print("=" * 60, flush=True)
print("Rebuilding Next.js", flush=True)
print("=" * 60, flush=True)
run(ssh, "cd /opt/edusyncai/edusync-web && rm -rf .next node_modules")
run(ssh, """cat > /opt/edusyncai/edusync-web/.env.production << 'EOF'
NEXT_PUBLIC_API_URL=https://62-171-138-230.nip.io
NEXT_PUBLIC_SIGNALR_URL=https://62-171-138-230.nip.io
EOF""")
run(ssh, "cd /opt/edusyncai/edusync-web && npm install --force --no-audit --no-fund")
run(ssh, "cd /opt/edusyncai/edusync-web && npm run build")
run(ssh, "ls -la /opt/edusyncai/edusync-web/.next/ | head -5")

# 4. Service update
run(ssh, """cat > /etc/systemd/system/edusyncai-web.service << 'EOF'
[Unit]
Description=EduSyncAI Next.js Frontend
After=network.target

[Service]
WorkingDirectory=/opt/edusyncai/edusync-web
ExecStart=/usr/bin/npm run start
Restart=always
RestartSec=10
Environment=NODE_ENV=production
Environment=PORT=3000

[Install]
WantedBy=multi-user.target
EOF""")
run(ssh, "systemctl daemon-reload && systemctl enable edusyncai-web && systemctl restart edusyncai-web")

# 5. DB & API Fix
run(ssh, "mkdir -p /opt/edusyncai/publish/api/Data")
run(ssh, "cp /opt/edusyncai/Data/edusync.db /opt/edusyncai/publish/api/Data/edusync.db")
run(ssh, "chmod 666 /opt/edusyncai/publish/api/Data/edusync.db")
run(ssh, "ln -sf /opt/edusyncai/Data/Recordings /opt/edusyncai/publish/api/Data/Recordings")
run(ssh, "ln -sf /opt/edusyncai/Data/WhiteboardImages /opt/edusyncai/publish/api/Data/WhiteboardImages")
run(ssh, "systemctl restart edusyncai-api")

time.sleep(5)
run(ssh, "certbot --nginx -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com")

run(ssh, "curl -s -o /dev/null -w 'API: %{http_code}' http://localhost:5152/api/courses")
run(ssh, "curl -s -o /dev/null -w 'Web: %{http_code}' http://localhost:3000")

ssh.close()
