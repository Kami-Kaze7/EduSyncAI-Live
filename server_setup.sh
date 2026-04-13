#!/bin/bash
# ==============================================
# EduSyncAI Full Server Setup Script
# Target: Ubuntu server at 62.171.138.230
# ==============================================

set -e

echo "=========================================="
echo " EduSyncAI Server Setup - Phase 3"
echo "=========================================="

# --- 1. System Update ---
echo "[1/10] Updating system packages..."
apt update && apt upgrade -y

# --- 2. Install .NET 9 SDK ---
echo "[2/10] Installing .NET 9 SDK..."
if ! command -v dotnet &> /dev/null; then
    wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 9.0
    echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
    echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
    export DOTNET_ROOT=$HOME/.dotnet
    export PATH=$PATH:$HOME/.dotnet
fi
echo "  .NET version: $(dotnet --version)"

# --- 3. Install Node.js 20 LTS ---
echo "[3/10] Installing Node.js 20 LTS..."
if ! command -v node &> /dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
    apt install -y nodejs
fi
echo "  Node version: $(node --version)"
echo "  NPM version: $(npm --version)"

# --- 4. Install Python 3 + pip ---
echo "[4/10] Installing Python 3..."
apt install -y python3 python3-pip python3-venv
echo "  Python version: $(python3 --version)"

# --- 5. Install Nginx ---
echo "[5/10] Installing Nginx..."
apt install -y nginx
systemctl enable nginx

# --- 6. Install Certbot ---
echo "[6/10] Installing Certbot..."
apt install -y certbot python3-certbot-nginx

# --- 7. Install Git ---
echo "[7/10] Installing Git..."
apt install -y git

# --- 8. Clone Repository ---
echo "[8/10] Cloning EduSyncAI repository..."
if [ -d "/opt/edusyncai" ]; then
    echo "  Removing existing /opt/edusyncai..."
    rm -rf /opt/edusyncai
fi
cd /opt
git clone https://github.com/Kami-Kaze7/EduSyncAI-Live.git edusyncai
cd /opt/edusyncai

# Ensure Data directory permissions
chmod -R 755 Data/
mkdir -p Data/Recordings Data/WhiteboardImages Data/LectureMaterials

# --- 9. Build .NET WebAPI ---
echo "[9a/10] Building .NET WebAPI..."
cd /opt/edusyncai/EduSyncAI.WebAPI
dotnet restore
dotnet publish -c Release -o /opt/edusyncai/publish/api

# --- 9b. Build Next.js Frontend ---
echo "[9b/10] Building Next.js frontend..."
cd /opt/edusyncai/edusync-web
npm install
npm run build

# --- 9c. Setup Python Backend ---
echo "[9c/10] Setting up Python backend..."
cd /opt/edusyncai/backend
python3 -m venv venv
source venv/bin/activate
pip install -r requirements_facial.txt
deactivate

echo "[10/10] All dependencies installed and applications built!"
echo "=========================================="
echo " Run setup_services.sh next to configure"
echo " Nginx, SSL, and systemd services."
echo "=========================================="
