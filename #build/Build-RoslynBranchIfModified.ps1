param (
    [Parameter(Mandatory=$true)] [string] $sourceRoot,
    [Parameter(Mandatory=$true)] [string] $branchName,
    [Parameter(Mandatory=$true)] [string] $outputRoot,
    [ScriptBlock] $ifBuilt = $null
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Note: Write-Host, Write-Error and Write-Warning do not function properly in Azure
."$PSScriptRoot\Setup-Build.ps1"

$branchFsName = $branchName -replace '[/\\:_]', '-'

$hashMarkerPath = "$outputRoot\!BranchHash"
$hashMarkerPathFull = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($hashMarkerPath)

$newHash = (Invoke-Git $sourceRoot log "origin/$branchName" -n 1 --pretty=format:"%H")

if (Test-Path $hashMarkerPath) {
    $oldHash = [IO.File]::ReadAllText($hashMarkerPath)
    if ($oldHash -eq $newHash) {
        Write-Output "Branch '$branchName' is up to date."
        return
    }
}

Write-Output "Resetting '$branchName'..."

Invoke-Git $sourceRoot checkout $branchName --force
Invoke-Git $sourceRoot reset --hard origin/$branchName
#Invoke-Git $sourceRoot clean --force
if (Test-Path Binaries) {
    Remove-Item Binaries -Recurse -Force
}

Write-Output "Building '$branchName'..."

$buildLogPath = "$(Resolve-Path "$sourceRoot\..")\$([IO.Path]::GetFileName($sourceRoot))-$branchFsName.build.log"
if (Test-Path $buildLogPath) {
    Remove-Item $buildLogPath
}

function Build-Project(
    [Parameter(Mandatory=$true)][string[]] $candidateProjectPaths,
    [string] $msbuildArgs
) {
    $projectPath = @($candidateProjectPaths | ? { Test-Path "$sourceRoot\$_" })[0];
    "  $projectPath $msbuildArgs" | Out-Default

    $projectPath = "$sourceRoot\$projectPath"
    Invoke-Expression $("&`"$MSBuild`" `"$projectPath`" $msbuildArgs >> `"$buildLogPath`"")
    if ($LastExitCode -ne 0) {
        throw New-Object BranchBuildException("Build failed, see $buildLogPath", $buildLogPath)
    }
}

function Restore-Packages() {
   #if (Test-Path "$sourceRoot\.nuget\NuGetRestore.ps1") {
   #    "  .nuget\NuGetRestore.ps1" | Out-Default
   #     &"$sourceRoot\.nuget\NuGetRestore.ps1"
   #}

    Push-Location $sourceRoot
    try {
        if (Test-Path "Restore.cmd") {
            "  Restore.cmd" | Out-Default
            Invoke-Expression "cmd /c Restore.cmd"
            if ($LastExitCode -ne 0) {
                throw New-Object BranchBuildException("Restore failed, see $buildLogPath", $buildLogPath)
            }
            return
        }

        if (Test-Path "BuildAndTest.proj") {
            $buildContent = [IO.File]::ReadAllText((Resolve-Path "BuildAndTest.proj"))
            if ($buildContent -match 'RestorePackages') {
                #Copy-Item NuGet.Roslyn.config "NuGet.config" -Force
                Build-Project BuildAndTest.proj "/target:RestorePackages"
                return
            }
        }

        throw New-Object BranchBuildException("Failed to find a NuGet restore strategy.")
    }
    finally {
        Pop-Location
    }
}

Restore-Packages

$standardArgs = "/p:RestorePackages=false /p:Configuration=Debug /p:DelaySign=false /p:SignAssembly=false /p:NeedsFakeSign=false /p:SolutionDir=`"$sourceRoot\Src`""
$csCandidates = @(
    "Src\Compilers\CSharp\Portable\CSharpCodeAnalysis.csproj",
    "Src\Compilers\CSharp\Source\CSharpCodeAnalysis.csproj",
    "Src\Compilers\CSharp\Desktop\CSharpCodeAnalysis.Desktop.csproj"
);
Build-Project $csCandidates $standardArgs

$vbSyntaxGenerator = "Src\Tools\Source\CompilerGeneratorTools\Source\VisualBasicSyntaxGenerator\VisualBasicSyntaxGenerator.vbproj";
Build-Project $vbSyntaxGenerator $standardArgs

$vbCandidates = @(
    "Src\Compilers\VisualBasic\Portable\BasicCodeAnalysis.vbproj",
    "Src\Compilers\VisualBasic\Source\BasicCodeAnalysis.vbproj",
    "Src\Compilers\VisualBasic\Desktop\BasicCodeAnalysis.Desktop.vbproj"
);
Build-Project $vbCandidates "$standardArgs /p:IldasmPath=`"$(Resolve-Path "!tools\ildasm.exe")`""

if (Test-Path "$sourceRoot\NuGet.config") {
    Remove-Item "$sourceRoot\NuGet.config"
}

robocopy "$sourceRoot\Binaries\Debug" $outputRoot /MIR /np
[IO.File]::WriteAllText($hashMarkerPathFull, $newHash)

Write-Output "  Build completed"

if ($ifBuilt) {
    &$ifBuilt
}