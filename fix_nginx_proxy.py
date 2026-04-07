import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

# Fix: NGINX is pointing /api to port 5005 but the API is on port 5152
# Also add /uploads proxy and /hubs proxy for SignalR
fix_cmd = """
# Fix NGINX: change 5005 -> 5152 for API proxy
sed -i 's|proxy_pass http://127.0.0.1:5005;|proxy_pass http://127.0.0.1:5152;|g' /etc/nginx/sites-enabled/edusyncai

# Also add /uploads and /hubs locations to the HTTPS server block if missing
# Check if /uploads location already exists
if ! grep -q 'location /uploads' /etc/nginx/sites-enabled/edusyncai; then
    # Add /uploads and /hubs blocks right after the /api block in each server
    sed -i '/location \\/api {/,/}/{
        /}/a\\
\\
    location /uploads {\\
        proxy_pass http://127.0.0.1:5152;\\
        proxy_http_version 1.1;\\
        proxy_set_header Host $host;\\
        proxy_cache_bypass $http_upgrade;\\
    }\\
\\
    location /hubs {\\
        proxy_pass http://127.0.0.1:5152;\\
        proxy_http_version 1.1;\\
        proxy_set_header Upgrade $http_upgrade;\\
        proxy_set_header Connection "upgrade";\\
        proxy_set_header Host $host;\\
        proxy_cache_bypass $http_upgrade;\\
    }
    }' /etc/nginx/sites-enabled/edusyncai
fi

echo "=== TESTING NGINX CONFIG ==="
nginx -t

echo "=== RELOADING NGINX ==="
systemctl reload nginx

echo "=== VERIFYING CURL TO API ==="
curl -s -o /dev/null -w '%{http_code}' --connect-timeout 2 http://127.0.0.1:5152/api/ModelAssets
echo ""

echo "=== VERIFYING CURL THROUGH NGINX ==="
curl -s -o /dev/null -w '%{http_code}' --connect-timeout 2 http://127.0.0.1/api/ModelAssets -H "Host: 173-212-248-253.nip.io"
echo ""

echo "=== FINAL NGINX PROXY_PASS LINES ==="
grep proxy_pass /etc/nginx/sites-enabled/edusyncai

echo "=== DONE ==="
"""

_, stdout, stderr = client.exec_command(fix_cmd, timeout=15)
print(stdout.read().decode('utf-8', 'ignore'))
err = stderr.read().decode('utf-8', 'ignore')
if err:
    print("STDERR:", err)

client.close()
print("FIX APPLIED")
