import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

cmd = """
echo "=== CURRENT API DB PATH (from journal) ==="
journalctl -u edusyncai-api.service --no-pager -n 30 | grep "Database:"

echo ""
echo "=== ALL edusync.db FILES ON SERVER ==="
find /opt/edusyncai/ -name "edusync.db" -exec ls -la {} \;

echo ""
echo "=== CHECK /opt/edusyncai/Data/ ==="
ls -la /opt/edusyncai/Data/ 2>/dev/null

echo ""
echo "=== CHECK /opt/edusyncai/publish/Data/ ==="
ls -la /opt/edusyncai/publish/Data/ 2>/dev/null

echo ""
echo "=== CHECK /opt/edusyncai/publish/webapi/../Data/ ==="
ls -la /opt/edusyncai/publish/webapi/../Data/ 2>/dev/null

echo ""
echo "=== OLD DB - table counts ==="
if [ -f /opt/edusyncai/Data/edusync.db ]; then
    echo "OLD DB EXISTS at /opt/edusyncai/Data/edusync.db"
    sqlite3 /opt/edusyncai/Data/edusync.db "SELECT 'Lecturers:', COUNT(*) FROM Lecturers; SELECT 'Students:', COUNT(*) FROM Students; SELECT 'Courses:', COUNT(*) FROM Courses; SELECT 'ModelAssets:', COUNT(*) FROM ModelAssets;" 2>/dev/null
else
    echo "OLD DB DOES NOT EXIST"
fi

echo ""
echo "=== NEW DB - table counts ==="
if [ -f /opt/edusyncai/publish/Data/edusync.db ]; then
    echo "NEW DB EXISTS at /opt/edusyncai/publish/Data/edusync.db"
    sqlite3 /opt/edusyncai/publish/Data/edusync.db "SELECT 'Lecturers:', COUNT(*) FROM Lecturers; SELECT 'Students:', COUNT(*) FROM Students; SELECT 'Courses:', COUNT(*) FROM Courses; SELECT 'ModelAssets:', COUNT(*) FROM ModelAssets;" 2>/dev/null
else
    echo "NEW DB DOES NOT EXIST"
fi

echo "DONE"
"""

_, stdout, _ = client.exec_command(cmd, timeout=15)
result = stdout.read().decode('utf-8', 'ignore')
with open("db_check.txt", "w", encoding="utf-8") as f:
    f.write(result)
print("SAVED")
client.close()
