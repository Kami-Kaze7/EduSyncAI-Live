import paramiko

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft', timeout=10, banner_timeout=10)

def run(cmd, timeout=15):
    print(f"\n--- {cmd} ---")
    try:
        stdin, stdout, stderr = c.exec_command(cmd, timeout=timeout)
        out = stdout.read().decode('utf-8', errors='replace')
        err = stderr.read().decode('utf-8', errors='replace')
        if out: print(out[:2000])
        if err: print("STDERR:", err[:500])
    except Exception as ex:
        print(f"ERROR: {ex}")

# Step 1: Kill ALL processes on port 3000
print("=== STEP 1: Kill all processes on port 3000 ===")
run("fuser -k 3000/tcp 2>&1 || true")

# Step 2: Stop PM2 edusync-web and delete it
print("\n=== STEP 2: Stop and delete PM2 edusync-web process ===")
run("pm2 stop edusync-web 2>&1 || true")
run("pm2 delete edusync-web 2>&1 || true")

# Step 3: Verify nothing on port 3000
print("\n=== STEP 3: Verify port 3000 is free ===")
run("ss -tlnp | grep 3000 || echo 'Port 3000 is FREE'")

# Step 4: Start edusync-web fresh via PM2
print("\n=== STEP 4: Start edusync-web fresh ===")
run("cd /opt/edusyncai/edusync-web && pm2 start npm --name edusync-web -- start")

# Step 5: Wait a moment then check status
import time
time.sleep(3)

print("\n=== STEP 5: Check PM2 status ===")
run("pm2 list")

# Step 6: Check error log to see if it started cleanly
run("tail -n 5 /root/.pm2/logs/edusync-web-error.log")
run("tail -n 5 /root/.pm2/logs/edusync-web-out.log")

# Step 7: Check the port is now bound
run("ss -tlnp | grep 3000")

# Step 8: Save PM2 so this persists on reboot
run("pm2 save")

c.close()
print("\n=== FIX COMPLETE ===")
