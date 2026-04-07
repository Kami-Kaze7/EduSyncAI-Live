import paramiko
c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft')
sftp = c.open_sftp()
sql_script = """
import sqlite3
import traceback
try:
    conn = sqlite3.connect('/opt/edusyncai/publish/Data/edusync.db')
    cur = conn.cursor()
    print("Creating CourseVideos table if not exists...")
    cur.execute('''CREATE TABLE IF NOT EXISTS CourseVideos (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CourseId INTEGER NOT NULL,
        Title TEXT NOT NULL,
        Description TEXT NULL,
        VideoUrl TEXT NOT NULL,
        OrderIndex INTEGER NOT NULL,
        AddedAt TEXT NOT NULL,
        FOREIGN KEY (CourseId) REFERENCES Courses (Id) ON DELETE CASCADE
    )''')
    cur.execute('CREATE INDEX IF NOT EXISTS IX_CourseVideos_CourseId ON CourseVideos (CourseId)')
    conn.commit()
    print("CourseVideos table created successfully!")
except Exception as e:
    print('Error:', e)
    traceback.print_exc()
"""
with sftp.open('/tmp/fix_coursevids.py', 'w') as f:
    f.write(sql_script)
sftp.close()

stdin, stdout, stderr = c.exec_command('python3 /tmp/fix_coursevids.py')
print('OUT:', stdout.read().decode())
print('ERR:', stderr.read().decode())
c.close()
