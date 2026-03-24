import paramiko

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft', timeout=30, banner_timeout=30)

# 1. Fix the appsettings.json connection string to point to the ORIGINAL production database
print("Fixing connection string...")
fix_cmd = """python3 -c "
import json
with open('/opt/edusyncai/EduSyncAI.WebAPI/appsettings.json','r') as f:
    cfg = json.load(f)
cfg['ConnectionStrings']['DefaultConnection'] = 'Data Source=/opt/edusyncai/Data/edusync.db'
with open('/opt/edusyncai/EduSyncAI.WebAPI/appsettings.json','w') as f:
    json.dump(cfg, f, indent=2)
print('Connection string restored!')
print(cfg['ConnectionStrings'])
" """
i, o, e = c.exec_command(fix_cmd)
print(o.read().decode())
print(e.read().decode())

# 2. Restart the API service
print("Restarting edusyncai-api service...")
i, o, e = c.exec_command('systemctl restart edusyncai-api && sleep 2 && systemctl status edusyncai-api --no-pager')
print(o.read().decode())
print(e.read().decode())

# 3. Restart nginx and web
print("Restarting nginx and web...")
i, o, e = c.exec_command('systemctl restart nginx && pm2 restart all')
print(o.read().decode())

# 4. Quick health check
print("Health check...")
i, o, e = c.exec_command('curl -s http://localhost:5152/api/admin/course-list 2>/dev/null | head -c 200')
print(o.read().decode())

c.close()
print("DONE!")
