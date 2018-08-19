Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Write-Output 'git submodule update --recursive --init'
git submodule update --recursive --init

Write-Output 'dotnet restore source/WebApp/WebApp.csproj'
dotnet restore source/WebApp/WebApp.csproj

Write-Output 'dotnet msbuild source/WebApp/WebApp.csproj /p:Configuration=Release /p:UnbreakablePolicyReportEnabled=false /t:Publish /verbosity:minimal'
dotnet msbuild source/WebApp/WebApp.csproj /p:Configuration=Release /p:UnbreakablePolicyReportEnabled=false /t:Publish /verbosity:minimal

Write-Output 'Compress-Archive -Path source/WebApp/bin/publish/* -DestinationPath source/WebApp/bin/publish/WebApp.zip'
Compress-Archive -Path source/WebApp/bin/publish/* -DestinationPath source/WebApp/bin/publish/WebApp.zip