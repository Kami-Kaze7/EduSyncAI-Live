import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

big_cmd = """
echo "===SERVICE_FILE==="
cat /etc/systemd/system/edusyncai-api.service
echo "===JOURNAL==="
journalctl -u edusyncai-api.service --no-pager -n 30
echo "===NGINX_CONF==="
cat /etc/nginx/sites-enabled/edusyncai 2>/dev/null || cat /etc/nginx/sites-enabled/default
echo "===LISTENING_PORTS==="
ss -tlnp
echo "===END==="
"""

_, stdout, _ = client.exec_command(big_cmd, timeout=10)
data = stdout.read().decode('utf-8', 'ignore')

sftp = client.open_sftp()
with open("server_state.txt", "w", encoding="utf-8") as f:
    f.write(data)

client.close()
print("SAVED to server_state.txt")
