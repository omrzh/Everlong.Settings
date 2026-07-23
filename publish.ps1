param(
  [ValidateSet("Debug", "Release")]
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $repoRoot "publish"

# Clean publish directory
if (Test-Path $publishDir) {
  Remove-Item -Path "$publishDir\*" -Recurse -Force
} else {
  New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
}

Write-Host "=== Building & packing Everlong.Settings ($Configuration) ===" -ForegroundColor Cyan

# Restore & build everything
dotnet restore "$repoRoot\Everlong.Settings.slnx"
dotnet build "$repoRoot\Everlong.Settings.slnx" `
  --configuration $Configuration `
  --no-restore

# Pack the runtime library
dotnet pack "$repoRoot\src\Everlong.Settings\Everlong.Settings.csproj" `
  --configuration $Configuration `
  --no-build `
  --output $publishDir

Write-Host ""
Write-Host "=== Published packages ===" -ForegroundColor Green
Get-ChildItem $publishDir -Filter "*.nupkg" | ForEach-Object {
  Write-Host "  $($_.Name)" -ForegroundColor White
}
Write-Host ""
Write-Host "Done -> $publishDir" -ForegroundColor Green
