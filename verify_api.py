import paramiko

def verify_api():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "PM2 API LOGS:"
        pm2 logs edusync-api --lines 20 --nostream
        echo "WHAT IS LISTENING ON PORT 5000?"
        lsof -i :5000
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    verify_api()
