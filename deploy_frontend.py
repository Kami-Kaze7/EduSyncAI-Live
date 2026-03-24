import paramiko

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft', timeout=30, banner_timeout=30)

commands = [
    "cd /opt/edusyncai && git pull origin main",
    "cd /opt/edusyncai/edusync-web && npm install && rm -rf .next && npm run build",
    "pm2 restart all"
]

for cmd in commands:
    print(f"\n--- Running: {cmd} ---")
    stdin, stdout, stderr = c.exec_command(cmd, get_pty=True, timeout=300)
    while True:
        line = stdout.readline()
        if not line:
            break
        print(line, end="")
    exit_status = stdout.channel.recv_exit_status()
    if exit_status != 0:
        print(f"Command failed with exit code: {exit_status}")
        err = stderr.read().decode()
        if err:
            print(f"STDERR: {err}")
        break

print("\nDeployment complete!")
c.close()
