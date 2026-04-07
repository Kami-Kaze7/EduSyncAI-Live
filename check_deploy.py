import paramiko
import sys

def check_deploy_user():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "Getting deploy user CWD..."
        lsof -a -U -u deploy -c node | head -n 5
        pwdx $(pgrep -u deploy node | head -n 1)
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_deploy_user()
