import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmds = {
    "SYSTEMD_STATUS": "systemctl is-active edusyncai-api.service 2>&1",
    "PORT_5000": "ss -tlnp | grep 5000",
    "PORT_5005": "ss -tlnp | grep 5005",
    "PORT_5152": "ss -tlnp | grep 5152",
    "DOTNET_PROCS": "ps aux | grep -i [E]duSync",
    "CURL_5000": "curl -s -o /dev/null -w '%{http_code}' --connect-timeout 2 http://127.0.0.1:5000/api/ModelAssets 2>&1",
    "CURL_5152": "curl -s -o /dev/null -w '%{http_code}' --connect-timeout 2 http://127.0.0.1:5152/api/ModelAssets 2>&1",
}

for label, cmd in cmds.items():
    _, stdout, stderr = client.exec_command(cmd, timeout=5)
    out = stdout.read().decode('utf-8','ignore').strip()
    print(f"{label}: {out}")

client.close()
