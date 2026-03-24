import paramiko
import os
import json

def deploy_backend():
    host = '173.212.248.253'
    username = 'root'
    password = 'viicsoft'
    
    # Connect SSH
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    print("Connecting to server...")
    client.connect(host, username=username, password=password)
    
    # 1. Find PM2 script path
    print("Locating backend directory via PM2...")
    stdin, stdout, stderr = client.exec_command("pm2 jlist")
    jlist_str = stdout.read().decode().strip()
    
    script_path = ""
    try:
        processes = json.loads(jlist_str)
        for proc in processes:
            if proc['name'] == 'edusyncai-webapi':
                script_path = proc['pm2_env']['pm_exec_path']
                break
    except Exception as e:
        print("Failed to parse PM2 list.", e)
    
    if not script_path:
        print("Could not find edusyncai-webapi PM2 process path. Defaulting to /opt/edusyncai/backend/")
        script_dir = "/opt/edusyncai/backend/"
    else:
        script_dir = os.path.dirname(script_path)
    
    print(f"Backend directory determined as: {script_dir}")
    
    # 2. Upload Zip via SFTP
    print("Uploading backend.zip via SFTP...")
    sftp = client.open_sftp()
    remote_zip_path = f"{script_dir}/backend_update.zip"
    sftp.put(r"c:\EduSyncAI\backend.zip", remote_zip_path)
    sftp.close()
    
    # 3. Unzip and Restart
    print("Extracting files and restarting service...")
    commands = [
        f"cd {script_dir}",
        f"unzip -o backend_update.zip",
        f"rm backend_update.zip",
        f"chmod +x EduSyncAI.WebAPI",  # Ensure it remains executable
        f"pm2 restart edusyncai-webapi"
    ]
    
    cmd_str = " && ".join(commands)
    stdin, stdout, stderr = client.exec_command(cmd_str)
    
    print("--- Output ---")
    print(stdout.read().decode())
    
    err = stderr.read().decode()
    if err:
        print("--- Errors ---")
        print(err)
        
    print("Deployment cycle finished.")
    client.close()

if __name__ == "__main__":
    deploy_backend()
