import paramiko

def check_ports():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "WHAT IS LISTENING ON PORT 3000?"
        lsof -i :3000
        echo "WHAT IS LISTENING ON PORT 3001?"
        lsof -i :3001
        echo "WHAT IS LISTENING ON PORT 3002?"
        lsof -i :3002
        echo "FINDING PORT FOR EDUSYNC-WEB:"
        netstat -ltnp | grep node
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_ports()
