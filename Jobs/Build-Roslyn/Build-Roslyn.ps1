Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

# Note: Write-Host, Write-Error and Write-Warning do not function properly in Azure
$MSBuild = ${env:ProgramFiles(x86)} + '\MSBuild\12.0\bin\MSBuild.exe'
$WebHookUrl = ${env:WEBJOB_BUILD_WEBHOOK_URL}

# Functions ------
function Send-Notification(
    [Parameter(Mandatory=$true)][string]$title,
    [Parameter(Mandatory=$true)][string]$message,
    [string]$logPath
) {
    if ($WebHookUrl -eq $null) {
        return
    }
   
    "Sending notification to webhook at $WebHookUrl" | Out-Default
    $json = @{
      title="Build-Roslyn.ps1: $title";
      message=$message;
      log=[IO.File]::ReadAllText($logPath);
    } | ConvertTo-Json
    
    Invoke-RestMethod $WebHookUrl `
                      -Method Post `
                      -Body $json `
                      -ContentType 'application/json' `
                      -ErrorAction Continue | Out-Null
}

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

function Build-Branch($directory, $branch) {
    $fsName = [IO.Path]::GetFileName($directory)

    Write-Output "Building $directory"

    $buildLogPath = "$sourcesRoot\$fsName.build.log"
    if (Test-Path $buildLogPath) {
        Remove-Item $buildLogPath
    }
        
    function Build-Project(
        [Parameter(Mandatory=$true)][string[]] $candidateProjectPaths,
        [string] $msbuildArgs
    ) {    
        $projectPath = @($candidateProjectPaths | ? { Test-Path "$directory\$_" })[0];        
        "  $projectPath" | Out-Default
        
        $projectPath = "$directory\$projectPath"
        Invoke-Expression $("&`"$MSBuild`" `"$projectPath`" $msbuildArgs >> `"$buildLogPath`"")
        if ($LastExitCode -ne 0) {
            "  [WARNING] Build failed, see $buildLogPath" | Out-Default
            Send-Notification -Title "'$branch' build failed" -Message "See attached log for details" -LogPath $buildLogPath
            return $false
        }
        return $true
    }

    if (Test-Path "$directory\BuildAndTest.proj") {
        Copy-Item NuGet.Roslyn.config "$directory\NuGet.config" -Force
        if (!(Build-Project BuildAndTest.proj "/target:RestorePackages")) { return }
    }
        
    $standardArgs = "/p:RestorePackages=false /p:Configuration=Debug /p:DelaySign=false /p:SignAssembly=false /p:NeedsFakeSign=false /p:SolutionDir=`"$directory\Src`""
    $csCandidates = @(
        "Src\Compilers\CSharp\Portable\CSharpCodeAnalysis.csproj",
        "Src\Compilers\CSharp\Source\CSharpCodeAnalysis.csproj",
        "Src\Compilers\CSharp\Desktop\CSharpCodeAnalysis.Desktop.csproj"
    );
    if (!(Build-Project $csCandidates $standardArgs)) { return }
    
    $vbSyntaxGenerator = "Src\Tools\Source\CompilerGeneratorTools\Source\VisualBasicSyntaxGenerator\VisualBasicSyntaxGenerator.vbproj";
    if (!(Build-Project $vbSyntaxGenerator $standardArgs)) { return }
    
    $vbCandidates = @(
        "Src\Compilers\VisualBasic\Portable\BasicCodeAnalysis.vbproj",
        "Src\Compilers\VisualBasic\Source\BasicCodeAnalysis.vbproj",
        "Src\Compilers\VisualBasic\Desktop\BasicCodeAnalysis.Desktop.vbproj"
    );
    if (!(Build-Project $vbCandidates "$standardArgs /p:IldasmPath=`"$toolsRoot\ildasm.exe`"")) { return }
    
    if (Test-Path "$directory\NuGet.config") {
        Remove-Item "$directory\NuGet.config"
    }

    $binariesDirectory = "$binariesRoot\" + $fsName
    robocopy "$directory\Binaries\Debug" $binariesDirectory /MIR /XF "LastCommit.txt"
    Copy-Item "$sourcesRoot\$fsName.lastcommit.txt" -Destination "$binariesDirectory\LastCommit.txt"
    
    Write-Output "  Build completed"
}

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "Build Roslyn" # prevents title > 1024 char errors

    #Write-Output "Killing VBCSCompiler instances"
    #taskkill /IM VBCSCompiler.exe /F

    Write-Output "Environment:"
    Write-Output "Current path: $(Get-Location)"
    Write-Output "WEBROOT_PATH: ${env:WEBROOT_PATH}"
    Write-Output "WebHookUrl: $WebHookUrl"
    
    $webRoot = Resolve-Path $env:WEBROOT_PATH
    Write-Output "   $webRoot"
    
    $webRoot = Resolve-Path $env:WEBROOT_PATH
    $sourcesRoot = "$webRoot\..\!roslyn-sources"
    $toolsRoot = "$webRoot\..\!roslyn-build-tools"
    $binariesRoot = "$webRoot\App_Data\RoslynBranches"
    $repositoryUrl = 'https://github.com/dotnet/roslyn.git'

    if (-not (Test-Path $sourcesRoot)) {
        New-Item -ItemType directory -Path $sourcesRoot | Out-Null    
    }
    $sourcesRoot = Resolve-Path $sourcesRoot
    Write-Output "Sources Root: $sourcesRoot"
    
    if (-not (Test-Path $toolsRoot)) {
        Throw "Path not found: $toolsRoot"
    }
    $toolsRoot =  Resolve-Path $toolsRoot
    Write-Output "Tools Root: $toolsRoot"

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
        Build-Branch $directory $_
    }
    
    Remove-Item .git -Force -Recurse
    [IO.File]::SetLastWriteTime("$webRoot\web.config", [DateTime]::Now)
    
    #Write-Output "Killing VBCSCompiler instances"
    #taskkill /IM VBCSCompiler.exe /F
}
catch {    
    Write-Output "[ERROR] $_"
    Send-Notification -Title "Critical failure" -Message $_
    Write-Output 'Returning exit code 1'
    exit 1
}