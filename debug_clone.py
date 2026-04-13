import paramiko, sys

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

def run(ssh, cmd, timeout=300):
    print(f"\n>>> {cmd}", flush=True)
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode('utf-8', errors='replace')
    err = stderr.read().decode('utf-8', errors='replace')
    ec = stdout.channel.recv_exit_status()
    if out.strip():
        for l in out.strip().split('\n')[-40:]:
            print(f"  {l}", flush=True)
    if err.strip():
        for l in err.strip().split('\n')[-15:]:
            print(f"  [ERR] {l}", flush=True)
    print(f"  [EXIT: {ec}]", flush=True)
    return out, err, ec

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)
print("Connected!", flush=True)

# Check what failed with clone
print("\n=== DEBUG CLONE ===", flush=True)
run(ssh, "ls -la /opt/edusyncai/ 2>&1 || echo 'Dir does not exist'")
run(ssh, "git clone https://github.com/Kami-Kaze7/EduSyncAI-Live.git /tmp/test_clone 2>&1 | tail -5", timeout=60)

ssh.close()
