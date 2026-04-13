import paramiko, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def get(cmd, t=30):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    out = stdout.read().decode('utf-8', errors='replace').strip()
    err = stderr.read().decode('utf-8', errors='replace').strip()
    return f"CMD: {cmd}\nSTDOUT:\n{out}\nSTDERR:\n{err}\n{'='*50}"

results = []
results.append(get("dpkg --configure -a"))
results.append(get("apt-get install -f -y"))
results.append(get("apt-get update -y"))
results.append(get("node --version"))

ssh.close()

with open(r"C:\EduSyncAI\apt_diag.txt", "w", encoding="utf-8") as f:
    f.write('\n'.join(results))

print("Diagnostics written to apt_diag.txt")
