# Quick Pip Installation Script
# This will download and install pip for your Python installation

# Download get-pip.py
Invoke-WebRequest -Uri "https://bootstrap.pypa.io/get-pip.py" -OutFile "get-pip.py"

# Install pip
python get-pip.py

# Verify installation
python -m pip --version

# Clean up
Remove-Item get-pip.py

Write-Host "Pip installation complete!" -ForegroundColor Green
Write-Host "Now installing facial recognition dependencies..." -ForegroundColor Yellow

# Install required packages
python -m pip install google-generativeai flask flask-cors opencv-python pillow python-dotenv

Write-Host "All dependencies installed!" -ForegroundColor Green
