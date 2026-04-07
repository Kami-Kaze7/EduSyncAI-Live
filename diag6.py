import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmd = """
echo "=== .env files ==="
cat /opt/edusyncai/edusync-web/.env.local 2>/dev/null
echo "---"
cat /opt/edusyncai/edusync-web/.env.production 2>/dev/null
echo "---"
cat /opt/edusyncai/edusync-web/.env 2>/dev/null
echo ""
echo "=== next.config check ==="
cat /opt/edusyncai/edusync-web/next.config.* 2>/dev/null
echo ""
echo "=== Checking built JS for API URL ==="
grep -r 'NEXT_PUBLIC_API_URL\|localhost:5152' /opt/edusyncai/edusync-web/.next/static/ 2>/dev/null | head -5
echo ""
echo "=== Full POST test ==="
echo "test" > /tmp/test.obj
curl -s -w '\nHTTP_CODE:%{http_code}' --connect-timeout 5 -X POST http://127.0.0.1:5152/api/ModelAssets \
  -F "title=Test Model" \
  -F "description=Test" \
  -F "discipline=Biology" \
  -F "modelFile=@/tmp/test.obj;type=application/octet-stream"
echo ""
echo "DONE"
"""

_, stdout, _ = client.exec_command(cmd, timeout=15)
result = stdout.read().decode('utf-8', 'ignore')
print(result)
client.close()
