import paramiko

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft', timeout=30, banner_timeout=30)

# Search for Docker containers
print("=== DOCKER CONTAINERS ===")
i, o, e = c.exec_command('docker ps --format "{{.Names}} {{.Image}}" 2>/dev/null')
print(o.read().decode())

# Search for docker-compose Jitsi
print("=== DOCKER COMPOSE FILES ===")
i, o, e = c.exec_command('find / -name "docker-compose.yml" -path "*jitsi*" 2>/dev/null; find / -name ".env" -path "*jitsi*" 2>/dev/null; find /root -name "docker-compose.yml" 2>/dev/null; find /opt -name "docker-compose.yml" 2>/dev/null')
print(o.read().decode())

# Search for Jitsi config anywhere
print("=== JITSI FILES ANYWHERE ===")
i, o, e = c.exec_command('find / -name "interface_config.js" 2>/dev/null; find / -name "*jitsi*config*" -not -path "*/proc/*" 2>/dev/null | head -20')
print(o.read().decode())

# Check nginx config for meet.viicsoft.dev
print("=== NGINX JITSI CONFIG ===")
i, o, e = c.exec_command('grep -rl "meet.viicsoft" /etc/nginx/ 2>/dev/null')
files = o.read().decode().strip()
print(files)
if files:
    for f in files.split('\n'):
        if f.strip():
            print(f"\n--- {f} ---")
            i2, o2, e2 = c.exec_command(f'cat {f.strip()}')
            print(o2.read().decode()[:2000])

c.close()
