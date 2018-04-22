Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host Fetching submodules
git submodule update --recursive --init
if ($LastExitCode -ne 0) {
    throw "git failed with exit code $LastExitCode"
}

Write-Host Done