import paramiko

def fetch_logs():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    client.connect('173.212.248.253', username='root', password='viicsoft')
    
    # Check nginx tail logs
    print("--- NGINX ERRORS ---")
    stdin, stdout, stderr = client.exec_command('tail -n 25 /var/log/nginx/error.log')
    print(stdout.read().decode())
    
    print("--- NGINX ACCESS ---")
    stdin, stdout, stderr = client.exec_command('tail -n 25 /var/log/nginx/access.log | grep -v "/hubs/"')
    print(stdout.read().decode())

    client.close()

if __name__ == '__main__':
    fetch_logs()
