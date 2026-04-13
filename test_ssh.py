import paramiko
import sys

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

print(f"Testing SSH to {HOST}...", flush=True)
ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
try:
    ssh.connect(HOST, username=USER, password=PASS, timeout=30)
    print("Connected!", flush=True)
    stdin, stdout, stderr = ssh.exec_command("hostname && uname -a && uptime", timeout=30)
    print(stdout.read().decode(), flush=True)
    print(stderr.read().decode(), flush=True)
    ssh.close()
    print("SSH test passed!", flush=True)
except Exception as e:
    print(f"SSH FAILED: {e}", flush=True)
