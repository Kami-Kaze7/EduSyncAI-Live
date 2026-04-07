import paramiko
import json

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

print("--- NGINX CONFIGURATION ---")
stdin, stdout, stderr = client.exec_command('grep -r "proxy_pass" /etc/nginx/sites-enabled/')
print(stdout.read().decode())

print("--- PM2 APPS DIRECTORIES ---")
stdin, stdout, stderr = client.exec_command('pm2 jlist')
pm2_output = stdout.read().decode()
try:
    apps = json.loads(pm2_output)
    for app in apps:
        print(f"App: {app.get('name')}")
        print(f"  CWD: {app.get('pm2_env', {}).get('pm_cwd')}")
        print(f"  Script: {app.get('pm2_env', {}).get('pm_exec_path')}")
except Exception as e:
    print(f"PM2 Parse Error: {e}")

print("--- LIST OF ALL PAGE.TSX MATCHING DASHBOARD/ADMIN ---")
stdin, stdout, stderr = client.exec_command('find / -maxdepth 5 -path "*/admin/dashboard/page.tsx" -o -path "*/admin/dashboard.tsx" 2>/dev/null')
print(stdout.read().decode())

client.close()
