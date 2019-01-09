Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host Fetching submodules
git submodule update --recursive --init
if ($LastExitCode -ne 0) {
    throw "git failed with exit code $LastExitCode"
}

Write-Host Creating stub .env
if (!(Test-Path './source/WebApp/.env')) {
    Copy-Item './source/WebApp/.env.template' './source/WebApp/.env'
}

Write-Host Done