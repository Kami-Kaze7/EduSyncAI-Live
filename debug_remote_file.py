import paramiko
c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft')
stdin, stdout, stderr = c.exec_command('find /opt/edusyncai/publish -name "CourseVideosTab.tsx"')
match = stdout.read().decode().strip().split('\n')[0]
if match:
    stdin, stdout, stderr = c.exec_command(f'cat {match} | head -n 115 | tail -n 35')
    print(stdout.read().decode())
else:
    print("Not found")
