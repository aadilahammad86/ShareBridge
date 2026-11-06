# Uninstall-Package.ps1 - Share Bridge Clean Removal Script
$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Share Bridge - Package Removal" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$pkg = Get-AppxPackage -Name "ShareBridge.App" -ErrorAction SilentlyContinue
if ($pkg) {
    Write-Host "Removing package: $($pkg.PackageFullName)..." -ForegroundColor Yellow
    Remove-AppxPackage -Package $pkg.PackageFullName
    Write-Host "[SUCCESS] Share Bridge has been cleanly uninstalled." -ForegroundColor Green
} else {
    Write-Host "Share Bridge is not currently installed." -ForegroundColor DarkGray
}
