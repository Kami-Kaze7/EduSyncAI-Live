import paramiko

def repair_api_boot():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "STOPPING PM2 WEBAPI"
        pm2 stop edusyncai-webapi
        
        echo "KILLING ANYTHING ON PORT 5000"
        while lsof -i:5000 -t; do
            kill -9 $(lsof -i:5000 -t)
            sleep 1
        done
        
        echo "RESTARTING WEBAPI IN FORK MODE (NO CLUSTER)"
        pm2 start edusyncai-webapi -i 1 || true
        pm2 restart edusyncai-webapi
        
        echo "WAITING 5 SECONDS"
        sleep 5
        pm2 logs edusyncai-webapi --lines 15 --nostream
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line, end="")
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    repair_api_boot()
