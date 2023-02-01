param (
    [switch] [boolean] $ServerOnly = $false,
    [switch] [boolean] $NoCache = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host "Opening new window for initial wait" -ForegroundColor White

if ($IsWindows) {
    Start-Process PowerShell -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""
} else {
    Start-Process pwsh -ArgumentList "-File `"$PSScriptRoot/run/wait.ps1`""
}

$tags = @('--tags', 'server')
if (!$ServerOnly) { $tags += @('--tags', 'assets') }
if (!$NoCache) { $tags += @('--tags', 'cache') }

dotnet tye run --watch @tags