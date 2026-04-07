import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmd = """
echo "=== Check if NEXT_PUBLIC_API_URL is baked into the build ==="
grep -r '173-212-248-253.nip.io' /opt/edusyncai/edusync-web/.next/static/chunks/ 2>/dev/null | head -3
echo "---"
grep -r 'localhost:5152' /opt/edusyncai/edusync-web/.next/static/chunks/ 2>/dev/null | head -3
echo ""

echo "=== Test HTTPS POST through NGINX ==="
echo "test_model_data" > /tmp/test2.obj
curl -sv --connect-timeout 5 -X POST https://173-212-248-253.nip.io/api/ModelAssets \
  -F "title=NGINX Test" \
  -F "description=Testing through NGINX" \
  -F "discipline=Physics" \
  -F "modelFile=@/tmp/test2.obj;type=application/octet-stream" 2>&1 | tail -20
echo ""
echo "DONE"
"""

_, stdout, _ = client.exec_command(cmd, timeout=15)
result = stdout.read().decode('utf-8', 'ignore')
print(result)
client.close()
