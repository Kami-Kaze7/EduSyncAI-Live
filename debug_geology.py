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
cur.execute("SELECT Id, CourseCode, CourseTitle, YearOfStudyId FROM Courses WHERE CourseTitle LIKE '%Geology%'")
print("GEOLOGY TITLES:")
for row in cur.fetchall(): print(dict(row))
"""
with sftp.open('/tmp/check_geom.py', 'w') as f:
    f.write(sql_script)
sftp.close()

stdin, stdout, stderr = c.exec_command('python3 /tmp/check_geom.py')
print('OUT:', stdout.read().decode())
print('ERR:', stderr.read().decode())
c.close()
