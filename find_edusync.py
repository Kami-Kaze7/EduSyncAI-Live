import paramiko
import sys

def find_edusync():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "WHAT IS LISTENING ON PORT 3000?"
        lsof -i :3000
        
        echo "WHAT NODE PROCESSES IS DEPLOY RUNNING?"
        ps aux | grep node | grep deploy
        
        echo "FINDING EDUSYNC WEB CWD FOR DEPLOY USER..."
        find /home/deploy -maxdepth 5 -type d -name "edusync-web" 2>/dev/null
        find /home/deploy -maxdepth 5 -type d -name "app" 2>/dev/null | grep admin
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    find_edusync()
