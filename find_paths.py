import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

client.exec_command('find / -maxdepth 6 -type f -iname "*dashboard*" > /tmp/paths.txt')
import time
time.sleep(5)
sftp = client.open_sftp()
sftp.get('/tmp/paths.txt', r'C:\EduSyncAI\paths.txt')
client.close()
