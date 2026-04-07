import os
import subprocess
import paramiko
import time

def deploy_true_backend():
    print("1. Compiling Windows/Linux Cross-Platform WebAPI...")
    # We must explicitly publish the API for Linux
    os.chdir(r"C:\EduSyncAI\EduSyncAI.WebAPI")
    subprocess.run(["dotnet", "publish", "-c", "Release", "-r", "linux-x64", "--self-contained", "false", "-o", "publish_linux"], check=True)
    
    print("2. Zipping Linux Build...")
    os.chdir("publish_linux")
    subprocess.run(["tar", "-czf", "../backend_linux_deploy.tgz", "."], check=True)
    os.chdir("..")
    
    print("3. Connecting via SFTP to Contabo Server...")
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    client.connect('173.212.248.253', username='root', password='viicsoft')
    
    sftp = client.open_sftp()
    print("4. Uploading backend_linux_deploy.tgz to /tmp/ ...")
    sftp.put("backend_linux_deploy.tgz", "/tmp/backend_linux_deploy.tgz")
    sftp.close()
    
    print("5. Deploying to SystemD Production Directory...")
    cmd = """
    # Disable the conflicting PM2 instances
    pm2 stop edusyncai-webapi || true
    pm2 delete edusyncai-webapi || true
    pm2 save
    
    # Deploy to the true published path
    mkdir -p /opt/edusyncai/publish/webapi
    cd /opt/edusyncai/publish/webapi
    echo "Extracting new binary..."
    tar -xzf /tmp/backend_linux_deploy.tgz
    
    # Restart the SystemD daemon permanently owning Port 5000
    echo "Restarting edusyncai-api.service..."
    systemctl daemon-reload
    systemctl enable edusyncai-api.service
    systemctl restart edusyncai-api.service
    
    sleep 3
    systemctl status edusyncai-api.service --no-pager
    """
    
    stdin, stdout, stderr = client.exec_command(cmd)
    for line in stdout:
        print(line.strip())
        
    client.close()
    print("DEPLOYMENT COMPLETE")

if __name__ == '__main__':
    deploy_true_backend()
