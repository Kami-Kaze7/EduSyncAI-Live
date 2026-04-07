import paramiko
import os
import posixpath

def deploy_fast():
    c = paramiko.SSHClient()
    c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    c.connect('173.212.248.253', username='root', password='viicsoft')
    sftp = c.open_sftp()
    
    local_base = r'C:\EduSyncAI\edusync-web'
    remote_base = '/opt/edusyncai/edusync-web'
    
    dirs_to_copy = ['app', 'components']
    
    for d in dirs_to_copy:
        local_dir = os.path.join(local_base, d)
        for root, dirs, files in os.walk(local_dir):
            for file in files:
                if file.endswith('.tsx') or file.endswith('.ts') or file.endswith('.css'):
                    local_path = os.path.join(root, file)
                    rel_path = os.path.relpath(local_path, local_base).replace('\\', '/')
                    remote_path = posixpath.join(remote_base, rel_path)
                    
                    remote_dir = posixpath.dirname(remote_path)
                    # Create remote dir if not exists
                    try:
                        sftp.stat(remote_dir)
                    except FileNotFoundError:
                        stdin, stdout, stderr = c.exec_command(f"mkdir -p \"{remote_dir}\"")
                        stdout.read()
                    
                    print(f"Uploading {rel_path}...")
                    sftp.put(local_path, remote_path)
                    
    sftp.close()
    
    commands = [
        "cd /opt/edusyncai/edusync-web && npm run build",
        "pm2 restart all"
    ]
    
    for cmd in commands:
        print(f"Running: {cmd}")
        stdin, stdout, stderr = c.exec_command(cmd)
        print(stdout.read().decode())
        
    c.close()
    print("Done!")

if __name__ == '__main__':
    deploy_fast()
