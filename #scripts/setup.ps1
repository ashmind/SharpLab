Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function New-SLSiteUnlessExists(
    [string] $hostName,
    [string] $relativePath,
    [switch] $use32bit = $false
) {
    if (Get-IISSite | ? { $_.Name -eq $hostName }) {
        return
    }

    $iisManager = Get-IISServerManager
    $appPool = $iisManager.ApplicationPools.Add($hostName)
    if ($use32bit) {
        $appPool.Enable32BitAppOnWin64 = $true
    }

    $site = New-IISSite `
        -Name $hostName `
        -PhysicalPath (Resolve-Path $relativePath) `
        -BindingInformation "*:80:$hostName" `
        -Passthru

    $site.Applications["/"].ApplicationPoolName = $hostName

    Write-Host "Please add to etc/hosts: $hostName" -ForegroundColor Magenta
}

Write-Host "Fetching submodules"
git submodule update --recursive --init
if ($LastExitCode -ne 0) {
    throw "git failed with exit code $LastExitCode"
}

Write-Host "Creating stub .env"
if (!(Test-Path './source/WebApp/.env')) {
    Copy-Item './source/WebApp/.env.template' './source/WebApp/.env'
}

Write-Host "Building and publishing"
Push-Location 'source'
try {
    dotnet build -c Debug
    if ($LastExitCode -ne 0) {
        throw "dotnet build failed with exit code $LastExitCode"
    }

    dotnet publish Server.AspNetCore -c Debug --no-restore --no-build
    if ($LastExitCode -ne 0) {
        throw "dotnet publish failed with exit code $LastExitCode"
    }
}
finally {
    Pop-Location
}

Write-Host "Creating web sites (IIS)"
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-IISCommitDelay
    New-SLSiteUnlessExists "sharplab.local" -RelativePath "source/WebApp" -Use32Bit
    New-SLSiteUnlessExists "sl-a-x64.sharplab.local" -RelativePath "source/Server.Owin"
    New-SLSiteUnlessExists "sl-a-core-x64.sharplab.local" -RelativePath "source/Server.AspNetCore/bin/Debug/netcoreapp3.0/publish"
    Stop-IISCommitDelay
}
else {
    Write-Warning "Skipping IIS setup. If you want to set up local sites, please run this script as an admin."
}

Write-Host "Generating stub !branches.json"
if (!(Test-Path './source/WebApp/wwwroot/!branches.json')) {
    $json = ConvertTo-Json @(
        @{ id = 'x64'; name = 'x64'; url = 'http://sl-a-x64.sharplab.local'; group = 'Platforms' },
        @{ id = 'core-x64'; name = '.NET Core x64'; url = 'http://sl-a-core-x64.sharplab.local'; group = 'Platforms' }
    )
    [IO.File]::WriteAllText("$(Get-Location)/source/WebApp/wwwroot/!branches.json", $json)
}

Write-Host "Done"