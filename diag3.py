import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmds = [
    ("SERVICE_FILE", "cat /etc/systemd/system/edusyncai-api.service"),
    ("JOURNAL_LAST_30", "journalctl -u edusyncai-api.service --no-pager -n 30"),
    ("ALL_LISTENING_PORTS", "ss -tlnp | head -20"),
]

for label, cmd in cmds:
    _, stdout, stderr = client.exec_command(cmd, timeout=8)
    out = stdout.read().decode('utf-8','ignore').strip()
    print(f"\n{'='*50}")
    print(f"  {label}")
    print(f"{'='*50}")
    print(out[:2000])

client.close()
