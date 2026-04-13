#!/bin/bash
# ==============================================
# EduSyncAI Services & Nginx Configuration
# ==============================================

set -e

echo "=========================================="
echo " Configuring Services & Nginx + SSL"
echo "=========================================="

# --- 1. Create systemd service for .NET WebAPI ---
echo "[1/5] Creating .NET WebAPI service..."
cat > /etc/systemd/system/edusyncai-api.service << 'EOF'
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
EOF

# --- 2. Create systemd service for Next.js Frontend ---
echo "[2/5] Creating Next.js frontend service..."
cat > /etc/systemd/system/edusyncai-web.service << 'EOF'
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
EOF

# --- 3. Create systemd service for Python Face Recognition ---
echo "[3/5] Creating Python face recognition service..."
cat > /etc/systemd/system/edusyncai-face.service << 'EOF'
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
EOF

# --- 4. Configure Nginx ---
echo "[4/5] Configuring Nginx..."
cat > /etc/nginx/sites-available/edusyncai << 'NGINX'
server {
    listen 80;
    server_name 62-171-138-230.nip.io;

    # Increase upload size for video recordings
    client_max_body_size 250M;

    # API and SignalR proxy
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

    # SignalR Hub
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

    # Static uploads (profile photos, 3D models, etc.)
    location /uploads/ {
        proxy_pass http://localhost:5152;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Swagger UI (optional, for API docs)
    location /swagger {
        proxy_pass http://localhost:5152;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # Next.js frontend (everything else)
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
NGINX

# Enable site
ln -sf /etc/nginx/sites-available/edusyncai /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

# Test and reload Nginx
nginx -t
systemctl reload nginx

# --- 5. Setup SSL with Let's Encrypt ---
echo "[5/5] Setting up SSL certificate..."
certbot --nginx -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com

# --- Start all services ---
echo "Starting all services..."
systemctl daemon-reload
systemctl enable edusyncai-api edusyncai-web edusyncai-face
systemctl start edusyncai-api
systemctl start edusyncai-web
systemctl start edusyncai-face
systemctl reload nginx

echo "=========================================="
echo " ✅ EduSyncAI is now LIVE!"
echo ""
echo " 🌐 Frontend: https://62-171-138-230.nip.io"
echo " 📡 API:      https://62-171-138-230.nip.io/api"
echo " 📋 Swagger:  https://62-171-138-230.nip.io/swagger"
echo " 🎥 SignalR:  wss://62-171-138-230.nip.io/hubs/classroom"
echo "=========================================="
