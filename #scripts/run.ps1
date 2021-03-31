Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$webappServerUrl = 'http://localhost:54100'

Write-Host "Opening new window for initial wait" -ForegroundColor White
Start-Process powershell -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""

dotnet tye run