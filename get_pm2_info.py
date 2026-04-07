import paramiko
import json

def get_pm2_info():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        stdin, stdout, stderr = client.exec_command('pm2 jlist > /tmp/pm2_clean.json')
        client.exec_command('lsof -i :3000 > /tmp/lsof_3000.txt')
        client.exec_command('lsof -i :3002 > /tmp/lsof_3002.txt')
        
        import time
        time.sleep(2)
        sftp = client.open_sftp()
        sftp.get('/tmp/pm2_clean.json', 'pm2_clean.json')
        sftp.get('/tmp/lsof_3000.txt', 'lsof_3000.txt')
        sftp.get('/tmp/lsof_3002.txt', 'lsof_3002.txt')
        sftp.close()
        print("DOWNLOAD DONE")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    get_pm2_info()
