Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$MSBuild = ${env:ProgramFiles(x86)} + '\MSBuild\12.0\bin\MSBuild.exe'

$sourcesRoot = '!roslyn-sources'
$binariesRoot = '!roslyn-binaries'
$repositoryUrl = 'https://git01.codeplex.com/roslyn'

function Sync-Branch($directory, $branch) {
    if (Test-Path $directory) {
        Push-Location $directory 
        git pull origin $branch
        Pop-Location
    }
    else {
        New-Item -ItemType directory -Path $directory | Out-Null
        git clone -b $branch $repositoryUrl $directory
    }
}

function Build-Branch($directory) {
    $fsName = [IO.Path]::GetFileName($directory)

    Write-Host "Building $directory" -ForegroundColor White

    $buildLogPath = "$sourcesRoot\$fsName.build.log"
    Copy-Item NuGet.Roslyn.config "$directory\NuGet.config" -Force
    &$MSBuild "$directory\BuildAndTest.proj" /target:RestorePackages > "$buildLogPath"
    if ($LastExitCode -ne 0) {
        Write-Host "  Build failed, see $buildLogPath" -ForegroundColor Yellow
        return
    }      
    
    &$MSBuild "$directory\Src\Diagnostics\Roslyn\CSharp\CSharpRoslynDiagnosticAnalyzers.csproj" /p:RestorePackages=false /p:DelaySign=false /p:SignAssembly=false >> "$buildLogPath"
    if ($LastExitCode -ne 0) {
        Write-Host "  Build failed, see $buildLogPath" -ForegroundColor Yellow
        return
    }
  
    $binariesDirectory = "$binariesRoot\" + $fsName
    Remove-Item $binariesDirectory -Recurse -ErrorAction SilentlyContinue
    Move-Item "$directory\Binaries\Debug" $binariesDirectory
    Remove-Item "$directory\NuGet.config"
    
    Write-Host "  Build completed" -ForegroundColor Green
}

$branchesRaw = (git ls-remote --heads https://git01.codeplex.com/roslyn)
$branches = $branchesRaw | % { ($_ -match 'refs/heads/(.+)$') | Out-Null; $matches[1] }

if (-not (Test-Path $binariesRoot)) {
    New-Item -ItemType directory -Path $binariesRoot | Out-Null
}
$branches | % {
    $directory = "$sourcesRoot\" + $_.Replace('/', '-')
    Sync-Branch $directory $_
    Build-Branch $directory
}