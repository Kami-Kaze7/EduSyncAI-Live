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
    return out

results = []

results.append("=== NODE VERSION ===")
results.append(get("node --version"))

results.append("\n=== SERVICE STATUS ===")
results.append("API:  " + get("systemctl is-active edusyncai-api"))
results.append("Web:  " + get("systemctl is-active edusyncai-web"))
results.append("Face: " + get("systemctl is-active edusyncai-face"))

results.append("\n=== HTTP STATUS ===")
results.append("API:  " + get("curl -s -o /dev/null -w '%{http_code}' http://localhost:5152/api/courses"))
results.append("Web:  " + get("curl -s -o /dev/null -w '%{http_code}' http://localhost:3000"))

results.append("\n=== PORTS LISTENING ===")
results.append(get("ss -tlnp | grep -E '3000|5152|80|443'"))

results.append("\n=== API RESPONSE (courses) ===")
results.append(get("curl -s http://localhost:5152/api/courses | head -c 500"))

results.append("\n=== .NEXT DIRECTORY ===")
results.append(get("ls -la /opt/edusyncai/edusync-web/.next/ 2>&1 | head -5"))

results.append("\n=== WEB SERVICE LOGS ===")
results.append(get("journalctl -u edusyncai-web --no-pager -n 10 --output=cat 2>&1"))

results.append("\n=== SSL CERTIFICATES ===")
results.append(get("certbot certificates 2>&1"))

results.append("\n=== DB CHECK ===")
results.append(get("ls -lh /opt/edusyncai/publish/api/Data/edusync.db 2>&1"))

ssh.close()

with open(r"C:\EduSyncAI\final_status.txt", "w", encoding="utf-8") as f:
    f.write('\n'.join(results))

print("Results written to final_status.txt")
