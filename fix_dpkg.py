import paramiko, sys, io, time
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def run(cmd):
    print(f"\n>>> {cmd}")
    stdin, stdout, stderr = ssh.exec_command(cmd)
    out = stdout.read().decode('utf-8', errors='replace')
    err = stderr.read().decode('utf-8', errors='replace')
    print(f"STDOUT:\n{out[:500]}")
    print(f"STDERR:\n{err[:500]}")

run("killall apt apt-get dpkg 2>/dev/null")
run("rm -f /var/lib/apt/lists/lock /var/cache/apt/archives/lock /var/lib/dpkg/lock*")

run("dpkg --configure -a")
run("apt-get install -f -y")
run("apt-get remove -y nodejs npm libnode-dev; apt-get autoremove -y")
run("rm -rf /etc/apt/sources.list.d/nodesource.list*")
run("curl -fsSL https://deb.nodesource.com/setup_20.x | bash -")
run("apt-get install -y nodejs")

run("node --version")
ssh.close()
