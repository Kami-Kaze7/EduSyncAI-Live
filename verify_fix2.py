import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

# Test the API directly, through nginx, and check the controller exists
cmd = """
echo "TEST1_DIRECT:"
curl -s --connect-timeout 3 http://127.0.0.1:5152/api/ModelAssets
echo ""
echo "TEST2_HTTPS:"
curl -sk --connect-timeout 3 https://173-212-248-253.nip.io/api/ModelAssets
echo ""
echo "TEST3_POST_CHECK:"
curl -s -o /dev/null -w '%{http_code}' --connect-timeout 3 -X POST http://127.0.0.1:5152/api/ModelAssets -F "title=test" -F "discipline=test" -F "modelFile=@/dev/null"
echo ""
echo "TEST4_CONTROLLER_DLL:"
strings /opt/edusyncai/publish/webapi/EduSyncAI.WebAPI.dll 2>/dev/null | grep -i "ModelAsset" | head -5
echo ""
echo "TEST5_UPLOADS_DIR:"
ls -la /opt/edusyncai/Data/uploads/ 2>/dev/null | head -10
echo "DONE"
"""

_, stdout, stderr = client.exec_command(cmd, timeout=15)
result = stdout.read().decode('utf-8', 'ignore')
with open("api_test_results.txt", "w", encoding="utf-8") as f:
    f.write(result)
print(result)
client.close()
