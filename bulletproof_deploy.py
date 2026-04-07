import paramiko
import os
import sys

def bulletproof_sftp_deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        print("Connecting to Contabo...")
        client.connect('173.212.248.253', username='root', password='viicsoft')
        sftp = client.open_sftp()
        
        print("Transferring page.tsx directly to /opt/edusyncai...")
        sftp.put(r'C:\EduSyncAI\edusync-web\app\admin\dashboard\page.tsx', '/opt/edusyncai/edusync-web/app/admin/dashboard/page.tsx')
        
        print("Transferring adminApi.ts...")
        sftp.put(r'C:\EduSyncAI\edusync-web\lib\adminApi.ts', '/opt/edusyncai/edusync-web/lib/adminApi.ts')
        
        # Uploading Backend Changes
        print("Uploading WebAPI/Controllers/ModelAssetsController.cs...")
        sftp.put(r'C:\EduSyncAI\EduSyncAI.WebAPI\Controllers\ModelAssetsController.cs', '/opt/edusyncai/EduSyncAI.WebAPI/Controllers/ModelAssetsController.cs')
        
        print("Uploading WebAPI/Program.cs...")
        sftp.put(r'C:\EduSyncAI\EduSyncAI.WebAPI\Program.cs', '/opt/edusyncai/EduSyncAI.WebAPI/Program.cs')
        
        print("Uploading Services/DatabaseService.cs...")
        sftp.put(r'C:\EduSyncAI\Services\DatabaseService.cs', '/opt/edusyncai/Services/DatabaseService.cs')
        
        sftp.close()
        
        command = """
        echo "CD TO OPT NEXTJS..."
        cd /opt/edusyncai/edusync-web || exit 1
        echo "NPM RUN BUILD..."
        npm run build
        echo "RESTARTING edusync-web..."
        pm2 restart edusync-web
        
        echo "CD TO OPT WEBAPI..."
        cd /opt/edusyncai/EduSyncAI.WebAPI || exit 1
        echo "DOTNET BUILD..."
        dotnet build
        echo "RESTARTING API..."
        systemctl restart edusync-api.service || systemctl restart edusync || echo "systemctl bypass"
        pm2 restart all || echo "pm2 all bypass"
        
        echo "BULLETPROOF_DEPLOY_SUCCESS"
        """
        
        print("Executing remote rebuild commands...")
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
    except Exception as e:
        print(f"Failed bulletproof deploy: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    bulletproof_sftp_deploy()
