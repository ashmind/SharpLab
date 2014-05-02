Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
# Note: Write-Host, Write-Error and Write-Warning do not function properly in Azure

$MSBuild = ${env:ProgramFiles(x86)} + '\MSBuild\12.0\bin\MSBuild.exe'

# Functions ------
function Sync-Branch($directory, $branch) {
    Write-Output "Syncing $directory"
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

    Write-Output "Building $directory"

    $buildLogPath = "$sourcesRoot\$fsName.build.log"
    Copy-Item NuGet.Roslyn.config "$directory\NuGet.config" -Force
    &$MSBuild "$directory\BuildAndTest.proj" /target:RestorePackages > "$buildLogPath"
    if ($LastExitCode -ne 0) {
        Write-Output "  [WARNING] Build failed, see $buildLogPath"
        return
    }      
    
    &$MSBuild "$directory\Src\Compilers\CSharp\Source\CSharpCodeAnalysis.csproj" `
        /p:RestorePackages=false `
        /p:DelaySign=false `
        /p:SignAssembly=false `
        >> "$buildLogPath"

    if ($LastExitCode -ne 0) {
        Write-Output "  [WARNING] Build failed, see $buildLogPath"
        return
    }
  
    $binariesDirectory = "$binariesRoot\" + $fsName
    robocopy  "$directory\Binaries\Debug" $binariesDirectory /MIR
    Remove-Item "$directory\NuGet.config"
    
    Write-Output "  Build completed"
}

# Code ------
try {
    Write-Output "Environment:"
    Write-Output "Current path: $(Get-Location)"
    Write-Output "WEBROOT_PATH: ${env:WEBROOT_PATH}"
    Write-Output "   $(Resolve-Path $env:WEBROOT_PATH)"

    $sourcesRoot = "${env:WEBROOT_PATH}\..\!roslyn-sources"
    $binariesRoot = "${env:WEBROOT_PATH}\App_Data\RoslynBranches"
    $repositoryUrl = 'https://git01.codeplex.com/roslyn'

    if (-not (Test-Path $sourcesRoot)) {
        New-Item -ItemType directory -Path $sourcesRoot | Out-Null    
    }
    $sourcesRoot = Resolve-Path $sourcesRoot
    Write-Output "Sources Root: $sourcesRoot"

    if (-not (Test-Path $binariesRoot)) {
        New-Item -ItemType directory -Path $binariesRoot | Out-Null    
    }
    $binariesRoot =  Resolve-Path $binariesRoot
    Write-Output "Binaries Root: $binariesRoot"

    ${env:$HOME} = "${env:WEBROOT_PATH}\.."
    git --version

    # Hack to make sure git does not traverse up
    git init

    Write-Output "Requesting branches..."    
    $branchesRaw = (git ls-remote --heads $repositoryUrl)
    $branches = $branchesRaw | % { ($_ -match 'refs/heads/(.+)$') | Out-Null; $matches[1] }

    Write-Output "  $branches"
    $branches | % {
        Write-Output ''
        Write-Output "*** $_"
        $directory = "$sourcesRoot\" + $_.Replace('/', '-')
        Sync-Branch $directory $_
        Build-Branch $directory
    }
    
    Remove-Item .git -Force -Recurse
}
catch {
    $ErrorActionPreference = 'Continue'
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}