import paramiko
import sys

def revert_nginx():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "REVERTING NGINX TO PORT 3000"
        sed -i 's/proxy_pass http:\/\/localhost:3002;/proxy_pass http:\/\/localhost:3000;/g' /etc/nginx/sites-enabled/edusyncai
        sed -i 's/proxy_pass http:\/\/localhost:3002;/proxy_pass http:\/\/localhost:3000;/g' /etc/nginx/sites-enabled/default 2>/dev/null
        
        nginx -t && systemctl reload nginx
        
        echo "RE-BINDING EDUSYNC-WEB TO PORT 3000 EXCLUSIVELY IN PM2"
        cd /opt/edusyncai/edusync-web || exit 1
        pm2 delete edusync-web
        pm2 start npm --name "edusync-web" -- start -- -p 3000
        pm2 save
        
        echo "NGINX_REVERTED_SUCCESSFULLY"
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    revert_nginx()
