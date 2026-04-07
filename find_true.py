import paramiko

def find_true_deployment():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "ALL CWD FOR NODE PROCESSES RUN BY DEPLOY:"
        for pid in $(pgrep -u deploy node); do
            pwdx $pid
        done
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    find_true_deployment()
