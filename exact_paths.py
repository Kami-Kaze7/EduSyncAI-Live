import paramiko

def exact_paths():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        stdin, stdout, stderr = client.exec_command('ls -d /home/deploy/*')
        lines = stdout.read().decode('utf-8').splitlines()
        print("HOME DEPLOY DIRS:")
        for line in lines:
            print(line)
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    exact_paths()
