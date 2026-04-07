import paramiko
import sys

def sniff_server():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        print("Checking open port 3000 processes:")
        stdin, stdout, stderr = client.exec_command('lsof -i :3000')
        print(stdout.read().decode())
        
        print("\nChecking node processes:")
        stdin, stdout, stderr = client.exec_command('ps aux | grep node | grep -v grep | head -n 5')
        print(stdout.read().decode())
        
        print("\nChecking curl localhost:3000 for 3D Repository:")
        stdin, stdout, stderr = client.exec_command('curl -s http://localhost:3000/admin/dashboard | grep -i "3D" | head -n 5')
        print(stdout.read().decode())
        
        print("Done Python.")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    sniff_server()
