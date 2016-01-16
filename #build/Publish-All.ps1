param (
    [switch] [boolean] $azure,
    [string] $azureResourceGroupName = $(if ($azure) { throw "-AzureResourceGroupName must be specified is -Azure is specified." }),
    [string] $azureProfilePath = $(if ($azure) { throw "-AzureProfilePath must be specified is -Azure is specified." })
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
$PublishToIIS = Resolve-Path "$PSScriptRoot\Publish-ToIIS.ps1"
$PublishToAzure = Resolve-Path "$PSScriptRoot\Publish-ToAzure.ps1"

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "Deploy TryRoslyn" # prevents title > 1024 char errors

    Write-Output "Environment:"
    Write-Output "  Current Path:       $(Get-Location)"    
    Write-Output "  Script Root:        $PSScriptRoot"
    
    $sourceRoot = Resolve-Path "$PSScriptRoot\..\Source"
    Write-Output "  Source Root:        $sourceRoot"

    $sitesBuildRoot = Resolve-Path "$PSScriptRoot\..\!sites"
    Write-Output "  Sites Build Root:   $sitesBuildRoot"
        
    if ($azure) {
        $azureProfilePath = (Resolve-Path $azureProfilePath)
        Write-Host "Loading Azure profile from $azureProfilePath"
        Select-AzureRmProfile $azureProfilePath | Out-Null
    }  

    $branchesJson = @()
    Get-ChildItem $sitesBuildRoot | ? { $_ -is [IO.DirectoryInfo] } | % {
        $branchFsName = $_.Name

        $siteMainRoot = "$($_.FullName)\!site"
        if (!(Test-Path $siteMainRoot) -or !(Get-ChildItem $siteMainRoot -Recurse | ? { $_ -is [IO.FileInfo] })) {
            return
        }

        Write-Output ''
        Write-Output "*** $_"

        $siteMainRoot   = Resolve-Path $siteMainRoot
        $siteRoslynRoot = Resolve-Path "$($_.FullName)\!roslyn"        

        $branchInfo = ConvertFrom-Json ([IO.File]::ReadAllText("$siteRoslynRoot\!BranchInfo.json"))

        $webAppName = "tr-b-dotnet-$($branchFsName.ToLowerInvariant())"
        $iisSiteName = "$webAppName.tryroslyn.local"
        $url = "http://$iisSiteName"
        &$PublishToIIS -SiteName $iisSiteName -SourcePath $siteMainRoot
        
        if ($azure) {
            &$PublishToAzure `
                -ResourceGroupName $azureResourceGroupName `
                -WebAppName $webAppName `
                -CanCreateWebApp `
                -SourcePath $siteMainRoot `
                -TargetPath "."
            $url = "http://$($webAppName).azurewebsites.net"
        }
        
        # Success!
        $branchesJson += @{
            name = $branchInfo.name
            url = $url
            commits = $branchInfo.commits
        }
    }
    
    Write-Output "Updating branches.json..."
    Set-Content "$sitesBuildRoot\!branches.js" "angular.module('app').constant('branches', $(ConvertTo-Json $branchesJson -Depth 100));"
    Copy-Item "$sitesBuildRoot\!branches.js" "$sourceRoot\Web\App\!branches.js"
    
    if ($azure) {
        &$PublishToAzure `
            -ResourceGroupName $azureResourceGroupName `
            -WebAppName "tryroslyn" `
            -SourcePath "$sitesBuildRoot\!branches.js" `
            -TargetPath "App\!branches.js"
    }
}
catch {    
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}