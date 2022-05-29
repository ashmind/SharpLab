param (
    [switch] [boolean] $ServerOnly = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host "Opening new window for initial wait" -ForegroundColor White
Start-Process powershell -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""

$tags = $ServerOnly ? @('server-only') : @();

dotnet tye run --watch --tags $tags