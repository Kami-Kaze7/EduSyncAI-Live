import paramiko

def check_var_www():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "WHAT IS IN VAR WWW?"
        ls -la /var/www/edusyncai/ 2>/dev/null || echo "NOT IN VAR WWW"
        ls -la /var/www/edusyncai/edusync-web/ 2>/dev/null
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_var_www()
