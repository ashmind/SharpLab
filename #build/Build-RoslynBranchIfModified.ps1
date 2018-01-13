param (
    [Parameter(Mandatory=$true)] [string] $sourceRoot,
    [Parameter(Mandatory=$true)] [string] $branchName,
    [Parameter(Mandatory=$true)] [string] $artifactsRoot,
    [ScriptBlock] $ifBuilt = $null
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

."$PSScriptRoot\Setup-Build.ps1"

$branchFsName = $branchName -replace '[/\\:_]', '-'

$hashMarkerPath = "$artifactsRoot\BranchHash"
$hashMarkerPathFull = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($hashMarkerPath)

$newHash = (Invoke-Git $sourceRoot log "origin/$branchName" -n 1 --pretty=format:"%H")
if (Test-Path $hashMarkerPath) {
    $oldHash = [IO.File]::ReadAllText($hashMarkerPath)
    if ($oldHash -eq $newHash) {
        Write-Output "No changes since the last build."
        return
    }
}

[IO.File]::WriteAllText($hashMarkerPathFull, $newHash)

Write-Output "Resetting '$branchName'..."

Invoke-Git $sourceRoot checkout $branchName --force
Invoke-Git $sourceRoot reset --hard origin/$branchName
#Invoke-Git $sourceRoot clean --force
if (Test-Path "$sourceRoot\Binaries") {
    # We have to use robocopy to ensure long file names can be deleted:
    New-Item "$sourceRoot\Binaries_Empty"
    robocopy "$sourceRoot\Binaries_Empty" "$sourceRoot\Binaries" /MIR
    Remove-Item "$sourceRoot\Binaries_Empty" -Force
}

function Build-Project(
    [Parameter(Mandatory=$true)][string[]] $candidateProjectPaths,
    [string] $msbuildArgs
) {
    $projectPath = @($candidateProjectPaths | ? { Test-Path "$sourceRoot\$_" })[0];
    if (!$projectPath) {
        throw New-Object BranchBuildException("Project not found: none of @($candidateProjectPaths) matched.", $buildLogPath)
    }
    "  msbuild $projectPath $msbuildArgs" | Out-Default
    &$MSBuild $projectPath /m /p:Configuration=Release /p:DelaySign=false /p:SignAssembly=false /p:NeedsFakeSign=false /p:SolutionDir="$sourceRoot\Src" >> "$buildLogPath"
    if ($LastExitCode -ne 0) {
        throw New-Object BranchBuildException("Build failed, see $buildLogPath", $buildLogPath)
    }
}

Write-Output "Building '$branchName'..."

$buildLogPath = "$(Resolve-Path "$sourceRoot\..")\$([IO.Path]::GetFileName($sourceRoot))-$branchFsName.build.log"
if (Test-Path $buildLogPath) {
    Remove-Item $buildLogPath
}

Push-Location $sourceRoot
try {
    if (!(Test-Path '.\Restore.cmd')) {
        throw New-Object BranchBuildException("Build failed: Restore.cmd not found.", $buildLogPath)
    }
    Write-Output "  .\Restore.cmd"
    .\Restore.cmd >> "$buildLogPath"
        
    Build-Project "Src\Compilers\Core\Portable\CodeAnalysis.csproj"
    Build-Project "Src\Compilers\CSharp\Portable\CSharpCodeAnalysis.csproj"
    Build-Project "src\Features\CSharp\Portable\CSharpFeatures.csproj"
    Build-Project "Src\Tools\Source\CompilerGeneratorTools\Source\VisualBasicSyntaxGenerator\VisualBasicSyntaxGenerator.vbproj"
    Build-Project "Src\Compilers\VisualBasic\Portable\BasicCodeAnalysis.vbproj"
    Build-Project "src\Features\VisualBasic\Portable\BasicFeatures.vbproj"
}
finally {
    Pop-Location
}

robocopy "$sourceRoot\Binaries\Release" "$artifactsRoot\Binaries\Release" `
    /xd "$sourceRoot\Binaries\Release\Exes" `
    /xd "$sourceRoot\Binaries\Release\CompilerGeneratorTools" `
    /xd "runtimes" `
    /mir /np

Write-Output "  Build completed"

if ($ifBuilt) {
    &$ifBuilt
}