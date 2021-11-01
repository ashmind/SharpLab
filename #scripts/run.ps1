Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$webappServerUrl = 'http://localhost:44100'

Write-Host "Opening new window for initial wait" -ForegroundColor White

if ($IsWindows) {
    Start-Process PowerShell -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""
} else {
    Start-Process pwsh -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""
}

dotnet tye run --watch