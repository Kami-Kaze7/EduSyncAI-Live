import paramiko
import json

def check_pm2_jlist():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        stdin, stdout, stderr = client.exec_command('pm2 jlist')
        data = json.loads(stdout.read().decode('utf-8'))
        
        for app in data:
            print(f"App: {app.get('name')}")
            print(f"  PID: {app.get('pid')}")
            print(f"  Status: {app.get('pm2_env', {}).get('status')}")
            print(f"  Port handling? Look below:")
            
        print("------- LSOF PORT 3000 -------")
        stdin, stdout, stderr = client.exec_command('lsof -i :3000')
        print(stdout.read().decode('utf-8'))
        
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_pm2_jlist()
