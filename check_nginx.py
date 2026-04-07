import paramiko

def check_nginx():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        cat /etc/nginx/sites-enabled/edusyncai
        cat /etc/nginx/sites-enabled/default 2>/dev/null
        """
        stdin, stdout, stderr = client.exec_command(command)
        with open('C:\\EduSyncAI\\nginx_config.txt', 'w', encoding='utf-8') as f:
            for line in stdout:
                f.write(line)
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_nginx()
