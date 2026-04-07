import paramiko

def check_files():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "CHECKING IF 3D REPOSITORY EXISTS NATIVELY ON CONTABO..."
        grep -i '3D Repository' /opt/edusyncai/edusync-web/app/admin/dashboard/page.tsx
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in iter(stdout.readline, ""):
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    check_files()
