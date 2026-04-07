import paramiko
import sys

def fix_deploy():
    c = paramiko.SSHClient()
    c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    c.connect('173.212.248.253', username='root', password='viicsoft')
    sftp = c.open_sftp()

    local_path = r'C:\EduSyncAI\edusync-web\lib\adminApi.ts'
    remote_path = '/opt/edusyncai/edusync-web/lib/adminApi.ts'

    print('Uploading adminApi.ts...')
    sftp.put(local_path, remote_path)
    sftp.close()

    commands = [
        'cd /opt/edusyncai/edusync-web && rm -rf .next',
        'cd /opt/edusyncai/edusync-web && npm run build',
        'pm2 restart all'
    ]

    for cmd in commands:
        print(f'Running: {cmd}')
        stdin, stdout, stderr = c.exec_command(cmd)
        exit_status = stdout.channel.recv_exit_status()
        print(stdout.read().decode())
        print(stderr.read().decode())
        if exit_status != 0:
            print(f'Command failed with exit code: {exit_status}')
            sys.exit(1)

    c.close()
    print('Fix Complete!')

if __name__ == '__main__':
    fix_deploy()
