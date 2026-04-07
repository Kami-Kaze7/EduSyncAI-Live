import paramiko

def check_pm2_root_port():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        stdin, stdout, stderr = client.exec_command('netstat -ltnp | grep node')
        print("Root ports:")
        print(stdout.read().decode())
        
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_pm2_root_port()
