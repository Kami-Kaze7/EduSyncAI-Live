import paramiko
import os
import sys

def true_sftp_deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        print("Connecting to Contabo...")
        client.connect('173.212.248.253', username='root', password='viicsoft')
        sftp = client.open_sftp()
        
        print("Transferring page.tsx directly to deploy user's directory...")
        sftp.put(r'C:\EduSyncAI\edusync-web\app\admin\dashboard\page.tsx', '/home/deploy/viicsofteom/edusync-web/app/admin/dashboard/page.tsx')
        
        print("Transferring adminApi.ts...")
        sftp.put(r'C:\EduSyncAI\edusync-web\lib\adminApi.ts', '/home/deploy/viicsofteom/edusync-web/lib/adminApi.ts')
        
        sftp.close()
        
        command = """
        echo "FIXING PERMISSIONS FOR DEPLOY USER..."
        chown -R deploy:deploy /home/deploy/viicsofteom/edusync-web
        
        echo "SWITCHING TO DEPLOY USER TO RUN BUILD..."
        su - deploy -c "cd /home/deploy/viicsofteom/edusync-web && npm run build && pm2 restart all"
        
        echo "TRUE_DEPLOY_SUCCESS"
        """
        
        print("Executing remote rebuild commands as deploy user...")
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
    except Exception as e:
        print(f"Failed true deploy: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    true_sftp_deploy()
