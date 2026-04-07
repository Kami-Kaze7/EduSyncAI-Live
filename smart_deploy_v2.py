import paramiko
import os

def smart_deploy_v2():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        # Determine the root path of edusync-web
        stdin, stdout, stderr = client.exec_command('find /var/www /opt -type d -name "edusync-web" 2>/dev/null | head -n 1')
        root_path = stdout.read().decode('utf-8').strip()
        if not root_path:
            print("Could not find edusync-web root path on server!")
            return
            
        print(f"Found remote root path: {root_path}")
        
        sftp = client.open_sftp()
        
        files_to_sync = [
            (r'C:\EduSyncAI\edusync-web\app\admin\dashboard\page.tsx', f'{root_path}/app/admin/dashboard/page.tsx'),
            (r'C:\EduSyncAI\edusync-web\lib\adminApi.ts', f'{root_path}/lib/adminApi.ts'),
            (r'C:\EduSyncAI\edusync-web\lib\studentApi.ts', f'{root_path}/lib/studentApi.ts'),
            (r'C:\EduSyncAI\edusync-web\components\admin\CourseUploadTab.tsx', f'{root_path}/components/admin/CourseUploadTab.tsx'),
            (r'C:\EduSyncAI\edusync-web\components\admin\FacultiesTab.tsx', f'{root_path}/components/admin/FacultiesTab.tsx'),
            (r'C:\EduSyncAI\edusync-web\app\student\dashboard\page.tsx', f'{root_path}/app/student/dashboard/page.tsx'),
            (r'C:\EduSyncAI\edusync-web\components\student\CourseVideosTab.tsx', f'{root_path}/components/student/CourseVideosTab.tsx'),
        ]
        
        for local_file, remote_file in files_to_sync:
            print(f"Uploading {local_file} -> {remote_file}")
            try:
                sftp.put(local_file, remote_file)
                print(f"  Success!")
            except Exception as e:
                print(f"  Error uploading {local_file}: {e}")
        
        sftp.close()
        
        print(f"Building NextJS in {root_path} (this will take 1-3 minutes...)")
        # Run npm build sync instead of sleeping blindly
        stdin, stdout, stderr = client.exec_command(f'cd {root_path} && rm -rf .next && npm run build')
        for line in stdout:
            print("BUILD:", line.strip())
        for line in stderr:
            print("BUILD ERR:", line.strip())
        
        print("Restarting PM2 process...")
        client.exec_command('pm2 restart edusync-web')
        print("DEPLOY COMPLETE!")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    smart_deploy_v2()
