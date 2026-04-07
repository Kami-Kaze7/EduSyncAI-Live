import paramiko

def debug_deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "Looking for all edusync-web folders..."
        find / -maxdepth 5 -type d -name "edusync-web" 2>/dev/null
        echo "Looking for all .git folders..."
        find / -maxdepth 5 -type d -name ".git" 2>/dev/null | grep -i edusync
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line, end="")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    debug_deploy()
