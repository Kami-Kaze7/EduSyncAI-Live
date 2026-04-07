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
cur.execute('SELECT Id, CourseCode, CourseName FROM Courses')
courses = [dict(row) for row in cur.fetchall()]
print("COURSES:")
for c in courses: print(c)

print("-----")
cur.execute('SELECT s.Id, s.MatricNumber, e.CourseId FROM Students s JOIN CourseEnrollments e ON s.Id = e.StudentId')
enrolls = [dict(row) for row in cur.fetchall()]
print("ENROLLS:")
for e in enrolls: print(e)
"""
with sftp.open('/tmp/debug_courses.py', 'w') as f:
    f.write(sql_script)
sftp.close()

stdin, stdout, stderr = c.exec_command('python3 /tmp/debug_courses.py')
print('OUT:', stdout.read().decode())
print('ERR:', stderr.read().decode())
c.close()
