import os
import subprocess
import shutil

print("=== Building Backend for Linux (Self-Contained) ===")

opts = [
    "dotnet", "publish", 
    "c:\\EduSyncAI\\EduSyncAI.WebAPI\\EduSyncAI.WebAPI.csproj",
    "-c", "Release",
    "-r", "linux-x64",
    "-p:PublishSingleFile=true",
    "--self-contained", "true",
    "-o", "c:\\EduSyncAI\\publish"
]
subprocess.run(opts, check=True)

print("=== Zipping ===")
if os.path.exists("c:\\EduSyncAI\\backend.zip"):
    os.remove("c:\\EduSyncAI\\backend.zip")
shutil.make_archive("c:\\EduSyncAI\\backend", 'zip', "c:\\EduSyncAI\\publish")

print("=== Done! Run deploy_backend.py ===")
