import paramiko
import os

def sftp_deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        sftp = client.open_sftp()
        
        # Uploading Frontend Changes
        print("Uploading page.tsx...")
        sftp.put(r'C:\EduSyncAI\edusync-web\app\admin\dashboard\page.tsx', '/var/www/edusyncai/edusync-web/app/admin/dashboard/page.tsx')
        
        print("Uploading adminApi.ts...")
        sftp.put(r'C:\EduSyncAI\edusync-web\lib\adminApi.ts', '/var/www/edusyncai/edusync-web/lib/adminApi.ts')
        
        # Uploading Backend Changes
        print("Uploading Controller...")
        sftp.put(r'C:\EduSyncAI\EduSyncAI.WebAPI\Controllers\ModelAssetsController.cs', '/var/www/edusyncai/EduSyncAI.WebAPI/Controllers/ModelAssetsController.cs')
        
        print("Uploading Program.cs...")
        sftp.put(r'C:\EduSyncAI\EduSyncAI.WebAPI\Program.cs', '/var/www/edusyncai/EduSyncAI.WebAPI/Program.cs')
        
        print("Uploading DatabaseService...")
        sftp.put(r'C:\EduSyncAI\Services\DatabaseService.cs', '/var/www/edusyncai/Services/DatabaseService.cs')
        
        sftp.close()
        
        print("Triggering NPM RUN BUILD remotely...")
        stdin, stdout, stderr = client.exec_command('cd /var/www/edusyncai/edusync-web && npm run build && pm2 restart edusync-web')
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
        print("Triggering .NET Restart remotely...")
        client.exec_command('cd /var/www/edusyncai/EduSyncAI.WebAPI && dotnet build && systemctl restart edusync-api.service || systemctl restart edusync')
        
        print("SFTP_DEPLOY_SUCCESS")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    sftp_deploy()
