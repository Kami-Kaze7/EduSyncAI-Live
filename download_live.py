import paramiko
import sys

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

sftp = client.open_sftp()
try:
    sftp.get('/var/www/edusyncai/edusync-web/app/admin/dashboard/page.tsx', r'C:\tmp\remote_page.tsx')
    print("DOWNLOADED")
except Exception as e:
    print(f"FAILED TO DOWNLOAD: {e}")
finally:
    sftp.close()
    client.close()
