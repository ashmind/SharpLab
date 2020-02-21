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

Write-Host "Installing local tools"
dotnet tool restore
if ($LastExitCode -ne 0) {
    throw "dotnet failed with exit code $LastExitCode"
} 

Write-Host "Building and publishing"
Push-Location 'source'
try {
    Write-Output 'msbuild source\Native.Profiler\Native.Profiler.vcxproj'
    $msbuild = (@(Get-Item "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\msbuild.exe")[0]).FullName
    &$msbuild Native.Profiler\Native.Profiler.vcxproj /p:SolutionName=SharpLab

    dotnet build -c Debug
    if ($LastExitCode -ne 0) {
        throw "dotnet build failed with exit code $LastExitCode"
    }

    dotnet publish Server -c Debug --no-restore --no-build
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
    Stop-IISCommitDelay
}
else {
    Write-Warning "Skipping IIS setup. If you want to set up local sites, please run this script as an admin."
}

Write-Host "Generating stub !branches.json"
if (!(Test-Path './source/WebApp/wwwroot/!branches.json')) {
    $json = ConvertTo-Json @(
        @{ id = 'x64'; name = 'x64'; url = 'http://sl-a-x64.sharplab.local'; group = 'Platforms'; kind = 'platform' },
        @{ id = 'core-x64'; name = '.NET Core (x64)'; url = 'http://localhost:54100'; group = 'Platforms'; kind = 'platform' },
        @{ id = 'core-x64-profiled'; name = '.NET Core (x64, Profiler)'; url = 'http://localhost:54200'; group = 'Platforms'; kind = 'platform' }
    )
    [IO.File]::WriteAllText("$(Get-Location)/source/WebApp/wwwroot/!branches.json", $json)
}

Write-Host "Done"