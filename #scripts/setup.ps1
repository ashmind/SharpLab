Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Host "Fetching submodules" -ForegroundColor White
git submodule update --recursive --init
if ($LastExitCode -ne 0) {
    throw "git failed with exit code $LastExitCode"
}

Write-Host "Creating stub .envs" -ForegroundColor White
if (!(Test-Path './source/WebApp.Server/.env')) {
    Copy-Item './source/WebApp.Server/.env.template' './source/WebApp.Server/.env'
}

if (!(Test-Path './source/Container.Manager/.env')) {
    Copy-Item './source/Container.Manager/.env.template' './source/Container.Manager/.env'
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
$azuriteTempPath = './!azurite'
if (-not (Test-Path $azuriteTempPath)) {
    New-Item $azuriteTempPath -Type Directory | Out-Null
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

Write-Host "Preparing container host" -ForegroundColor White
Push-Location './source/Container.Manager'
try {
    dotnet build
    if ($LastExitCode -ne 0) {
        throw "dotnet build failed with exit code $LastExitCode"
    }

    $binPath = './bin/Debug/net6.0'
    $containerCapabilityId = New-Object Security.Principal.SecurityIdentifier @(
        'S-1-15-3-1024-4233803318-1181731508-1220533431-3050556506-2713139869-1168708946-594703785-1824610955'
    )
    $aclRule = New-Object Security.AccessControl.FileSystemAccessRule @(
        $containerCapabilityId,
        [Security.AccessControl.FileSystemRights]::Read,
        ([Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit),
        [Security.AccessControl.PropagationFlags]::None,
        [Security.AccessControl.AccessControlType]::Allow
    )
    $acl = Get-Acl $binPath
    $acl.AddAccessRule($aclRule);
    Set-Acl $binPath -AclObject $acl
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "SharpLab setup done." -ForegroundColor White
Write-Host "Run " -ForegroundColor White -NoNewLine
Write-Host "sl run" -ForegroundColor Cyan -NoNewLine
Write-Host " to start." -ForegroundColor White -NoNewLine