import paramiko, os

HOST = "62.171.138.230"
USER = "root"
PASS = "viicsoft"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASS, timeout=30)
sftp = ssh.open_sftp()

def run(cmd, t=30):
    stdin, stdout, stderr = ssh.exec_command(cmd, timeout=t)
    out = stdout.read().decode('utf-8', errors='replace').strip()
    err = stderr.read().decode('utf-8', errors='replace').strip()
    return out, err

# 1. Check what exists on the server
print("=== SERVER UPLOADS ===")
o, e = run("find /opt/edusyncai/Data/uploads -type f 2>/dev/null")
print(o if o else "(empty)")
print()

o, e = run("find /opt/edusyncai/publish/Data/uploads -type f 2>/dev/null")
print(f"Publish uploads: {o if o else '(empty)'}")
print()

# 2. Check which path the API actually reads from
o, e = run("readlink -f /opt/edusyncai/publish/Data")
print(f"Publish Data resolves to: {o}")

# 3. Check the API's working directory and how it serves static files
o, e = run("grep -r 'UseStaticFiles\\|StaticFiles\\|PhysicalFileProvider\\|uploads' /opt/edusyncai/EduSyncAI.WebAPI/Program.cs 2>/dev/null")
print(f"\nProgram.cs static files config:\n{o}")

# 4. Create directories on server
print("\n=== CREATING DIRECTORIES ===")
run("mkdir -p /opt/edusyncai/Data/uploads/students")
run("mkdir -p /opt/edusyncai/Data/uploads/models/biology")
run("mkdir -p /opt/edusyncai/publish/Data/uploads/students")
run("mkdir -p /opt/edusyncai/publish/Data/uploads/models/biology")

# 5. Upload the files
local_base = r"C:\EduSyncAI\Data\uploads"
files_to_upload = [
    ("students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg", 
     "/opt/edusyncai/Data/uploads/students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg"),
    ("models/biology/35431b47-1376-40fd-9dee-cf3e648b3980.obj",
     "/opt/edusyncai/Data/uploads/models/biology/35431b47-1376-40fd-9dee-cf3e648b3980.obj"),
    ("models/biology/775951b8-272c-4a86-9255-d14e2e292a8f.obj",
     "/opt/edusyncai/Data/uploads/models/biology/775951b8-272c-4a86-9255-d14e2e292a8f.obj"),
]

for local_rel, remote_path in files_to_upload:
    local_path = os.path.join(local_base, local_rel)
    if os.path.exists(local_path):
        print(f"\nUploading {local_rel} ({os.path.getsize(local_path)} bytes)...")
        sftp.put(local_path, remote_path)
        # Also copy to publish/Data
        publish_path = remote_path.replace("/opt/edusyncai/Data/", "/opt/edusyncai/publish/Data/")
        sftp.put(local_path, publish_path)
        print(f"  -> Uploaded to {remote_path}")
        print(f"  -> Uploaded to {publish_path}")
    else:
        print(f"  SKIP: {local_path} not found locally")

# 6. Set permissions
print("\n=== SETTING PERMISSIONS ===")
run("chmod -R 755 /opt/edusyncai/Data/uploads")
run("chmod -R 755 /opt/edusyncai/publish/Data/uploads")

# 7. Verify
print("\n=== VERIFICATION ===")
o, e = run("find /opt/edusyncai/Data/uploads -type f -exec ls -lh {} \\;")
print(f"Data/uploads:\n{o}")
o, e = run("find /opt/edusyncai/publish/Data/uploads -type f -exec ls -lh {} \\;")
print(f"\nPublish/Data/uploads:\n{o}")

# 8. Check what photoPath the student has in DB
o, e = run("sqlite3 /opt/edusyncai/publish/Data/edusync.db \"SELECT Id, FullName, PhotoPath FROM Students WHERE PhotoPath IS NOT NULL AND PhotoPath != '';\"")
print(f"\nStudents with photos:\n{o}")

# 9. Test fetching the image via the API
o, e = run("curl -sk -o /dev/null -w '%{http_code}' https://62-171-138-230.nip.io/uploads/students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg")
print(f"\nHTTPS image fetch status: {o}")

o, e = run("curl -s -o /dev/null -w '%{http_code}' http://localhost:5152/uploads/students/2017_123456_6a5fd118-3947-43b5-89f5-008b4e0a894e.jpeg")
print(f"API direct image fetch status: {o}")

sftp.close()
ssh.close()
print("\nDONE!")
