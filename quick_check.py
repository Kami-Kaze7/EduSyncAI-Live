import paramiko

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)

def get(cmd, t=30):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    return stdout.read().decode('utf-8', errors='replace').strip()

# Test image access with proper curl format
https_test = get("curl -sk -o /dev/null -w '%{http_code}' 'https://62-171-138-230.nip.io/uploads/students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg'")
api_test = get("curl -s -o /dev/null -w '%{http_code}' 'http://localhost:5152/uploads/students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg'")

print(f"HTTPS status: {https_test}")
print(f"API status: {api_test}")

ssh.close()
