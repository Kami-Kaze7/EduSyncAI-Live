import paramiko

def check_backend_logs():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "WHAT ARE THE LATEST BACKEND LOG ERRORS?"
        pm2 logs edusyncai-webapi --lines 40 --nostream
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_backend_logs()
