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
    return out

# ===== FIX 1: FRONTEND API URL =====
print("=" * 50)
print("FIX 1: Fix NEXT_PUBLIC_API_URL (missing /api)")
print("=" * 50)

# Fix the .env.production - need /api suffix
run("""cat > /opt/edusyncai/edusync-web/.env.production << 'EOF'
NEXT_PUBLIC_API_URL=https://62-171-138-230.nip.io/api
NEXT_PUBLIC_SIGNALR_URL=https://62-171-138-230.nip.io
EOF""")

run("cat /opt/edusyncai/edusync-web/.env.production")

# Rebuild Next.js with the corrected env
run("cd /opt/edusyncai/edusync-web && npm run build", t=300)

# Restart web service
run("systemctl restart edusyncai-web")
time.sleep(3)

# Test the login from browser perspective (via nginx)
print("\n" + "=" * 50)
print("VERIFY: Admin login via HTTPS")
print("=" * 50)

# The API endpoint should now be https://62-171-138-230.nip.io/api/admin/login
run("""curl -sk -X POST https://62-171-138-230.nip.io/api/admin/login -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin123"}' 2>&1""")

# ===== FIX 2: SSL CERTIFICATE =====
print("\n" + "=" * 50)
print("FIX 2: SSL - Try Let's Encrypt with certonly")
print("=" * 50)

# First, temporarily restore HTTP access for ACME challenge
# Add a location block for .well-known in the HTTP redirect config
run("""cat > /etc/nginx/sites-available/edusyncai-http << 'EOF'
server {
    listen 80;
    server_name 62-171-138-230.nip.io;

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    location / {
        return 301 https://$host$request_uri;
    }
}
EOF""")

# Update the main config to include both
run("""cat > /etc/nginx/sites-available/edusyncai << 'EOF'
server {
    listen 80;
    server_name 62-171-138-230.nip.io;

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    location / {
        return 301 https://$host$request_uri;
    }
}

server {
    listen 443 ssl;
    server_name 62-171-138-230.nip.io;
    client_max_body_size 250M;

    ssl_certificate /etc/nginx/ssl/selfsigned.crt;
    ssl_certificate_key /etc/nginx/ssl/selfsigned.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

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
}
EOF""")

run("nginx -t 2>&1")
run("systemctl reload nginx")

# Create webroot directory
run("mkdir -p /var/www/html/.well-known/acme-challenge")

# Try certbot with webroot
run("certbot certonly --webroot -w /var/www/html -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com 2>&1", t=120)

# Check if cert was created
cert_check = run("test -f /etc/letsencrypt/live/62-171-138-230.nip.io/fullchain.pem && echo 'YES' || echo 'NO'")

if 'YES' in cert_check:
    print("\nLet's Encrypt cert obtained! Updating nginx...", flush=True)
    # Update nginx to use LE cert
    run("""sed -i 's|ssl_certificate /etc/nginx/ssl/selfsigned.crt;|ssl_certificate /etc/letsencrypt/live/62-171-138-230.nip.io/fullchain.pem;|' /etc/nginx/sites-available/edusyncai""")
    run("""sed -i 's|ssl_certificate_key /etc/nginx/ssl/selfsigned.key;|ssl_certificate_key /etc/letsencrypt/live/62-171-138-230.nip.io/privkey.pem;|' /etc/nginx/sites-available/edusyncai""")
    run("nginx -t 2>&1")
    run("systemctl reload nginx")
    print("SSL updated to Let's Encrypt!", flush=True)
else:
    print("\nLet's Encrypt failed - keeping self-signed cert", flush=True)
    print("Note: nip.io domains sometimes have rate-limiting issues with LE", flush=True)

# ===== FINAL VERIFY =====
print("\n" + "=" * 50)
print("FINAL VERIFICATION")
print("=" * 50)
time.sleep(3)

run("systemctl is-active edusyncai-api")
run("systemctl is-active edusyncai-web")
run("curl -sk -o /dev/null -w 'HTTPS: %{http_code}' https://62-171-138-230.nip.io && echo ''")
run("""curl -sk -X POST https://62-171-138-230.nip.io/api/admin/login -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin123"}'""")

ssh.close()
print("\nDONE!", flush=True)
