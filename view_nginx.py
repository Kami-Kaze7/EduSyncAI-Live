import paramiko
HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"
ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)
stdin, stdout, stderr = ssh.exec_command("cat /etc/nginx/sites-enabled/edusyncai")
out = stdout.read().decode('utf-8', errors='replace')
with open(r"C:\EduSyncAI\nginx_config_utf8.txt", "w", encoding="utf-8") as f:
    f.write(out)
ssh.close()
