import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmd = """
echo "=== ENV FILE ==="
cat /opt/edusyncai/edusync-web/.env* 2>/dev/null
echo ""
echo "=== NEXT_PUBLIC_API_URL IN BUILD ==="
grep -r "NEXT_PUBLIC_API_URL" /opt/edusyncai/edusync-web/.env* 2>/dev/null
echo ""
echo "=== CHECK .next/BUILD ==="
grep -r "5152" /opt/edusyncai/edusync-web/.next/server/app/ 2>/dev/null | head -3
grep -r "localhost:5152" /opt/edusyncai/edusync-web/.next/ 2>/dev/null | head -5
echo ""
echo "=== CONTROLLER SOURCE ==="
cat /opt/edusyncai/publish/webapi/EduSyncAI.WebAPI.dll 2>/dev/null | strings | grep -i "uploads/models" | head -5
echo ""
echo "=== MODELS DIR EXISTS? ==="
ls -la /opt/edusyncai/Data/uploads/models/ 2>/dev/null || echo "DIR DOES NOT EXIST"
echo "DONE"
"""

_, stdout, _ = client.exec_command(cmd, timeout=10)
result = stdout.read().decode('utf-8', 'ignore')
print(result)
client.close()
