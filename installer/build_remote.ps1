# ============================================================
# EduSync AI — Remote-Server Installer Build Script
# Publishes desktop app only (connects to live server)
# ============================================================
# Usage: .\build_remote.ps1
# Prerequisites: .NET SDK 9.0+, Inno Setup 6
# ============================================================

$ErrorActionPreference = "Stop"
$ROOT = (Get-Item "$PSScriptRoot\..").FullName
$STAGING = "$ROOT\installer\staging"
$ISCC = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"

# Fallback Inno Setup paths
if (-not (Test-Path $ISCC)) {
    $ISCC = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $ISCC)) {
    $ISCC = "C:\Program Files\Inno Setup 6\ISCC.exe"
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  EduSync AI Installer Builder (Remote Mode)" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Verify UseRemoteServer is set to true
$appConfigPath = "$ROOT\AppConfig.cs"
$appConfigContent = Get-Content $appConfigPath -Raw
if ($appConfigContent -notmatch 'UseRemoteServer\s*=\s*true') {
    Write-Host "  WARNING: AppConfig.UseRemoteServer is not set to true!" -ForegroundColor Red
    Write-Host "  The installer requires remote server mode." -ForegroundColor Red
    Write-Host "  Setting UseRemoteServer = true..." -ForegroundColor Yellow
    $appConfigContent = $appConfigContent -replace 'UseRemoteServer\s*=\s*false', 'UseRemoteServer = true'
    Set-Content $appConfigPath $appConfigContent -NoNewline
    Write-Host "  Done." -ForegroundColor Green
}

# ------------------------------------------------------------
# Step 0: Clean staging directory
# ------------------------------------------------------------
Write-Host "[0/3] Cleaning staging directory..." -ForegroundColor Yellow
if (Test-Path $STAGING) { Remove-Item $STAGING -Recurse -Force }
New-Item -ItemType Directory -Path $STAGING -Force | Out-Null
Write-Host "  Done." -ForegroundColor Green

# ------------------------------------------------------------
# Step 1: Publish WPF Desktop App (self-contained)
# ------------------------------------------------------------
Write-Host "[1/3] Publishing Desktop App (self-contained, x64)..." -ForegroundColor Yellow
dotnet publish "$ROOT\EduSyncAI.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -o "$STAGING\app" `
    /p:PublishSingleFile=false `
    /p:IncludeNativeLibrariesForSelfExtract=true
if ($LASTEXITCODE -ne 0) { throw "Desktop app publish failed" }
Write-Host "  Desktop App published." -ForegroundColor Green

# ------------------------------------------------------------
# Step 2: Copy supporting files
# ------------------------------------------------------------
Write-Host "[2/3] Copying supporting files..." -ForegroundColor Yellow

# Copy app icon
if (Test-Path "$ROOT\edusync_icon.ico") {
    Copy-Item "$ROOT\edusync_icon.ico" "$STAGING\app\edusync_icon.ico"
    Write-Host "  App icon copied." -ForegroundColor Green
}

# Copy FFmpeg if bundled (needed for recording)
$ffmpegPaths = @(
    "$ROOT\bin\Debug\net9.0-windows10.0.19041.0\ffmpeg",
    "$ROOT\bin\Release\net9.0-windows10.0.19041.0\ffmpeg",
    "$ROOT\ffmpeg"
)
foreach ($ffmpegDir in $ffmpegPaths) {
    if (Test-Path $ffmpegDir) {
        Write-Host "  Copying FFmpeg from $ffmpegDir..." -ForegroundColor Gray
        Copy-Item $ffmpegDir "$STAGING\app\ffmpeg" -Recurse
        Write-Host "  FFmpeg copied." -ForegroundColor Green
        break
    }
}

# Create Data directory structure
$dataDir = "$STAGING\app\Data"
New-Item -ItemType Directory -Path "$dataDir\Recordings" -Force | Out-Null
New-Item -ItemType Directory -Path "$dataDir\StudentPhotos" -Force | Out-Null
Write-Host "  Data directories created." -ForegroundColor Green

# Show staging size
$stagingSize = (Get-ChildItem "$STAGING" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "  Staging size: $([math]::Round($stagingSize, 1)) MB" -ForegroundColor Gray

# ------------------------------------------------------------
# Step 3: Run Inno Setup
# ------------------------------------------------------------
Write-Host "[3/3] Creating installer..." -ForegroundColor Yellow
$issFile = "$ROOT\installer\EduSyncAI_Remote.iss"
if (Test-Path $ISCC) {
    & $ISCC "$issFile"
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }
    Write-Host ""
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "  Installer created successfully!" -ForegroundColor Green
    Write-Host "  Output: $ROOT\installer\Output\" -ForegroundColor Green
    Write-Host "=============================================" -ForegroundColor Green
} else {
    Write-Host "  Inno Setup not found at: $ISCC" -ForegroundColor Red
    Write-Host "  Staging directory is ready at: $STAGING" -ForegroundColor Yellow
    Write-Host "  Run Inno Setup manually on: $issFile" -ForegroundColor Yellow
}
