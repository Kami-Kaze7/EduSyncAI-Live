import paramiko

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

def run(ssh, cmd, timeout=30):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode('utf-8', errors='replace').strip()
    err = stderr.read().decode('utf-8', errors='replace').strip()
    ec = stdout.channel.recv_exit_status()
    return out, err, ec

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

# Check services
api_out, _, _ = run(ssh, "systemctl is-active edusyncai-api")
web_out, _, _ = run(ssh, "systemctl is-active edusyncai-web")
face_out, _, _ = run(ssh, "systemctl is-active edusyncai-face")
print(f"API service:  {api_out}", flush=True)
print(f"Web service:  {web_out}", flush=True)
print(f"Face service: {face_out}", flush=True)

# Check HTTP
api_http, _, _ = run(ssh, "curl -s -o /dev/null -w '%{http_code}' http://localhost:5152/api/courses")
web_http, _, _ = run(ssh, "curl -s -o /dev/null -w '%{http_code}' http://localhost:3000")
print(f"\nAPI HTTP:  {api_http}", flush=True)
print(f"Web HTTP:  {web_http}", flush=True)

# Check ports
ports_out, _, _ = run(ssh, "ss -tlnp | grep -E '5152|3000|5001|80|443'")
print(f"\nListening ports:\n{ports_out}", flush=True)

# Check SSL
ssl_out, ssl_err, _ = run(ssh, "certbot certificates 2>&1")
print(f"\nSSL:\n{ssl_out[:500]}", flush=True)

# Check DB
db_out, _, _ = run(ssh, "ls -lh /opt/edusyncai/Data/edusync.db 2>&1")
print(f"\nDB: {db_out}", flush=True)

# Check API DLL
dll_out, _, _ = run(ssh, "ls -lh /opt/edusyncai/publish/api/EduSyncAI.WebAPI.dll 2>&1")
print(f"DLL: {dll_out}", flush=True)

# Quick API test
api_test, _, _ = run(ssh, "curl -s http://localhost:5152/api/courses | head -c 200")
print(f"\nAPI Response: {api_test[:200]}", flush=True)

# Check API logs for errors
api_logs, _, _ = run(ssh, "journalctl -u edusyncai-api --no-pager -n 10 2>&1")
print(f"\nAPI Logs:\n{api_logs}", flush=True)

ssh.close()
