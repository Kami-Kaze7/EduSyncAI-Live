import paramiko, time

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft', timeout=10, banner_timeout=10)

results = []

def run(cmd, timeout=15):
    results.append(f"\n--- {cmd} ---")
    try:
        stdin, stdout, stderr = c.exec_command(cmd, timeout=timeout)
        out = stdout.read().decode('utf-8', errors='replace')
        err = stderr.read().decode('utf-8', errors='replace')
        if out: results.append(out[:3000])
        if err: results.append("STDERR: " + err[:1000])
    except Exception as ex:
        results.append(f"ERROR: {ex}")

# Check PM2 list
run("pm2 list")

# Check port 3000
run("ss -tlnp | grep 3000")

# Check error log
run("tail -n 10 /root/.pm2/logs/edusync-web-error.log")

# Check out log
run("tail -n 10 /root/.pm2/logs/edusync-web-out.log")

# Wait and re-check
time.sleep(5)

# Test the CSS fetch
run('curl -sSL http://localhost:3000/ -o /dev/null -w "HTTP Status: %{http_code}, Size: %{size_download}"')

# Find the current CSS file
run('find /opt/edusyncai/edusync-web/.next -name "*.css" -exec ls -la {} \\;')

# Try fetching the actual page HTML and check for stylesheet
run('curl -sSL http://localhost:3000/ | grep -oP \'href="[^"]+\\.css[^"]*"\'')

c.close()

with open(r'C:\EduSyncAI\fix_pm2_results.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(results))

print("Results written to fix_pm2_results.txt")
