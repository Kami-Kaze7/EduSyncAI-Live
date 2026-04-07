import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmd = """
cd /opt/edusyncai/edusync-web

echo "=== Setting NEXT_PUBLIC_API_URL for build ==="
export NEXT_PUBLIC_API_URL="https://173-212-248-253.nip.io/api"

echo "=== Rebuilding Next.js with production API URL ==="
npm run build 2>&1 | tail -20

echo "=== Restarting Next.js ==="
pm2 restart edusync-web
pm2 save

echo "=== Verifying build has correct URL ==="
sleep 3
grep -r '173-212-248-253.nip.io' .next/static/chunks/ 2>/dev/null | head -3

echo "REBUILD DONE"
"""

_, stdout, stderr = client.exec_command(cmd, timeout=120)
result = stdout.read().decode('utf-8', 'ignore')
with open("rebuild_result.txt", "w", encoding="utf-8") as f:
    f.write(result)
print("SAVED to rebuild_result.txt")
client.close()
