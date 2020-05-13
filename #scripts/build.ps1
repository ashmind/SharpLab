param (
    $configuration = 'Debug'
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Output 'source'
Push-Location "$PSScriptRoot\..\source"
try {
    Write-Output 'msbuild Native.Profiler\Native.Profiler.vcxproj'
    $msbuild = (@(Get-Item "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\msbuild.exe")[0]).FullName
    &$msbuild Native.Profiler\Native.Profiler.vcxproj /p:SolutionName=SharpLab /p:Configuration=$configuration /p:Platform=x64
    if ($LastExitCode -ne 0) {
        throw "msbuild failed with exit code $LastExitCode"
    }

    try {
        Copy-Item 'SharpLab.sln' 'SharpLab.Build.sln'
        dotnet sln SharpLab.Build.sln remove Native.Profiler\Native.Profiler.vcxproj
        if ($LastExitCode -ne 0) {
            throw "dotnet sln failed with exit code $LastExitCode"
        }
        
        Write-Output 'dotnet build'
        dotnet build SharpLab.Build.sln -c $configuration /p:UnbreakablePolicyReportEnabled=false
        if ($LastExitCode -ne 0) {
            throw "dotnet build failed with exit code $LastExitCode"
        }
    }
    finally {
        Remove-Item 'SharpLab.Build.sln' -ErrorAction Continue
    }
}
finally {
    Pop-Location
}

Write-Output 'source\WebApp'
Push-Location $PSScriptRoot\..\source\WebApp
try {
    if ($env:NODE_ENV -eq 'production') {
        throw "Build prerequisites cannot be installed with NODE_ENV=production."
    }

    Write-Output '  npm install'
    npm install
    if ($LastExitCode -ne 0) { throw "npm install exited with code $LastExitCode" }
    
    Write-Output '  npm run build'
    npm run build
    if ($LastExitCode -ne 0) { throw "npm run build exited with code $LastExitCode" }
}
finally {
    Pop-Location
}