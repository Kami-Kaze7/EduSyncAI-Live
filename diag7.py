import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmd = """
echo "=== .env.local ==="
cat /opt/edusyncai/edusync-web/.env.local 2>/dev/null
echo ""
echo "=== .env.production ==="
cat /opt/edusyncai/edusync-web/.env.production 2>/dev/null
echo ""
echo "=== .env ==="
cat /opt/edusyncai/edusync-web/.env 2>/dev/null
echo ""
echo "=== next.config ==="
cat /opt/edusyncai/edusync-web/next.config.mjs 2>/dev/null || cat /opt/edusyncai/edusync-web/next.config.js 2>/dev/null || cat /opt/edusyncai/edusync-web/next.config.ts 2>/dev/null
echo ""
echo "=== Built client chunks with API URL ==="
grep -rl 'localhost:5152' /opt/edusyncai/edusync-web/.next/static/chunks/ 2>/dev/null | head -5
echo ""
echo "=== VERIFY GET after POST ==="
curl -s --connect-timeout 3 http://127.0.0.1:5152/api/ModelAssets
echo ""
echo "DONE"
"""

_, stdout, _ = client.exec_command(cmd, timeout=15)
result = stdout.read().decode('utf-8', 'ignore')
with open("env_check.txt", "w", encoding="utf-8") as f:
    f.write(result)
print("SAVED")
client.close()
