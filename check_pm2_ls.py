import paramiko

def check_pm2_ls():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "WHAT IS RUNNING IN PM2 ROOT?"
        pm2 ls
        echo "WHAT IS RUNNING ON PORT 3000 RIGHT NOW?"
        lsof -i :3000
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_pm2_ls()
