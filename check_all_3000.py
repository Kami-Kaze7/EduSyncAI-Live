import paramiko

def check_all_3000():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "ALL PROCESSES ON 3000:"
        netstat -ltnp | grep ":3000"
        echo "WHAT IS THE NEXTJS LOGS FOR EDUSYNC-WEB?"
        pm2 logs edusync-web --lines 20 --nostream
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_all_3000()
