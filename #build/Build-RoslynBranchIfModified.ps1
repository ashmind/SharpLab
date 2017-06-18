param (
    [Parameter(Mandatory=$true)] [string] $sourceRoot,
    [Parameter(Mandatory=$true)] [string] $branchName,
    [Parameter(Mandatory=$true)] [string] $artifactsRoot,
    [ScriptBlock] $ifBuilt = $null
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Note: Write-Host, Write-Error and Write-Warning do not function properly in Azure
."$PSScriptRoot\Setup-Build.ps1"

$branchFsName = $branchName -replace '[/\\:_]', '-'

$hashMarkerPath = "$artifactsRoot\BranchHash"
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
if (Test-Path "$sourceRoot\Binaries") {
    Remove-Item "$sourceRoot\Binaries" -Recurse -Force
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
    if (!$projectPath) {
        throw New-Object BranchBuildException("Project not found: none of @($candidateProjectPaths) matched.", $buildLogPath)
    }
    "  $projectPath $msbuildArgs" | Out-Default

    $projectPath = "$sourceRoot\$projectPath"
    "    nuget restore" | Out-Default
    nuget restore "$projectPath" >> "$buildLogPath"
    if ($LastExitCode -ne 0) {
        throw New-Object BranchBuildException("Build failed, see $buildLogPath", $buildLogPath)
    }
    
    $msbuild = $env:MSBUILD_PATH
    "    msbuild ($msbuild)" | Out-Default
    Invoke-Expression ("&`"$msbuild`" `"$projectPath`" $msbuildArgs >> `"$buildLogPath`"")
    if ($LastExitCode -ne 0) {
        throw New-Object BranchBuildException("Build failed, see $buildLogPath", $buildLogPath)
    }
}

$standardArgs = "/nodeReuse:false /m /p:RestorePackages=false /p:Configuration=Debug /p:DelaySign=false /p:SignAssembly=false /p:NeedsFakeSign=false /p:SolutionDir=`"$sourceRoot\Src`""
Build-Project "Src\Compilers\Core\Portable\CodeAnalysis.csproj" $standardArgs
Build-Project "Src\Compilers\CSharp\Portable\CSharpCodeAnalysis.csproj" $standardArgs
Build-Project "src\Features\CSharp\Portable\CSharpFeatures.csproj" $standardArgs
Build-Project "Src\Tools\Source\CompilerGeneratorTools\Source\VisualBasicSyntaxGenerator\VisualBasicSyntaxGenerator.vbproj" $standardArgs
Build-Project "Src\Compilers\VisualBasic\Portable\BasicCodeAnalysis.vbproj" "$standardArgs /p:IldasmPath=`"$(Resolve-Path "$sourceRoot\..\..\!tools\ildasm.exe")`""
Build-Project "src\Features\VisualBasic\Portable\BasicFeatures.vbproj" $standardArgs

if (Test-Path "$sourceRoot\NuGet.config") {
    Remove-Item "$sourceRoot\NuGet.config"
}

robocopy "$sourceRoot\Binaries\Debug" "$artifactsRoot\Binaries\Debug" `
    /xd "$sourceRoot\Binaries\Debug\Exes" `
    /xd "$sourceRoot\Binaries\Debug\CompilerGeneratorTools" `
    /xd "runtimes" `
    /mir /np

[IO.File]::WriteAllText($hashMarkerPathFull, $newHash)

Write-Output "  Build completed"

if ($ifBuilt) {
    &$ifBuilt
}