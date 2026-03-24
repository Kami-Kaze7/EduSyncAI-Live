import paramiko
import time

def deploy():
    host = '173.212.248.253'
    username = 'root'
    password = 'viicsoft'
    
    print(f"Connecting to {host} as {username}...")
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    
    try:
        client.connect(host, username=username, password=password)
        print("Successfully connected!")
        
        commands = [
            # 1. Pull the latest code for edusync-web and WebAPI
            "cd /opt/edusyncai && git pull origin main",
            # 2. Build the .NET WebAPI backend
            "cd /opt/edusyncai/EduSyncAI.WebAPI && dotnet build -c Release",
            # 3. Build the Next.js frontend
            "cd /opt/edusyncai/edusync-web && npm install && rm -rf .next && npm run build",
            # 4. Restart the PM2 services
            "pm2 restart all"
        ]
        
        for cmd in commands:
            print(f"\n--- Running: {cmd} ---")
            stdin, stdout, stderr = client.exec_command(cmd, get_pty=True)
            
            # Print output in real-time
            while True:
                line = stdout.readline()
                if not line:
                    break
                print(line, end="")
                
            exit_status = stdout.channel.recv_exit_status()
            if exit_status != 0:
                print(f"Command failed with exit code: {exit_status}")
                err = stderr.read().decode()
                if err:
                    print(f"STDERR: {err}")
                return
                
        print("\nDeployment completed successfully!")
        
    except Exception as e:
        print(f"Error during deployment: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    deploy()
