# Install-DevPackage.ps1 - Share Bridge Sideloading Script
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Share Bridge - Development Installation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$cerPath = Join-Path $scriptDir "ShareBridge_Dev.cer"
$msixPath = Join-Path $scriptDir "ShareBridge_1.0.0.0_x64.msix"

if (-not (Test-Path $cerPath)) {
    Write-Error "Certificate file not found at $cerPath"
}
if (-not (Test-Path $msixPath)) {
    Write-Error "MSIX package not found at $msixPath"
}

# 1. Trust Certificate
Write-Host "`n[1/2] Installing signing certificate to TrustedPeople..." -ForegroundColor Yellow
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if ($isAdmin) {
    certutil -addstore TrustedPeople "$cerPath"
} else {
    Write-Host "Requesting elevation to install root certificate..." -ForegroundColor DarkYellow
    Start-Process powershell -Verb RunAs -Wait -ArgumentList "-NoProfile -Command certutil -addstore TrustedPeople '$cerPath'"
}

# 2. Install MSIX Package
Write-Host "`n[2/2] Installing ShareBridge.App package..." -ForegroundColor Yellow
Add-AppxPackage -Path "$msixPath"

# 3. Verification
$installed = Get-AppxPackage -Name "ShareBridge.App" -ErrorAction SilentlyContinue
if ($installed) {
    Write-Host "`n[SUCCESS] Share Bridge is successfully installed!" -ForegroundColor Green
    Write-Host "Package Full Name: $($installed.PackageFullName)" -ForegroundColor Gray
    Write-Host "`nYou can now right-click any file in File Explorer -> Share -> choose 'Share Bridge'!" -ForegroundColor Cyan
} else {
    Write-Warning "Installation finished, but package could not be verified via Get-AppxPackage."
}
