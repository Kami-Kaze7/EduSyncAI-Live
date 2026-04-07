import paramiko
import time

def full_diagnosis():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    client.connect('173.212.248.253', username='root', password='viicsoft')

    checks = [
        ("1. SYSTEMD SERVICE STATUS", "systemctl status edusyncai-api.service --no-pager -l 2>&1 | head -30"),
        ("2. WHAT IS LISTENING ON PORT 5000", "ss -tlnp | grep 5000"),
        ("3. WHAT IS LISTENING ON PORT 5005", "ss -tlnp | grep 5005"),
        ("4. WHAT IS LISTENING ON PORT 5152", "ss -tlnp | grep 5152"),
        ("5. ALL DOTNET PROCESSES", "ps aux | grep -i dotnet | grep -v grep"),
        ("6. ALL EduSyncAI PROCESSES", "ps aux | grep -i EduSync | grep -v grep"),
        ("7. NGINX CONFIG - API PROXY", "grep -n proxy_pass /etc/nginx/sites-enabled/edusyncai 2>/dev/null || grep -n proxy_pass /etc/nginx/sites-enabled/default 2>/dev/null"),
        ("8. SYSTEMD SERVICE FILE CONTENTS", "cat /etc/systemd/system/edusyncai-api.service 2>/dev/null"),
        ("9. PM2 LIST", "pm2 list 2>/dev/null"),
        ("10. CURL TEST PORT 5000", "curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5000/api/ModelAssets 2>&1"),
        ("11. CURL TEST PORT 5005", "curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5005/api/ModelAssets 2>&1"),
        ("12. CURL TEST PORT 5152", "curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5152/api/ModelAssets 2>&1"),
        ("13. CURL FULL RESPONSE PORT 5000", "curl -s http://127.0.0.1:5000/api/ModelAssets 2>&1 | head -20"),
        ("14. BACKEND DEPLOY DIR CONTENTS", "ls -la /opt/edusyncai/publish/webapi/ 2>/dev/null | head -20"),
        ("15. BACKEND LINUX DEPLOY DIR", "ls -la /opt/edusyncai/backend_linux_deploy/ 2>/dev/null | head -20"),
        ("16. CHECK ModelAssetsController IN PUBLISH", "find /opt/edusyncai/ -name '*.dll' | xargs strings 2>/dev/null | grep -i ModelAssets | head -10"),
        ("17. NGINX FULL CONFIG", "cat /etc/nginx/sites-enabled/edusyncai 2>/dev/null"),
        ("18. DEPLOY USER PM2", "su - deploy -c 'pm2 list' 2>/dev/null"),
    ]

    results = []
    for title, cmd in checks:
        _, stdout, stderr = client.exec_command(cmd, timeout=10)
        try:
            out = stdout.read().decode('utf-8', 'ignore').strip()
            err = stderr.read().decode('utf-8', 'ignore').strip()
        except:
            out = ""
            err = ""
        results.append(f"\n{'='*60}\n{title}\n{'='*60}\n{out}\n{err if err else ''}")

    client.close()
    
    with open("full_diagnosis_results.txt", "w", encoding="utf-8") as f:
        f.write("\n".join(results))
    print("DIAGNOSIS COMPLETE - saved to full_diagnosis_results.txt")

if __name__ == '__main__':
    full_diagnosis()
