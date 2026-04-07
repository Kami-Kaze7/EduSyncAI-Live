import paramiko
import sys

def final_deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "Navigating to true production directory..."
        cd /var/www/edusyncai || exit 1
        echo "Resetting permissions cleanly..."
        git fetch origin main
        git reset --hard origin/main
        
        cd edusync-web || exit 1
        echo "NPM RUN BUILD starting..."
        npm run build
        echo "RESTARTING edusync-web..."
        pm2 restart edusync-web
        
        echo "Navigating to WebAPI..."
        cd /var/www/edusyncai/EduSyncAI.WebAPI || exit 1
        dotnet build
        systemctl restart edusync-api.service || systemctl restart edusync
        
        echo "FINAL_DEPLOY_SUCCESS"
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
        print("Done Python.")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    final_deploy()
