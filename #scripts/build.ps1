param (
    $configuration = 'Debug'
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

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