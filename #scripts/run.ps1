param (
    [switch] [boolean] $ServerOnly = $false,
    [switch] [boolean] $NoCache = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host "Opening new window for initial wait" -ForegroundColor White
Start-Process powershell -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""

$tags = @('server')
if (!$ServerOnly) { $tags += 'assets' }
if (!$NoCache) { $tags += 'cache' }

dotnet tye run --watch --tags ($tags -join ',')