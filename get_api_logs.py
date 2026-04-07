import paramiko

def get_backend_logs():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        client.exec_command('pm2 logs edusyncai-webapi --lines 50 --nostream > /tmp/api_logs.txt')
        import time
        time.sleep(2)
        
        sftp = client.open_sftp()
        sftp.get('/tmp/api_logs.txt', 'downloaded_api_logs.txt')
        sftp.close()
        print("API LOGS DOWNLOADED")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    get_backend_logs()
