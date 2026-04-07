import paramiko
import sys

def fix_api_zombie():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "KILLING ZOMBIE API PROCESSES ON PORT 5000..."
        fuser -k 5000/tcp || true
        killall -9 EduSyncAI || true
        
        echo "CLEANING REDUNDANT PM2 API INSTANCES..."
        pm2 delete edusync-api || true
        
        echo "RESTARTING THE TRUE API..."
        pm2 restart edusyncai-webapi
        pm2 save
        
        echo "WAITING 5 SECONDS TO VERIFY IT BOOTS..."
        sleep 5
        pm2 logs edusyncai-webapi --lines 10 --nostream
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    fix_api_zombie()
