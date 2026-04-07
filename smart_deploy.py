import paramiko
import os

def smart_deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        print("Finding exact path of page.tsx on server...")
        stdin, stdout, stderr = client.exec_command('find /var/www -name "page.tsx" | grep "admin/dashboard"')
        remote_page_path = stdout.read().decode('utf-8').strip().split('\n')[0]
        
        print(f"Found remote path: {remote_page_path}")
        
        sftp = client.open_sftp()
        print("Overwriting via SFTP...")
        sftp.put(r'C:\EduSyncAI\edusync-web\app\admin\dashboard\page.tsx', remote_page_path)
        
        remote_api_path = remote_page_path.replace('app/admin/dashboard/page.tsx', 'lib/adminApi.ts').replace('app/admin/dashboard/page.tsx', 'lib/adminApi.ts')
        
        # Determine exact lib/adminApi.ts location robustly
        stdin, stdout, stderr = client.exec_command('find /var/www -name "adminApi.ts"')
        remote_api_path = stdout.read().decode('utf-8').strip().split('\n')[0]
        print(f"Found remote API path: {remote_api_path}")
        sftp.put(r'C:\EduSyncAI\edusync-web\lib\adminApi.ts', remote_api_path)
        
        sftp.close()
        
        frontend_dir = remote_page_path.split('app')[0]
        print(f"Building NextJS in {frontend_dir}")
        client.exec_command(f'cd {frontend_dir} && npm run build')
        
        import time
        time.sleep(40) # Wait for build safely
        
        client.exec_command('pm2 restart edusync-web')
        print("DEPLOY COMPLETE!")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    smart_deploy()
