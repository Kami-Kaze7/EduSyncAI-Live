import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('173.212.248.253', username='root', password='viicsoft')

stdin, stdout, stderr = client.exec_command('ls -la /var/www/edusyncai/edusync-web/')
for line in stdout:
    print(line, end="")

client.close()
