import json
with open('pm2_dump.json', 'r') as f:
    data = json.load(f)
    for app in data:
        print(f"APP: {app.get('name')}")
        print(f"CWD: {app.get('pm2_env', {}).get('pm_cwd')}")
