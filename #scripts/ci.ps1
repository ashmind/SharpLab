Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$SolutionRoot = Resolve-Path "$PSScriptRoot/.."

Write-Output 'git submodule update --recursive --init'
git submodule update --recursive --init

&"$PSScriptRoot\build.ps1" -Configuration Release

Write-Output 'dotnet publish source/Server/Server.csproj ...'
dotnet publish source/Server/Server.csproj -c Release --no-build --no-restore
if ($LastExitCode -ne 0) { throw "dotnet publish exited with code $LastExitCode" }
$serverPublishRoot = 'source/Server/bin/Release/net6.0/publish'
Write-Output "Compress-Archive -Path $serverPublishRoot/* -DestinationPath $SolutionRoot/Server.zip"
Compress-Archive -Path "$serverPublishRoot/*" -DestinationPath "$SolutionRoot/Server.zip"

Write-Output 'dotnet msbuild source/NetFramework/Server/Server.csproj /t:Publish ...'
dotnet msbuild source/NetFramework/Server/Server.csproj /t:Publish /p:Configuration=Release /p:UnbreakablePolicyReportEnabled=false /verbosity:minimal
if ($LastExitCode -ne 0) { throw "dotnet msbuild exited with code $LastExitCode" }
$netfxPublishRoot = 'source/NetFramework/Server/bin/publish'
Write-Output "Compress-Archive -Path $netfxPublishRoot/* -DestinationPath $SolutionRoot/Server.NetFramework.zip"
Compress-Archive -Path "$netfxPublishRoot/*" -DestinationPath "$SolutionRoot/Server.NetFramework.zip"

Write-Output 'dotnet publish source/WebApp.Server/WebApp.Server.csproj ...'
dotnet publish source/WebApp.Server/WebApp.Server.csproj -c Release --no-build --no-restore
if ($LastExitCode -ne 0) { throw "dotnet publish exited with code $LastExitCode" }
$webAppPublishRoot = 'source/WebApp.Server/bin/Release/net6.0/publish'
Write-Output "Compress-Archive -Path $webAppPublishRoot/* -DestinationPath $SolutionRoot/WebApp.Server.zip"
Compress-Archive -Path "$webAppPublishRoot/*" -DestinationPath "$SolutionRoot/WebApp.Server.zip"