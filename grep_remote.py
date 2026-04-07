import paramiko

def grep_remote():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        print("Grepping for Lecturer Directory on remote...")
        stdin, stdout, stderr = client.exec_command('grep -rn "Lecturer Directory" /var/www/edusyncai/ 2>/dev/null')
        for line in stdout:
            print(line, end="")
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    grep_remote()
