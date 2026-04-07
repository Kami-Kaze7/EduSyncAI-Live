import paramiko
c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft')
sftp = c.open_sftp()
sql_script = """
import sqlite3
import json
conn = sqlite3.connect('/opt/edusyncai/publish/Data/edusync.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()
cur.execute("SELECT e.CourseId, c.CourseCode, c.CourseName FROM CourseEnrollments e JOIN Courses c ON e.CourseId=c.Id WHERE e.StudentId = 3")
enrolls = [dict(row) for row in cur.fetchall()]
print("JONATHON ENROLLS:")
for row in enrolls: print(row)
"""
with sftp.open('/tmp/test_jon.py', 'w') as f:
    f.write(sql_script)
sftp.close()

stdin, stdout, stderr = c.exec_command('python3 /tmp/test_jon.py')
print('OUT:', stdout.read().decode())
print('ERR:', stderr.read().decode())
c.close()
