# ============================================================
# EduSync AI — Installer Build Script
# Publishes all components and creates Windows installer
# ============================================================
# Usage: .\build.ps1
# Prerequisites: .NET SDK 9.0+, Node.js, Python 3.10+, Inno Setup 6
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
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  EduSync AI Installer Builder" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# ------------------------------------------------------------
# Step 0: Clean staging directory
# ------------------------------------------------------------
Write-Host "[0/5] Cleaning staging directory..." -ForegroundColor Yellow
if (Test-Path $STAGING) { Remove-Item $STAGING -Recurse -Force }
New-Item -ItemType Directory -Path $STAGING -Force | Out-Null
Write-Host "  Done." -ForegroundColor Green

# ------------------------------------------------------------
# Step 1: Publish WPF Desktop App (self-contained)
# ------------------------------------------------------------
Write-Host "[1/5] Publishing Desktop App..." -ForegroundColor Yellow
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
# Step 2: Publish WebAPI (self-contained)
# ------------------------------------------------------------
Write-Host "[2/5] Publishing Web API..." -ForegroundColor Yellow
dotnet publish "$ROOT\EduSyncAI.WebAPI\EduSyncAI.WebAPI.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -o "$STAGING\app\webapi" `
    /p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { throw "WebAPI publish failed" }
Write-Host "  Web API published." -ForegroundColor Green

# ------------------------------------------------------------
# Step 3: Build Next.js Frontend (production)
# ------------------------------------------------------------
Write-Host "[3/5] Building Web Frontend..." -ForegroundColor Yellow
Push-Location "$ROOT\edusync-web"

if (-not (Test-Path "node_modules")) {
    Write-Host "  Installing npm dependencies..." -ForegroundColor Gray
    npm install --silent
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
}

Write-Host "  Running next build..." -ForegroundColor Gray
npm run build
if ($LASTEXITCODE -ne 0) { throw "Next.js build failed" }
Pop-Location

# Copy frontend to staging (exclude dev cache to reduce size)
Write-Host "  Copying frontend files..." -ForegroundColor Gray
$webDest = "$STAGING\app\edusync-web"
New-Item -ItemType Directory -Path $webDest -Force | Out-Null

# Copy .next but EXCLUDE /cache and /dev directories (dev-only, thousands of files)
robocopy "$ROOT\edusync-web\.next" "$webDest\.next" /E /XD cache dev /NJH /NJS /NDL /NFL /NC /NS /NP | Out-Null

# Copy node_modules (needed for next start)
Write-Host "  Copying node_modules (this may take a moment)..." -ForegroundColor Gray
robocopy "$ROOT\edusync-web\node_modules" "$webDest\node_modules" /E /NJH /NJS /NDL /NFL /NC /NS /NP | Out-Null

Copy-Item "$ROOT\edusync-web\package.json" "$webDest\package.json"
Copy-Item "$ROOT\edusync-web\package-lock.json" "$webDest\package-lock.json"
if (Test-Path "$ROOT\edusync-web\public") {
    Copy-Item "$ROOT\edusync-web\public" "$webDest\public" -Recurse
}
if (Test-Path "$ROOT\edusync-web\next.config.ts") {
    Copy-Item "$ROOT\edusync-web\next.config.ts" "$webDest\next.config.ts"
}
if (Test-Path "$ROOT\edusync-web\.env.local") {
    Copy-Item "$ROOT\edusync-web\.env.local" "$webDest\.env.local"
}
Write-Host "  Web Frontend built and copied." -ForegroundColor Green

# ------------------------------------------------------------
# Step 4: Copy Python Backend + FFmpeg
# ------------------------------------------------------------
Write-Host "[4/5] Copying Python backend and FFmpeg..." -ForegroundColor Yellow
$backendDest = "$STAGING\app\backend"
New-Item -ItemType Directory -Path $backendDest -Force | Out-Null
Copy-Item "$ROOT\backend\gemini_face_service.py" $backendDest
Copy-Item "$ROOT\backend\requirements_facial.txt" $backendDest
if (Test-Path "$ROOT\backend\.env") {
    Copy-Item "$ROOT\backend\.env" $backendDest
}
if (Test-Path "$ROOT\backend\.env.template") {
    Copy-Item "$ROOT\backend\.env.template" $backendDest
}

# Copy FFmpeg if bundled
$ffmpegDir = "$ROOT\bin\Debug\net9.0-windows10.0.19041.0\ffmpeg"
if (Test-Path $ffmpegDir) {
    Write-Host "  Copying FFmpeg..." -ForegroundColor Gray
    Copy-Item $ffmpegDir "$STAGING\app\ffmpeg" -Recurse
}
Write-Host "  Backend and FFmpeg copied." -ForegroundColor Green

# Copy Data directory structure (empty, for first run)
$dataDir = "$STAGING\app\Data"
New-Item -ItemType Directory -Path "$dataDir\Recordings" -Force | Out-Null
New-Item -ItemType Directory -Path "$dataDir\StudentPhotos" -Force | Out-Null

# ------------------------------------------------------------
# Step 5: Bundle Node.js and Python runtimes
# ------------------------------------------------------------
Write-Host "[5/6] Bundling Node.js and Python runtimes..." -ForegroundColor Yellow
$runtimesDir = "$ROOT\installer\runtimes"

# Copy Node.js
$nodeSource = "$runtimesDir\node"
if (Test-Path "$nodeSource\node.exe") {
    Write-Host "  Copying Node.js runtime..." -ForegroundColor Gray
    robocopy "$nodeSource" "$STAGING\app\node" /E /NJH /NJS /NDL /NFL /NC /NS /NP | Out-Null
    Write-Host "  Node.js bundled." -ForegroundColor Green
} else {
    Write-Host "  Node.js runtime not found at: $nodeSource" -ForegroundColor Red
    Write-Host "  Run the setup steps to download it first." -ForegroundColor Red
}

# Copy Python
$pythonSource = "$runtimesDir\python"
if (Test-Path "$pythonSource\python.exe") {
    Write-Host "  Copying Python runtime..." -ForegroundColor Gray
    robocopy "$pythonSource" "$STAGING\app\python" /E /NJH /NJS /NDL /NFL /NC /NS /NP | Out-Null
    Write-Host "  Python bundled." -ForegroundColor Green
} else {
    Write-Host "  Python runtime not found at: $pythonSource" -ForegroundColor Red
    Write-Host "  Run the setup steps to download it first." -ForegroundColor Red
}

# Install Python pip dependencies into the bundled Python
# First, ensure 'import site' is uncommented in the _pth file (required for pip)
$pthFile = Get-ChildItem "$STAGING\app\python\*.pth" | Select-Object -First 1
if ($pthFile) {
    $pthContent = Get-Content $pthFile.FullName -Raw
    $pthContent = $pthContent -replace '#import site', 'import site'
    Set-Content $pthFile.FullName $pthContent -NoNewline
    Write-Host "  Fixed $($pthFile.Name) for pip support." -ForegroundColor Gray
}
$pipExe = "$STAGING\app\python\Scripts\pip.exe"
$reqFile = "$STAGING\app\backend\requirements_facial.txt"
if ((Test-Path $pipExe) -and (Test-Path $reqFile)) {
    Write-Host "  Pre-installing Python dependencies..." -ForegroundColor Gray
    try {
        $prevPref = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        & "$STAGING\app\python\python.exe" -m pip install -r "$reqFile" --quiet --no-warn-script-location 2>&1 | Out-Null
        $ErrorActionPreference = $prevPref
    } catch {
        Write-Host "  Warning: pip install had issues, dependencies may need to be installed on first run." -ForegroundColor DarkYellow
    }
    Write-Host "  Python dependencies installed." -ForegroundColor Green
}

# ------------------------------------------------------------
# Step 6: Run Inno Setup
# ------------------------------------------------------------
Write-Host "[6/6] Creating installer..." -ForegroundColor Yellow
if (Test-Path $ISCC) {
    & $ISCC "$ROOT\installer\EduSyncAI.iss"
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "  Installer created successfully!" -ForegroundColor Green
    Write-Host "  Output: $ROOT\installer\Output\" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
} else {
    Write-Host "  Inno Setup not found at: $ISCC" -ForegroundColor Red
    Write-Host "  Staging directory is ready at: $STAGING" -ForegroundColor Yellow
    Write-Host "  Run Inno Setup manually with: $ROOT\installer\EduSyncAI.iss" -ForegroundColor Yellow
}
