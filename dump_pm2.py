import paramiko
import sys

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

client.exec_command('pm2 jlist > /tmp/pm2_dump.json')
import time
time.sleep(3)
sftp = client.open_sftp()
sftp.get('/tmp/pm2_dump.json', r'C:\EduSyncAI\pm2_dump.json')

sftp.close()
client.close()
