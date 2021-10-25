Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host "Fetching submodules" -ForegroundColor White
git submodule update --recursive --init
if ($LastExitCode -ne 0) {
    throw "git failed with exit code $LastExitCode"
}

Write-Host "Creating stub .env" -ForegroundColor White
if (!(Test-Path './source/WebApp.Server/.env')) {
    Copy-Item './source/WebApp.Server/.env.template' './source/WebApp.Server/.env'
}

Write-Host "Installing local tools" -ForegroundColor White
dotnet tool restore
if ($LastExitCode -ne 0) {
    throw "dotnet failed with exit code $LastExitCode"
}

Write-Host "Installing azurite (global)" -ForegroundColor White
npm install azurite -g
if ($LastExitCode -ne 0) {
    throw "npm install failed with exit code $LastExitCode"
}

Write-Host "Preparing externals: mirrorsharp" -ForegroundColor White
Push-Location './source/#external/mirrorsharp/WebAssets'
try {
    npm ci
    if ($LastExitCode -ne 0) {
        throw "npm ci failed with exit code $LastExitCode"
    }

    npm run build
    if ($LastExitCode -ne 0) {
        throw "npm ci failed with exit code $LastExitCode"
    }
}
finally {
    Pop-Location
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
Write-Host "sl run" -ForegroundColor Cyan -NoNewLine
Write-Host " to start." -ForegroundColor White -NoNewLine