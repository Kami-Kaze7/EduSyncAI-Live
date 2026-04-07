import paramiko

def get_pm2_logs():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        client.exec_command('pm2 logs edusync-web --lines 50 --nostream > /tmp/pm2_logs.txt')
        import time
        time.sleep(2)
        
        sftp = client.open_sftp()
        sftp.get('/tmp/pm2_logs.txt', 'pm2_logs.txt')
        sftp.close()
        print("LOGS DOWNLOADED")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    get_pm2_logs()
