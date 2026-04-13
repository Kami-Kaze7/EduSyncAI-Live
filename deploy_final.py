import paramiko, sys, io, time
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def run(cmd):
    print(f"\n>>> {cmd}", flush=True)
    stdin, stdout, stderr = ssh.exec_command(cmd)
    ec = stdout.channel.recv_exit_status()
    out = stdout.read().decode('utf-8', errors='replace')
    err = stderr.read().decode('utf-8', errors='replace')
    for line in out.strip().split('\n')[-30:]:
        clean = ''.join(c if ord(c) < 128 else '?' for c in line)
        if clean.strip():
            print(f"  {clean}", flush=True)
    for line in err.strip().split('\n')[-30:]:
        clean = ''.join(c if ord(c) < 128 else '?' for c in line)
        if clean.strip():
            print(f"  [ERR] {clean}", flush=True)
    print(f"  [EXIT: {ec}]", flush=True)
    return ec

print("=== REBUILD NEXT.JS ===")
run("cd /opt/edusyncai/edusync-web && rm -rf .next node_modules")
run("cd /opt/edusyncai/edusync-web && npm install --no-audit --no-fund")
run("cd /opt/edusyncai/edusync-web && lsof -t -i:3000 | xargs kill -9 2>/dev/null; npm run build")

run("systemctl restart edusyncai-web")
run("systemctl restart edusyncai-api")

print("=== FIX NGINX / SSL ===")
# Generate cert with certbot
run("certbot --nginx -d 62-171-138-230.nip.io --non-interactive --agree-tos -m viicsoftdev@gmail.com")

# Sleep a bit to let services warm up
time.sleep(5)

print("=== VERIFY ===")
run("curl -s -o /dev/null -w 'API HTTP: %{http_code}' http://localhost:5152/api/courses")
run("curl -s -o /dev/null -w 'Web HTTP: %{http_code}' http://localhost:3000")

ssh.close()
