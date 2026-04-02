import paramiko
import os
import difflib

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('173.212.248.253', username='root', password='viicsoft', timeout=30, banner_timeout=30)
sftp = c.open_sftp()
print("Connected to Contabo server!")

# Key files/dirs to compare
files_to_compare = []

# 1. Get the list of important files from the server
print("\n=== Collecting file lists ===")

# Frontend files
i, o, e = c.exec_command('find /opt/edusyncai/edusync-web/app -name "*.tsx" -o -name "*.ts" | sort')
server_frontend_files = [f.strip() for f in o.read().decode().strip().split('\n') if f.strip()]

i, o, e = c.exec_command('find /opt/edusyncai/edusync-web/lib -name "*.ts" | sort')
server_lib_files = [f.strip() for f in o.read().decode().strip().split('\n') if f.strip()]

# Combine
all_server_files = server_frontend_files + server_lib_files

# Add key config files
extra_files = [
    '/opt/edusyncai/edusync-web/package.json',
    '/opt/edusyncai/edusync-web/next.config.js',
    '/opt/edusyncai/edusync-web/next.config.ts',
    '/opt/edusyncai/edusync-web/.env.production',
]

for f in extra_files:
    try:
        sftp.stat(f)
        all_server_files.append(f)
    except FileNotFoundError:
        pass

print(f"Found {len(all_server_files)} files on server to compare")

# Compare each file
diffs = []
missing_local = []
missing_server = []
identical = 0

for server_path in all_server_files:
    # Convert server path to local path
    relative = server_path.replace('/opt/edusyncai/', '')
    local_path = os.path.join(r'c:\EduSyncAI', relative.replace('/', '\\'))
    
    # Read server file
    try:
        with sftp.file(server_path, 'r') as f:
            server_content = f.read().decode('utf-8', errors='replace')
    except Exception as ex:
        continue
    
    # Read local file
    if not os.path.exists(local_path):
        missing_local.append(relative)
        continue
    
    try:
        with open(local_path, 'r', encoding='utf-8', errors='replace') as f:
            local_content = f.read()
    except Exception:
        continue
    
    # Normalize line endings
    server_lines = server_content.replace('\r\n', '\n').splitlines(keepends=True)
    local_lines = local_content.replace('\r\n', '\n').splitlines(keepends=True)
    
    if server_lines == local_lines:
        identical += 1
    else:
        diff = list(difflib.unified_diff(
            local_lines, server_lines,
            fromfile=f'LOCAL: {relative}',
            tofile=f'SERVER: {relative}',
            n=3
        ))
        if diff:
            diffs.append((relative, ''.join(diff)))

# Check for local files not on server
i, o, e = c.exec_command('find /opt/edusyncai/edusync-web/app -name "*.tsx" -o -name "*.ts" | sort')
server_set = set(f.strip().replace('/opt/edusyncai/', '') for f in o.read().decode().strip().split('\n') if f.strip())

for root, dirs, files in os.walk(r'c:\EduSyncAI\edusync-web\app'):
    # Skip node_modules and .next
    dirs[:] = [d for d in dirs if d not in ['node_modules', '.next', '.git']]
    for fname in files:
        if fname.endswith(('.tsx', '.ts')):
            full = os.path.join(root, fname)
            rel = full.replace('c:\\EduSyncAI\\', '').replace('\\', '/')
            if rel not in server_set:
                missing_server.append(rel)

sftp.close()
c.close()

# Generate report
report = []
report.append(f"# Code Comparison Report\n")
report.append(f"**Date**: 2026-03-25\n")
report.append(f"**Local**: `c:\\EduSyncAI`\n")
report.append(f"**Server**: `/opt/edusyncai` on 173.212.248.253\n")
report.append(f"\n## Summary\n")
report.append(f"- ✅ **Identical files**: {identical}")
report.append(f"- ⚠️ **Files with differences**: {len(diffs)}")
report.append(f"- 🔴 **Missing locally** (on server but not local): {len(missing_local)}")
report.append(f"- 🟡 **Missing on server** (local but not on server): {len(missing_server)}")

if diffs:
    report.append(f"\n## Files With Differences\n")
    for rel, diff_text in diffs:
        report.append(f"\n### `{rel}`\n```diff")
        # Truncate very long diffs
        lines = diff_text.split('\n')
        if len(lines) > 60:
            report.append('\n'.join(lines[:60]))
            report.append(f"\n... ({len(lines) - 60} more lines)")
        else:
            report.append(diff_text)
        report.append("```\n")

if missing_local:
    report.append(f"\n## Missing Locally\nFiles on server but not in local repo:\n")
    for f in missing_local:
        report.append(f"- `{f}`")

if missing_server:
    report.append(f"\n## Missing on Server\nFiles in local repo but not deployed:\n")
    for f in missing_server:
        report.append(f"- `{f}`")

report_text = '\n'.join(report)

with open(r'c:\EduSyncAI\code_comparison_report.md', 'w', encoding='utf-8') as f:
    f.write(report_text)

print(report_text)
print("\n\nReport saved to c:\\EduSyncAI\\code_comparison_report.md")
