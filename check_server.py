import paramiko, sys

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

def run(ssh, cmd):
    print(f"\n>>> {cmd}", flush=True)
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=120)
    out = stdout.read().decode('utf-8', errors='replace')
    err = stderr.read().decode('utf-8', errors='replace')
    ec = stdout.channel.recv_exit_status()
    if out.strip():
        for l in out.strip().split('\n')[-30:]:
            print(f"  {l}", flush=True)
    if err.strip():
        for l in err.strip().split('\n')[-10:]:
            print(f"  [ERR] {l}", flush=True)
    print(f"  [EXIT: {ec}]", flush=True)
    return out, err, ec

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)
print("Connected!", flush=True)

# Check what's installed
run(ssh, "which dotnet || echo 'dotnet NOT found'; $HOME/.dotnet/dotnet --version 2>/dev/null || echo '.dotnet version check failed'")
run(ssh, "which node && node --version || echo 'node NOT found'")
run(ssh, "which python3 && python3 --version || echo 'python3 NOT found'")
run(ssh, "which nginx && nginx -v 2>&1 || echo 'nginx NOT found'")
run(ssh, "which certbot && certbot --version 2>&1 || echo 'certbot NOT found'")
run(ssh, "ls /opt/edusyncai/ 2>/dev/null || echo '/opt/edusyncai NOT found'")
run(ssh, "ls /opt/edusyncai/Data/edusync.db 2>/dev/null || echo 'DB NOT found'")

# Fix dpkg if needed
run(ssh, "dpkg --configure -a")
run(ssh, "apt --fix-broken install -y")

ssh.close()
