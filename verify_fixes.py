import paramiko

HOST = "62.171.138.230"
USER = "root"  
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def get(cmd, t=30):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    return stdout.read().decode('utf-8', errors='replace').strip()

r = []
r.append(f"API: {get('systemctl is-active edusyncai-api')}")
r.append(f"WEB: {get('systemctl is-active edusyncai-web')}")

# Check SSL cert
r.append(f"\nSSL cert: {get('certbot certificates 2>&1')[:500]}")

# Check nginx ssl config
r.append(f"\nNginx SSL paths:")
r.append(get("grep ssl_certificate /etc/nginx/sites-available/edusyncai"))

# Test admin login
r.append(f"\nAdmin login test:")
login_resp = get("""curl -s -X POST https://62-171-138-230.nip.io/api/admin/login -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin123"}'""")
r.append(login_resp[:500])

# Test HTTPS without -k flag (should work with LE cert)
r.append(f"\nHTTPS (no -k flag):")
r.append(get("curl -s -o /dev/null -w '%{http_code}' https://62-171-138-230.nip.io 2>&1"))

# Env check
r.append(f"\n.env.production:")
r.append(get("cat /opt/edusyncai/edusync-web/.env.production"))

ssh.close()

output = '\n'.join(r)
clean = ''.join(c if ord(c) < 128 else '?' for c in output)
with open(r"C:\EduSyncAI\final_verify.txt", "w") as f:
    f.write(clean)
print("Results written to final_verify.txt")
