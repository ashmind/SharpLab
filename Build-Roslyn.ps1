$ErrorActionPreference = 'Stop'
$MSBuild = ${env:ProgramFiles(x86)} + '\MSBuild\12.0\bin\MSBuild.exe'

Copy-Item NuGet.Roslyn.config '#roslyn\NuGet.config' -Force
&$MSBuild '#roslyn\BuildAndTest.proj' /target:RestorePackages
&$MSBuild '#roslyn\Src\Diagnostics\Roslyn\CSharp\CSharpRoslynDiagnosticAnalyzers.csproj' /p:RestorePackages=false /p:DelaySign=false /p:SignAssembly=false /v:diag
Remove-Item '!roslyn-binaries'
Move-Item '#roslyn\Binaries\Debug' '!roslyn-binaries'
Remove-Item '#roslyn\NuGet.config'