import paramiko
c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft')
sftp = c.open_sftp()
sql_script = """
import sqlite3
try:
    conn = sqlite3.connect('/opt/edusyncai/publish/Data/edusync.db')
    cur = conn.cursor()
    cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='CourseVideos'")
    print("CourseVideos exists?", len(cur.fetchall()) > 0)
except Exception as e:
    print('Error:', e)
"""
with sftp.open('/tmp/check_coursevids.py', 'w') as f:
    f.write(sql_script)
sftp.close()

stdin, stdout, stderr = c.exec_command('python3 /tmp/check_coursevids.py')
print('OUT:', stdout.read().decode())
print('ERR:', stderr.read().decode())
c.close()
