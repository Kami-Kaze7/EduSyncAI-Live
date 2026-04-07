import paramiko
import os

def fix_backend():
    c = paramiko.SSHClient()
    c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    c.connect('173.212.248.253', username='root', password='viicsoft')
    
    print('Applying DB Schema on Remote Server...')
    sql_script = """
import sqlite3
import traceback
try:
    conn = sqlite3.connect('/opt/edusyncai/publish/Data/edusync.db')
    cur = conn.cursor()
    
    cur.execute("PRAGMA foreign_keys = OFF")
    
    cur.execute("PRAGMA table_info(Courses)")
    cols = [col[1] for col in cur.fetchall()]
    if 'YearOfStudyId' not in cols:
        cur.execute('ALTER TABLE Courses ADD YearOfStudyId INTEGER NULL')
        print('Added YearOfStudyId to Courses.')
        
    cur.execute('''CREATE TABLE IF NOT EXISTS Faculties (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        CreatedAt TEXT NOT NULL
    )''')
    
    cur.execute('''CREATE TABLE IF NOT EXISTS Departments (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        FacultyId INTEGER NOT NULL,
        Name TEXT NOT NULL,
        FOREIGN KEY (FacultyId) REFERENCES Faculties (Id) ON DELETE CASCADE
    )''')
    
    cur.execute('''CREATE TABLE IF NOT EXISTS YearsOfStudy (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        DepartmentId INTEGER NOT NULL,
        Name TEXT NOT NULL,
        Level INTEGER NOT NULL,
        FOREIGN KEY (DepartmentId) REFERENCES Departments (Id) ON DELETE CASCADE
    )''')
    
    conn.commit()
    print('Database schema updated successfully.')
except Exception as e:
    print('Error:', e)
    traceback.print_exc()
"""
    
    sftp = c.open_sftp()
    
    with sftp.open('/tmp/run_sql.py', 'w') as f:
        f.write(sql_script)
        
    stdin, stdout, stderr = c.exec_command('python3 /tmp/run_sql.py')
    print('SQL Out:', stdout.read().decode())
    print('SQL Err:', stderr.read().decode())
    
    print('Uploading new DLL...')
    local_dll = r'C:\tmp\pub\EduSyncAI.WebAPI.dll'
    remote_dll = '/opt/edusyncai/publish/webapi/EduSyncAI.WebAPI.dll'
    sftp.put(local_dll, remote_dll)
    sftp.close()
    
    print('Restarting backend process...')
    stdin, stdout, stderr = c.exec_command('kill -9 $(lsof -t -i:5152)')
    print(stdout.read().decode())
    
    # Also reload PM2 just in case! 
    # (Since I killed the port, if systemd or pm2 manages it, it will bounce back up automatically.)
    
    c.close()
    print('Finished deployed WebAPI!')

if __name__ == '__main__':
    fix_backend()
