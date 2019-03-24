Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

Write-Host "Build" -ForegroundColor White
&"$PSScriptRoot/roslyn-branches/Build-All.ps1"

Write-Host "Publish (IIS)" -ForegroundColor White
&"$PSScriptRoot/roslyn-branches/Publish-All.ps1"