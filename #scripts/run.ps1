param (
    [switch] [boolean] $ServerOnly = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host "Opening new window for initial wait" -ForegroundColor White
Start-Process powershell -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""

if (!$ServerOnly) {
  dotnet tye run --watch
}
else {
  dotnet tye run --watch --tags server-only
}