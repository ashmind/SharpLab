Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
$BuildRoslynBranch = Resolve-Path "$PSScriptRoot\#build\Build-RoslynBranch.ps1"
&"$PSScriptRoot\#build\Setup-Common.ps1"

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

$DeploytoAzure = Resolve-Path "$PSScriptRoot\#build\Publish-ToAzure.ps1"

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

function Update-SiteSourceLinks([string] $sourceRoot, [string] $siteBuildRoot) {
    Write-Output "Relinking $sourceRoot to $siteBuildRoot"
    Get-ChildItem $siteBuildRoot |
        ? { !$_.Name.StartsWith("!") -and $_.Name -ne '#roslyn' } |
        % { 
            if ($_ -is [IO.DirectoryInfo]) {
                #Write-Output "  - junction $($_.Name)"
                cmd /c rmdir $($_.FullName)
            }
            else {
                #Write-Output "  - hardlink $($_.Name)"
                Remove-Item $($_.FullName)
            }
        }
        
    Get-ChildItem $sourceRoot |
        ? { !$_.Name.StartsWith("!") -and $_.Name -ne '#roslyn' } |
        % { 
            $targetPath = "$siteBuildRoot\$($_.Name)"
            if ($_ -is [IO.DirectoryInfo]) {                        
                New-Junction -SourcePath $($_.FullName) -TargetPath $targetPath
            }
            else {
                New-HardLink -SourcePath $($_.FullName) -TargetPath $targetPath
            }
        }
}

function New-Junction([string] $targetPath, [string] $sourcePath) {
    cmd /c mklink /J $targetPath $sourcePath | Out-Null
    if ($LastExitCode -ne 0) {
        throw "mklink failed with exit code $LastExitCode"
    }
}

function New-HardLink([string] $targetPath, [string] $sourcePath) {
    cmd /c mklink /H $targetPath $sourcePath | Out-Null
    if ($LastExitCode -ne 0) {
        throw "mklink failed with exit code $LastExitCode"
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
        
    $roslynBuildRoot = Ensure-ResolvedPath "$PSScriptRoot\!roslyn"
    Write-Output "  Roslyn Build Root:  $roslynBuildRoot"
    
    $roslynSourceRoot = Ensure-ResolvedPath "$roslynBuildRoot\root"
    Write-Output "  Roslyn Source Root: $roslynSourceRoot"
 
    $sitesBuildRoot = Ensure-ResolvedPath "$PSScriptRoot\!sites"
    Write-Output "  Sites Build Root:   $sitesBuildRoot"   
    
    $azureProfilePath = (Resolve-Path "$($DeployConfig.Azure.ProfileFileName)")
    Write-Host "Loading Azure profile from $azureProfilePath"
    Select-AzureRmProfile $azureProfilePath | Out-Null
    
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
    $branchDetails = @{}
    $branches | % {
        Write-Output ''
        Write-Output "*** $_"        
        $branchFsName = $_ -replace '[/\\:]', '-'
        
        $branchSiteBuildRoot = Ensure-ResolvedPath "$sitesBuildRoot\$branchFsName"
        $roslynBinaryRoot = Ensure-ResolvedPath "$branchSiteBuildRoot\#roslyn"
        $siteCopyRoot = Ensure-ResolvedPath "$branchSiteBuildRoot\!site"
        try {
            Push-Location $roslynBuildRoot
            try {            
                &$BuildRoslynBranch -SourceRoot $roslynSourceRoot -BranchName $_ -OutputRoot $roslynBinaryRoot
            }
            finally {                
                Pop-Location
            }
                        
            Update-SiteSourceLinks $sourceRoot $branchSiteBuildRoot
            Push-Location $branchSiteBuildRoot
            try {
                Write-Output "Building TryRoslyn.sln"                
                $buildLogPath = "$branchSiteBuildRoot\!build.log"
                &$MSBuild Web\Web.csproj > $buildLogPath `
                    /p:OutputPath=..\!site\bin\ `
                    /p:IntermediateOutputPath=..\!temp\ `
                    /p:AllowedReferenceRelatedFileExtensions=.pdb

                if ($LastExitCode -ne 0) {
                    throw New-Object BranchBuildException("Build failed, see $buildLogPath", $buildLogPath)
                }
                
                Copy-Item "Web\Web.config" "$siteCopyRoot\Web.config" -Force
                Write-Output "TryRoslyn build done"
            }
            finally {
                Pop-Location
            }
            
            $webAppName = "tr-b-dotnet-$($branchFsName.ToLowerInvariant())"           
            &$DeploytoAzure `
                -ResourceGroupName $($DeployConfig.Azure.ResourceGroupName) `
                -WebAppName $webAppName `
                -CanCreateWebApp `
                -SourcePath $siteCopyRoot `
                -TargetPath "."
                
            # Success!
            $branchDetails[$branchFsName] = @{
                url = "http://$($webAppName).azurewebsites.net"
            }
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
    $branchesJson = ConvertTo-Json $branchDetails    
    Set-Content "$sitesBuildRoot\!branches.json" $branchesJson
    &$DeploytoAzure `
        -ResourceGroupName $($DeployConfig.Azure.ResourceGroupName) `
        -WebAppName "tryroslyn" `
        -SourcePath "$sitesBuildRoot\!branches.json" `
        -TargetPath "Web\App\!branches.json"
                
    #Remove-Item .git -Force -Recurse
}
catch {    
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}