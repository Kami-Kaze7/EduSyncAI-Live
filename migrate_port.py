import paramiko

def migrate_to_5005():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "UPDATING PM2 ENVIRONMENT"
        pm2 stop edusyncai-webapi
        
        # Recreate the PM2 instance with the explicit environment variable
        cd /opt/edusyncai/backend_linux_deploy
        pm2 delete edusyncai-webapi || true
        ASPNETCORE_URLS="http://127.0.0.1:5005" pm2 start ./EduSyncAI.WebAPI --name "edusyncai-webapi"
        pm2 save
        
        echo "UPDATING NGINX PROXY PASS"
        sed -i 's/127.0.0.1:5000/127.0.0.1:5005/g' /etc/nginx/sites-enabled/edusyncai
        sed -i 's/localhost:5000/127.0.0.1:5005/g' /etc/nginx/sites-enabled/edusyncai
        sed -i 's/127.0.0.1:5000/127.0.0.1:5005/g' /etc/nginx/sites-enabled/default
        sed -i 's/localhost:5000/127.0.0.1:5005/g' /etc/nginx/sites-enabled/default
        
        nginx -t && systemctl reload nginx
        
        sleep 3
        echo "TESTING API ON 5005"
        curl -s -v http://127.0.0.1:5005/api/ModelAssets 2>&1
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    migrate_to_5005()
