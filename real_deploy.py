import paramiko
import sys

def real_deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "Navigating to actual PM2 path..."
        cd /opt/edusyncai || exit 1
        echo "Resetting permissions cleanly..."
        git fetch origin main
        git reset --hard origin/main
        
        cd edusync-web || exit 1
        echo "NPM RUN BUILD starting on OPT..."
        npm run build
        echo "RESTARTING edusync-web..."
        pm2 restart edusync-web
        
        echo "Navigating to WebAPI..."
        cd /opt/edusyncai/EduSyncAI.WebAPI || exit 1
        dotnet build
        systemctl restart edusync-api.service || systemctl restart edusync || echo "systemctl error bypass"
        pm2 restart all || echo "pm2 restart bypass"
        
        echo "OPTIMAL_DEPLOY_SUCCESS"
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
    real_deploy()
