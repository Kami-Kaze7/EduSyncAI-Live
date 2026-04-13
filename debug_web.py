import paramiko

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
    return out, err

results = []

results.append("=== WEB SERVICE JOURNAL ===")
out, _ = get("journalctl -u edusyncai-web --no-pager -n 30 --output=cat 2>&1")
results.append(out)

results.append("\n=== .NEXT DIRECTORY ===")
out, _ = get("ls -la /opt/edusyncai/edusync-web/.next/ 2>&1 | head -10")
results.append(out)

results.append("\n=== NEXT CONFIG ===")
out, _ = get("cat /opt/edusyncai/edusync-web/next.config.* 2>&1 | head -30")
results.append(out)

results.append("\n=== ENV PRODUCTION ===")
out, _ = get("cat /opt/edusyncai/edusync-web/.env.production 2>&1")
results.append(out)

results.append("\n=== API DB CHECK ===")
out, _ = get("ls -lh /opt/edusyncai/publish/api/*.db 2>&1; ls -lh /opt/edusyncai/Data/*.db 2>&1")
results.append(out)

results.append("\n=== NODE VERSION ===")
out, _ = get("node --version && npm --version && npx --version")
results.append(out)

results.append("\n=== MANUAL NEXT START ===")
out, err = get("cd /opt/edusyncai/edusync-web && timeout 5 npx next start -p 3001 2>&1 || true", t=15)
results.append(out if out else err)

results.append("\n=== CERTBOT ATTEMPT ===")
out, err = get("certbot --nginx -d 62-171-138-230.nip.io --dry-run --non-interactive --agree-tos -m viicsoftdev@gmail.com 2>&1", t=60)
results.append(out if out else err)

ssh.close()

# Write to file
with open(r"C:\EduSyncAI\server_debug_output.txt", "w", encoding="utf-8") as f:
    f.write('\n'.join(results))

print("Results written to server_debug_output.txt")
