import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmds = [
    ("DIRECT_API_5152", "curl -s --connect-timeout 3 http://127.0.0.1:5152/api/ModelAssets 2>&1"),
    ("VIA_NGINX_HTTPS", "curl -sk --connect-timeout 3 https://173-212-248-253.nip.io/api/ModelAssets 2>&1"),
    ("NGINX_PROXY_LINES", "grep proxy_pass /etc/nginx/sites-enabled/edusyncai"),
    ("SWAGGER_CHECK", "curl -s -o /dev/null -w '%{http_code}' --connect-timeout 3 http://127.0.0.1:5152/swagger/index.html"),
]

for label, cmd in cmds:
    _, stdout, _ = client.exec_command(cmd, timeout=8)
    out = stdout.read().decode('utf-8','ignore').strip()
    print(f"\n=== {label} ===")
    print(out[:500])

client.close()
