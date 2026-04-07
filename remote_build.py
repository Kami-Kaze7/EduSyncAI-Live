import paramiko

def deploy():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        print("Connecting to Contabo...")
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        # Build NextJS
        command = """
        FRONTEND_DIR=$(find / -maxdepth 4 -type d -name "edusync-web" 2>/dev/null | grep -i edusyncai | head -n 1)
        if [ -z "$FRONTEND_DIR" ]; then
            echo "COULD NOT FIND EDUSYNC-WEB DIR"
            exit 1
        fi
        cd "$FRONTEND_DIR"
        echo "Building Next.js inside $FRONTEND_DIR..."
        npm run build
        echo "Restarting pm2 edusync-web..."
        pm2 restart edusync-web
        echo "FRONTEND_REBUILD_SUCCESS"
        """
        stdin, stdout, stderr = client.exec_command(command)
        
        # Read streaming output
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
        exit_status = stdout.channel.recv_exit_status()
        print(f"Deploy returned exit code: {exit_status}")
    except Exception as e:
        print(f"Deploy failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    deploy()
