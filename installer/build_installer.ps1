$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$iscc = "C:\Users\sysadmin\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
$signtool = "C:\Users\sysadmin\.nuget\packages\microsoft.windows.sdk.buildtools\10.0.28000.2957\bin\10.0.28000.0\x64\signtool.exe"
$pfx = Join-Path $rootDir "ShareBridge_Dev.pfx"
$issPath = Join-Path $scriptDir "ShareBridge.iss"
$outputExe = Join-Path $rootDir "ShareBridge-Setup-v1.0.1.exe"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " Building Share Bridge Setup Installer" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# 1. Self-contained Release build
Write-Host "`n[1/3] Building self-contained Release binaries..." -ForegroundColor Yellow
dotnet build "$rootDir\src\ShareBridge\ShareBridge.csproj" -c Release -r win-x64 --self-contained true

# Ensure Assets exist in output
$binAssets = "$rootDir\src\ShareBridge\bin\Release\net10.0-windows10.0.26100.0\win-x64\Assets"
if (-not (Test-Path $binAssets)) {
    New-Item -ItemType Directory -Path $binAssets -Force | Out-Null
}
Copy-Item -Recurse -Force "$rootDir\src\ShareBridge\Assets\*" "$binAssets\"

# 2. Compile with Inno Setup
Write-Host "`n[2/3] Compiling Inno Setup package..." -ForegroundColor Yellow
& $iscc "$issPath"

if (-not (Test-Path $outputExe)) {
    Write-Error "Installer binary was not produced at $outputExe"
    exit 1
}

# 3. Digitally sign the installer
Write-Host "`n[3/3] Digitally signing installer..." -ForegroundColor Yellow
if ((Test-Path $signtool) -and (Test-Path $pfx)) {
    & $signtool sign /fd SHA256 /a /f "$pfx" /p "Pass1234" "$outputExe"
} else {
    Write-Warning "SignTool or PFX not found; skipping digital signature."
}

$fileInfo = Get-Item $outputExe
Write-Host "`n[SUCCESS] ShareBridge-Setup-v1.0.1.exe created successfully!" -ForegroundColor Green
Write-Host "Location: $outputExe" -ForegroundColor Gray
Write-Host "Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
