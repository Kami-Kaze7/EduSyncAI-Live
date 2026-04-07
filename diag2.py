import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmds = {
    "NGINX_API_PROXY": "grep -A2 -B2 'api' /etc/nginx/sites-enabled/edusyncai 2>/dev/null || grep -A2 -B2 'api' /etc/nginx/sites-enabled/default 2>/dev/null",
    "NGINX_ALL_PROXY": "grep proxy_pass /etc/nginx/sites-enabled/edusyncai 2>/dev/null || grep proxy_pass /etc/nginx/sites-enabled/default 2>/dev/null",
    "SYSTEMD_SERVICE_FILE": "cat /etc/systemd/system/edusyncai-api.service 2>/dev/null",
    "CURL_5005_MODELS": "curl -s --connect-timeout 2 http://127.0.0.1:5005/api/ModelAssets 2>&1",
    "CURL_5005_SWAGGER": "curl -s -o /dev/null -w '%{http_code}' --connect-timeout 2 http://127.0.0.1:5005/swagger/index.html 2>&1",
}

for label, cmd in cmds.items():
    _, stdout, stderr = client.exec_command(cmd, timeout=8)
    out = stdout.read().decode('utf-8','ignore').strip()
    print(f"\n=== {label} ===")
    print(out)

client.close()
