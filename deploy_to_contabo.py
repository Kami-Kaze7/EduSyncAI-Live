import paramiko, sys, time

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

def run(ssh, cmd, timeout=600):
    print(f"\n>>> {cmd}", flush=True)
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode('utf-8', errors='replace')
    err = stderr.read().decode('utf-8', errors='replace')
    ec = stdout.channel.recv_exit_status()
    if out.strip():
        lines = out.strip().split('\n')
        for l in lines[-30:]:
            print(f"  {l}", flush=True)
    if err.strip():
        for l in err.strip().split('\n')[-10:]:
            print(f"  [ERR] {l}", flush=True)
    print(f"  [EXIT: {ec}]", flush=True)
    if ec != 0:
        print(f"  ⚠️  Non-zero exit code!", flush=True)
    return out, err, ec

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
print(f"Connecting to {HOST}...", flush=True)
ssh.connect(HOST, username=USER, password=PASS, timeout=30)
print("Connected!\n", flush=True)

# ===== Step 1: Fix apt/dpkg =====
print("=" * 50, flush=True)
print("STEP 1: Fix apt/dpkg", flush=True)
print("=" * 50, flush=True)
run(ssh, "rm -f /var/lib/dpkg/lock-frontend /var/lib/dpkg/lock /var/cache/apt/archives/lock")
run(ssh, "dpkg --configure -a", timeout=300)
run(ssh, "apt-get update -y", timeout=120)

# ===== Step 2: Install core dependencies =====
print("\n" + "=" * 50, flush=True)
print("STEP 2: Install dependencies", flush=True)
print("=" * 50, flush=True)
run(ssh, "apt-get install -y git curl wget nginx certbot python3-certbot-nginx python3 python3-pip python3-venv", timeout=300)

# ===== Step 3: .NET 9 =====
print("\n" + "=" * 50, flush=True)
print("STEP 3: Install .NET 9", flush=True)
print("=" * 50, flush=True)
run(ssh, "test -f $HOME/.dotnet/dotnet && echo '.NET already installed' || (wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh && /tmp/dotnet-install.sh --channel 9.0)", timeout=300)
run(ssh, "grep -q DOTNET_ROOT ~/.bashrc || echo 'export DOTNET_ROOT=$HOME/.dotnet\nexport PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc")
run(ssh, "$HOME/.dotnet/dotnet --version")

# ===== Step 4: Node.js =====
print("\n" + "=" * 50, flush=True)
print("STEP 4: Install Node.js", flush=True)
print("=" * 50, flush=True)
run(ssh, "node --version 2>/dev/null || (curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && apt-get install -y nodejs)", timeout=180)
run(ssh, "node --version && npm --version")

# ===== Step 5: Clone repo =====
print("\n" + "=" * 50, flush=True)
print("STEP 5: Clone repository", flush=True)
print("=" * 50, flush=True)
run(ssh, "rm -rf /opt/edusyncai")
out, err, ec = run(ssh, "cd /opt && git clone https://github.com/Kami-Kaze7/EduSyncAI-Live.git edusyncai", timeout=600)
if ec != 0:
    print("FATAL: Clone failed! Aborting.", flush=True)
    ssh.close()
    sys.exit(1)
run(ssh, "ls -la /opt/edusyncai/")
run(ssh, "ls -lh /opt/edusyncai/Data/edusync.db 2>/dev/null || echo 'WARNING: DB not found'")
run(ssh, "chmod -R 755 /opt/edusyncai/Data/ && mkdir -p /opt/edusyncai/Data/Recordings /opt/edusyncai/Data/WhiteboardImages /opt/edusyncai/Data/LectureMaterials")

# ===== Step 6: Build .NET WebAPI =====
print("\n" + "=" * 50, flush=True)
print("STEP 6: Build .NET WebAPI", flush=True)
print("=" * 50, flush=True)
run(ssh, "cd /opt/edusyncai/EduSyncAI.WebAPI && $HOME/.dotnet/dotnet restore", timeout=300)
run(ssh, "cd /opt/edusyncai/EduSyncAI.WebAPI && $HOME/.dotnet/dotnet publish -c Release -o /opt/edusyncai/publish/api", timeout=300)
run(ssh, "ls /opt/edusyncai/publish/api/EduSyncAI.WebAPI.dll && echo 'API BUILD OK'")

# ===== Step 7: Build Next.js =====
print("\n" + "=" * 50, flush=True)
print("STEP 7: Build Next.js frontend", flush=True)
print("=" * 50, flush=True)
run(ssh, "cd /opt/edusyncai/edusync-web && npm install", timeout=300)
run(ssh, "cd /opt/edusyncai/edusync-web && npm run build", timeout=300)

# ===== Step 8: Python backend =====
print("\n" + "=" * 50, flush=True)
print("STEP 8: Setup Python backend", flush=True)
print("=" * 50, flush=True)
run(ssh, "cd /opt/edusyncai/backend && python3 -m venv venv", timeout=60)
run(ssh, "cd /opt/edusyncai/backend && ./venv/bin/pip install -r requirements_facial.txt 2>&1 | tail -5", timeout=300)

# ===== Step 9: Create systemd services =====
print("\n" + "=" * 50, flush=True)
print("STEP 9: Create systemd services", flush=True)
print("=" * 50, flush=True)

run(ssh, """cat > /etc/systemd/system/edusyncai-api.service << 'EOF'
[Unit]
Description=EduSyncAI .NET WebAPI
After=network.target

[Service]
WorkingDirectory=/opt/edusyncai/publish/api
ExecStart=/root/.dotnet/dotnet /opt/edusyncai/publish/api/EduSyncAI.WebAPI.dll --urls http://localhost:5152
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5152
Environment=JWT_SECRET=EduSyncAI-Super-Secret-Key-For-JWT-Authentication-Min-32-Chars

[Install]
WantedBy=multi-user.target
EOF""")

run(ssh, """cat > /etc/systemd/system/edusyncai-web.service << 'EOF'
[Unit]
Description=EduSyncAI Next.js Frontend
After=network.target

[Service]
WorkingDirectory=/opt/edusyncai/edusync-web
ExecStart=/usr/bin/npx next start -p 3000
Restart=always
RestartSec=10
Environment=NODE_ENV=production
Environment=PORT=3000

[Install]
WantedBy=multi-user.target
EOF""")

run(ssh, """cat > /etc/systemd/system/edusyncai-face.service << 'EOF'
[Unit]
Description=EduSyncAI Face Recognition Service
After=network.target

[Service]
WorkingDirectory=/opt/edusyncai/backend
ExecStart=/opt/edusyncai/backend/venv/bin/python gemini_face_service.py
Restart=always
RestartSec=10
Environment=FLASK_HOST=127.0.0.1
Environment=FLASK_PORT=5001

[Install]
WantedBy=multi-user.target
EOF""")

# ===== Step 10: Configure Nginx =====
print("\n" + "=" * 50, flush=True)
print("STEP 10: Configure Nginx", flush=True)
print("=" * 50, flush=True)

# Use a Python heredoc approach to avoid shell variable expansion issues
nginx_conf = r"""server {
    listen 80;
    server_name 62-171-138-230.nip.io;
    client_max_body_size 250M;

    location /api/ {
        proxy_pass http://localhost:5152;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 300s;
        proxy_send_timeout 300s;
    }

    location /hubs/ {
        proxy_pass http://localhost:5152;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400s;
    }

    location /uploads/ {
        proxy_pass http://localhost:5152;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /swagger {
        proxy_pass http://localhost:5152;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}"""

# Write nginx conf via SFTP to avoid shell escaping issues
sftp = ssh.open_sftp()
with sftp.file('/etc/nginx/sites-available/edusyncai', 'w') as f:
    f.write(nginx_conf)
sftp.close()
print("  Nginx config written via SFTP", flush=True)

run(ssh, "ln -sf /etc/nginx/sites-available/edusyncai /etc/nginx/sites-enabled/ && rm -f /etc/nginx/sites-enabled/default")
run(ssh, "nginx -t")
run(ssh, "systemctl reload nginx")

# ===== Step 11: Start services =====
print("\n" + "=" * 50, flush=True)
print("STEP 11: Start all services", flush=True)
print("=" * 50, flush=True)
run(ssh, "systemctl daemon-reload")
run(ssh, "systemctl enable edusyncai-api edusyncai-web edusyncai-face")
run(ssh, "systemctl restart edusyncai-api && sleep 3 && systemctl status edusyncai-api --no-pager -l | head -10")
run(ssh, "systemctl restart edusyncai-web && sleep 3 && systemctl status edusyncai-web --no-pager -l | head -10")
run(ssh, "systemctl restart edusyncai-face && sleep 3 && systemctl status edusyncai-face --no-pager -l | head -10")

# ===== Step 12: SSL =====
print("\n" + "=" * 50, flush=True)
print("STEP 12: Setup SSL certificate", flush=True)
print("=" * 50, flush=True)
run(ssh, "certbot --nginx -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com", timeout=120)

# ===== Step 13: Verify =====
print("\n" + "=" * 50, flush=True)
print("STEP 13: Final verification", flush=True)
print("=" * 50, flush=True)
time.sleep(5)
run(ssh, "curl -s -o /dev/null -w 'API HTTP Status: %{http_code}' http://localhost:5152/api/courses")
run(ssh, "curl -s -o /dev/null -w 'Web HTTP Status: %{http_code}' http://localhost:3000")
run(ssh, "systemctl is-active edusyncai-api edusyncai-web edusyncai-face")

print("\n\n" + "=" * 50, flush=True)
print("✅ DEPLOYMENT COMPLETE!", flush=True)
print("", flush=True)
print("🌐 https://62-171-138-230.nip.io", flush=True)
print("📡 https://62-171-138-230.nip.io/api", flush=True)
print("📋 https://62-171-138-230.nip.io/swagger", flush=True)
print("=" * 50, flush=True)

ssh.close()
