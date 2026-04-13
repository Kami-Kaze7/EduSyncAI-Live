import paramiko, time

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def run(cmd, t=120):
    print(f">>> {cmd}", flush=True)
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    out = stdout.read().decode('utf-8', errors='replace').strip()
    err = stderr.read().decode('utf-8', errors='replace').strip()
    ec = stdout.channel.recv_exit_status()
    if out:
        for line in out.split('\n')[-15:]:
            c = ''.join(ch if ord(ch) < 128 else '?' for ch in line)
            if c.strip(): print(f"  {c}", flush=True)
    if err:
        for line in err.split('\n')[-5:]:
            c = ''.join(ch if ord(ch) < 128 else '?' for ch in line)
            if c.strip(): print(f"  [E] {c}", flush=True)
    print(f"  [exit:{ec}]", flush=True)
    return out, err, ec

# ===== FIX 1: DATABASE =====
print("=" * 50)
print("FIX 1: Database")
print("=" * 50)

# Check where the API expects the DB
run("grep -r 'edusync.db' /opt/edusyncai/EduSyncAI.WebAPI/Data/ 2>/dev/null | head -5")
run("grep -ri 'ConnectionString\\|DataSource\\|edusync' /opt/edusyncai/publish/api/appsettings.json 2>/dev/null | head -5")

# Check appsettings for DB path
out, _, _ = run("cat /opt/edusyncai/publish/api/appsettings.json")

# Ensure Data directory and DB exist right next to the DLL
run("ls -la /opt/edusyncai/Data/edusync.db")
run("mkdir -p /opt/edusyncai/publish/api/Data")
run("cp -f /opt/edusyncai/Data/edusync.db /opt/edusyncai/publish/api/Data/edusync.db")
run("chmod 666 /opt/edusyncai/publish/api/Data/edusync.db")
run("ls -la /opt/edusyncai/publish/api/Data/edusync.db")

# Also symlink media directories
run("mkdir -p /opt/edusyncai/publish/api/Data/Recordings /opt/edusyncai/publish/api/Data/WhiteboardImages /opt/edusyncai/publish/api/Data/LectureMaterials")

# Restart API
run("systemctl restart edusyncai-api")
time.sleep(3)

# Test
out, _, _ = run("curl -s http://localhost:5152/api/courses | head -c 300")
print(f"\n  API courses response: {out[:200]}", flush=True)

# ===== FIX 2: SSL =====
print("\n" + "=" * 50)
print("FIX 2: SSL Certificate")
print("=" * 50)

# Try certbot - need to check if nip.io resolves properly first
run("dig +short 62-171-138-230.nip.io")
run("curl -s -o /dev/null -w '%{http_code}' http://62-171-138-230.nip.io")

# Try certbot with standalone first to see if it works
run("certbot certonly --webroot -w /var/www/html -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com 2>&1 || certbot --nginx -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com 2>&1", t=120)

# Check if cert was created
run("certbot certificates 2>&1")

# If no cert, generate self-signed as fallback
run("""
if [ ! -f /etc/letsencrypt/live/62-171-138-230.nip.io/fullchain.pem ]; then
    echo 'No LE cert - generating self-signed...'
    mkdir -p /etc/nginx/ssl
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /etc/nginx/ssl/selfsigned.key \
        -out /etc/nginx/ssl/selfsigned.crt \
        -subj '/CN=62-171-138-230.nip.io'
    echo 'Self-signed cert created'
fi
""")

# ===== FIX 3: NGINX with HTTPS =====
print("\n" + "=" * 50)
print("FIX 3: Nginx HTTPS config")
print("=" * 50)

# Determine which cert to use
out, _, _ = run("test -f /etc/letsencrypt/live/62-171-138-230.nip.io/fullchain.pem && echo 'letsencrypt' || echo 'selfsigned'")
cert_type = out.strip()

if cert_type == 'letsencrypt':
    ssl_cert = "/etc/letsencrypt/live/62-171-138-230.nip.io/fullchain.pem"
    ssl_key = "/etc/letsencrypt/live/62-171-138-230.nip.io/privkey.pem"
else:
    ssl_cert = "/etc/nginx/ssl/selfsigned.crt"
    ssl_key = "/etc/nginx/ssl/selfsigned.key"

nginx_conf = f"""server {{
    listen 80;
    server_name 62-171-138-230.nip.io;
    return 301 https://$host$request_uri;
}}

server {{
    listen 443 ssl;
    server_name 62-171-138-230.nip.io;
    client_max_body_size 250M;

    ssl_certificate {ssl_cert};
    ssl_certificate_key {ssl_key};
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    location /api/ {{
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
    }}

    location /hubs/ {{
        proxy_pass http://localhost:5152;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400s;
    }}

    location /uploads/ {{
        proxy_pass http://localhost:5152;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }}

    location /swagger {{
        proxy_pass http://localhost:5152;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }}

    location / {{
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }}
}}
"""

# Write via SFTP
sftp = ssh.open_sftp()
with sftp.file('/etc/nginx/sites-available/edusyncai', 'w') as f:
    f.write(nginx_conf)
sftp.close()
print("  Nginx HTTPS config written", flush=True)

run("nginx -t 2>&1")
run("systemctl reload nginx")

# ===== VERIFY =====
print("\n" + "=" * 50)
print("FINAL VERIFICATION")
print("=" * 50)
time.sleep(3)

run("curl -s -o /dev/null -w 'API: %{http_code}' http://localhost:5152/api/courses && echo ''")
run("curl -s -o /dev/null -w 'Web: %{http_code}' http://localhost:3000 && echo ''")
run("curl -sk -o /dev/null -w 'HTTPS: %{http_code}' https://62-171-138-230.nip.io && echo ''")
run("curl -sk -o /dev/null -w 'HTTPS API: %{http_code}' https://62-171-138-230.nip.io/api/courses && echo ''")

print(f"\nCert type used: {cert_type}", flush=True)

ssh.close()
print("\nDONE!", flush=True)
