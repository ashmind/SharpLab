Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host "Fetching submodules" -ForegroundColor White
git submodule update --recursive --init
if ($LastExitCode -ne 0) {
    throw "git failed with exit code $LastExitCode"
}

Write-Host "Creating stub .env" -ForegroundColor White
if (!(Test-Path './source/WebApp/.env')) {
    Copy-Item './source/WebApp/.env.template' './source/WebApp/.env'
}

Write-Host "Installing local tools" -ForegroundColor White
dotnet tool restore
if ($LastExitCode -ne 0) {
    throw "dotnet failed with exit code $LastExitCode"
}

Write-Host "Installing node modules" -ForegroundColor White
Push-Location './source/WebApp'
try {
    npm ci   
    if ($LastExitCode -ne 0) {
        throw "npm ci failed with exit code $LastExitCode"
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "SharpLab setup done." -ForegroundColor White
Write-Host "Run " -ForegroundColor White -NoNewLine
Write-Host "dotnet tye run" -ForegroundColor Cyan -NoNewLine
Write-Host " to start." -ForegroundColor White -NoNewLine