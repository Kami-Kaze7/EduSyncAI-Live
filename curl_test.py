import paramiko

def curl_api():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect('173.212.248.253', username='root', password='viicsoft')
        
        command = """
        echo "TESTING API ROUTE NATIVELY..."
        curl -s -o /dev/null -w "%{http_code}" -X GET http://localhost:5000/api/ModelAssets
        echo ""
        echo "TESTING NGINX PROXY..."
        curl -s -o /dev/null -w "%{http_code}" -X GET "https://173-212-248-253.nip.io/api/ModelAssets"
        echo ""
        """
        stdin, stdout, stderr = client.exec_command(command)
        for line in stdout:
            print(line.strip())
            
    except Exception as e:
        print(f"Failed: {e}")
    finally:
        client.close()

if __name__ == '__main__':
    curl_api()
