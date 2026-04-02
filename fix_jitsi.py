import paramiko

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('62.171.138.230', username='root', password='viicsoft', timeout=30, banner_timeout=30)
print("Connected!")

close_html = r"""<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Class Ended — EduSync AI</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap" rel="stylesheet">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
            background: linear-gradient(135deg, #0f0f23 0%, #1a1a2e 100%);
            color: white;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            text-align: center;
            padding: 24px;
        }
        .container {
            background: rgba(255,255,255,0.05);
            border-radius: 20px;
            padding: 40px;
            max-width: 420px;
            width: 100%;
            border: 1px solid rgba(255,255,255,0.1);
            backdrop-filter: blur(10px);
        }
        .icon { font-size: 56px; margin-bottom: 20px; }
        h1 { font-size: 24px; font-weight: 700; margin-bottom: 8px; }
        .subtitle { color: #a78bfa; font-size: 15px; font-weight: 600; margin-bottom: 6px; }
        p { color: #888; font-size: 14px; margin-bottom: 28px; line-height: 1.5; }
        .btn {
            display: block;
            width: 100%;
            padding: 16px 32px;
            background: linear-gradient(135deg, #3b82f6, #8b5cf6);
            color: white;
            border: none;
            border-radius: 12px;
            cursor: pointer;
            font-size: 16px;
            font-weight: 700;
            text-decoration: none;
            box-shadow: 0 4px 15px rgba(59, 130, 246, 0.4);
            transition: transform 0.2s, box-shadow 0.2s;
        }
        .btn:hover { transform: translateY(-2px); box-shadow: 0 6px 20px rgba(59, 130, 246, 0.5); }
        .countdown { color: #555; font-size: 12px; margin-top: 20px; }
        .brand { color: #444; font-size: 11px; margin-top: 24px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="icon">&#10024;</div>
        <div class="subtitle">Thank you for using</div>
        <h1>EduSync AI</h1>
        <p>Your live class session has ended successfully. Head back to your dashboard to review your courses.</p>
        <a class="btn" href="https://173-212-248-253.nip.io/student/dashboard">&#8592; Back to Dashboard</a>
        <p class="countdown" id="countdown">Auto-redirecting in 10 seconds...</p>
        <p class="brand">EduSync AI &mdash; Smart Education Platform</p>
    </div>
    <script>
        var seconds = 10;
        var el = document.getElementById('countdown');
        var timer = setInterval(function() {
            seconds--;
            if (seconds <= 0) {
                clearInterval(timer);
                window.location.href = 'https://173-212-248-253.nip.io/student/dashboard';
            } else {
                el.textContent = 'Auto-redirecting in ' + seconds + ' seconds...';
            }
        }, 1000);
    </script>
</body>
</html>"""

# Write the close page
sftp = c.open_sftp()
with sftp.file('/usr/share/jitsi-meet/static/close3.html', 'w') as f:
    f.write(close_html)
sftp.close()

# Verify
i, o, e = c.exec_command('wc -c /usr/share/jitsi-meet/static/close3.html')
print("Close page size:", o.read().decode().strip())

print("Done!")
c.close()
