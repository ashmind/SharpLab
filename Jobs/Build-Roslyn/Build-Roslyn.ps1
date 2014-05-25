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
    }
    else {
        New-Item -ItemType directory -Path $directory | Out-Null
        git clone -b $branch $repositoryUrl $directory
        Push-Location $directory
    }

    $directoryName = [IO.Path]::GetFileName($directory)    
    git log -n 1 --pretty=format:"%H %cd %aN%n%B" --date=short > "..\$directoryName.lastcommit.txt"
    Pop-Location
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
    
    &$MSBuild "$directory\Src\Tools\Source\CompilerGeneratorTools\Source\VisualBasicSyntaxGenerator\VisualBasicSyntaxGenerator.vbproj" `
        /p:RestorePackages=false `
        /p:DelaySign=false `
        /p:SignAssembly=false `
        /p:Configuration=Debug `
        >> "$buildLogPath"
        
    if ($LastExitCode -ne 0) {
        Write-Output "  [WARNING] Build failed, see $buildLogPath"
        return
    }
    
    &$MSBuild "$directory\Src\Compilers\VisualBasic\Source\BasicCodeAnalysis.vbproj" `
        /p:RestorePackages=false `
        /p:DelaySign=false `
        /p:SignAssembly=false `
        /p:Configuration=Debug `
        >> "$buildLogPath"

    if ($LastExitCode -ne 0) {
        Write-Output "  [WARNING] Build failed, see $buildLogPath"
        return
    }
  
    $binariesDirectory = "$binariesRoot\" + $fsName
    robocopy "$directory\Binaries\Debug" $binariesDirectory /MIR /XF "LastCommit.txt"
    Copy-Item "$sourcesRoot\$fsName.lastcommit.txt" -Destination "$binariesDirectory\LastCommit.txt"
    Remove-Item "$directory\NuGet.config"
    
    Write-Output "  Build completed"
}

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "Build Roslyn" # prevents title > 1024 char errors

    Write-Output "Environment:"
    Write-Output "Current path: $(Get-Location)"
    Write-Output "WEBROOT_PATH: ${env:WEBROOT_PATH}"
    
    $webRoot = Resolve-Path $env:WEBROOT_PATH
    Write-Output "   $webRoot"
    
    $webRoot = Resolve-Path $env:WEBROOT_PATH
    $sourcesRoot = "$webRoot\..\!roslyn-sources"
    $binariesRoot = "$webRoot\App_Data\RoslynBranches"
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

    ${env:$HOME} = "$webRoot\.."
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
    [IO.File]::SetLastWriteTime("$webRoot\web.config", [DateTime]::Now)
}
catch {
    $ErrorActionPreference = 'Continue'
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}