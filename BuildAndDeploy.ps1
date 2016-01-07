Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
$BuildRoslynBranch = Resolve-Path "$PSScriptRoot\Build\Build-RoslynBranch.ps1"
&"$PSScriptRoot\Build\Setup-Common.ps1"

$ImportDeployConfig = "$PSScriptRoot\Deploy.config.ps1"
if (!(Test-Path $ImportDeployConfig)) {
    Set-Content $ImportDeployConfig @"
Set-StrictMode -Version 2.0
`$ErrorActionPreference = 'Stop'
    
# This is an example build configuration -- feel free to change it
`$global:DeployConfig = @{
    Target = 'Azure' # No other choices for now
    Azure = @{
        ProfileFileName = '!azureprofile' # Create by using Save-AzureRmProfile
        ResourceGroupName = 'Default-Web-EastUS' # You probably want to change this if you deploy to Azure
    }
}
"@
    Write-Output "Created default config at $ImportDeployConfig -- please check it and restart this script."
    exit    
}
&$ImportDeployConfig

$DeploytoAzure = Resolve-Path "$PSScriptRoot\Build\Deploy-OneTryRoslynToAzure.ps1"

function Update-RoslynSource($directory) {
    Write-Output "Updating $directory"
    if (Test-Path $directory) {
        Push-Location $directory
        git config user.email "tryroslyn@github.test"
        git config user.name "TryRoslyn"
        git fetch origin
        Pop-Location
    }
    else {
        New-Item -ItemType directory -Path $directory | Out-Null
        git clone $repositoryUrl $directory
    }
}

function Relink-MainSource($sourceRoot, $branchBuildRoot) {
    Write-Output "Relinking $sourceRoot to $branchBuildRoot"
    Get-ChildItem $branchBuildRoot |
        ? { !$_.Name.StartsWith("!") -and $_.Name -ne '#roslyn' } |
        % { 
            if ($_ -is [IO.DirectoryInfo]) {
                Write-Output "  - junction $($_.Name)"
                cmd /c rmdir $($_.FullName)
            }
            else {
                Write-Output "  - hardlink $($_.Name)"
                Remove-Item $($_.FullName)
            }
        }
        
    Get-ChildItem $sourceRoot |
        ? { !$_.Name.StartsWith("!") -and $_.Name -ne '#roslyn' } |
        % {
            $isJunction = $_ -is [IO.DirectoryInfo]
            $linkType = $(if ($isJunction) { "/J" } else { "/H" })
            Write-Output "  + $(if ($isJunction) { "junction" } else { "hardlink" }) $($_.Name)"
            cmd /c mklink $linkType $("$branchBuildRoot\$($_.Name)") $($_.FullName) | Out-Null
        }
}

function Ensure-ResolvedPath($path) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType directory -Path $path | Out-Null    
    }
    return Resolve-Path $path
}

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "Build Roslyn" # prevents title > 1024 char errors

    #Write-Output "Killing VBCSCompiler instances"
    #taskkill /IM VBCSCompiler.exe /F

    Write-Output "Environment:"
    Write-Output "  Current Path:       $(Get-Location)"    
    Write-Output "  Script Root:        $PSScriptRoot"
    
    $sourceRoot = Resolve-Path "$PSScriptRoot\Source"
    Write-Output "  Source Root:        $sourceRoot"
 
    $buildRoot = Ensure-ResolvedPath "$PSScriptRoot\!build"
    Write-Output "  Build Root:         $buildRoot"
        
    $roslynBuildRoot = Ensure-ResolvedPath "$PSScriptRoot\!roslyn"
    Write-Output "  Roslyn Build Root:  $roslynBuildRoot"
    
    $roslynSourceRoot = Ensure-ResolvedPath "$roslynBuildRoot\root"
    Write-Output "  Roslyn Source Root: $roslynSourceRoot"
    
    $repositoryUrl = 'https://github.com/dotnet/roslyn.git'

    ${env:$HOME} = $PSScriptRoot
    git --version

    # Hack to make sure git does not traverse up
    #git init

    Write-Output "Updating..."
    Update-RoslynSource $roslynSourceRoot

    # TODO: This can be done in local now
    Write-Output "Requesting branches..."    
    $branchesRaw = (git ls-remote --heads $repositoryUrl)
    $branches = $branchesRaw | % { ($_ -match 'refs/heads/(.+)$') | Out-Null; $matches[1] }

    Write-Output "  $branches"
    $branchDomains = @{}
    $branches | % {
        Write-Output ''
        Write-Output "*** $_"        
        $branchFsName = $_ -replace '[/\\:]', '-'
        
        $branchBuildRoot = Ensure-ResolvedPath "$buildRoot\$branchFsName"
        $roslynBinaryRoot = Ensure-ResolvedPath "$branchBuildRoot\#roslyn"
        try {
            Push-Location $roslynBuildRoot
            try {            
                &$BuildRoslynBranch -SourceRoot $roslynSourceRoot -BranchName $_ -OutputRoot $roslynBinaryRoot
            }
            finally {                
                Pop-Location
            }
                        
            Relink-MainSource $sourceRoot $branchBuildRoot
            Push-Location $branchBuildRoot
            try {
                Write-Output "Building TryRoslyn.sln"                
                $buildLogPath = "$branchBuildRoot\!build.log"
                &$MSBuild TryRoslyn.sln > $buildLogPath
                if ($LastExitCode -ne 0) {
                    throw New-Object BranchBuildException("Build failed, see $buildLogPath", $buildLogPath)
                }
                Write-Output "TryRoslyn build done"
            }
            finally {
                Pop-Location
            }
            
            $webAppName = "tr-b-dotnet-$($branchFsName.ToLowerInvariant())"           
            &$DeploytoAzure `
                -ProfileFileName (Resolve-Path "$($DeployConfig.Azure.ProfileFileName)") `
                -ResourceGroupName $($DeployConfig.Azure.ResourceGroupName) `
                -WebAppName $webAppName `
                -SourceRoot $branchBuildRoot
                
            # Success!
            $branchDomains[$_] = $webAppName + '.azurewebsites.net'
        }
        catch {
            $ex = $_.Exception
            if ($ex -isnot [BranchBuildException]) {
                throw
            }
            
            Write-Output "  [WARNING] $($ex.Message)"
        }
    }
    
    Write-Output "Updating branches.json..."
    $branchesJson = Convert-ToJson $branchDomains    
    Set-Content "$buildRoot\branches.json" $branchesJson
    
    #Remove-Item .git -Force -Recurse
}
catch {    
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}