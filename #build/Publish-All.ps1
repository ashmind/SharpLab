param (
    [switch] [boolean] $azure
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
$PublishToIIS = Resolve-Path "$PSScriptRoot\Publish-ToIIS.ps1"
$PublishToAzure = Resolve-Path "$PSScriptRoot\Publish-ToAzure.ps1"

function Login-ToAzure($azureConfig) {
    $passwordKey = $env:TR_AZURE_PASSWORD_KEY
    if (!$passwordKey) {
        throw "Azure credentials require TR_AZURE_PASSWORD_KEY to be set."
    }
    $passwordKey = [Convert]::FromBase64String($passwordKey)
    $password = $azureConfig.Password | ConvertTo-SecureString -Key $passwordKey
    $credential = New-Object Management.Automation.PSCredential($azureConfig.UserName, $password)

    "Logging to Azure as $($azureConfig.UserName)..." | Out-Default
    Login-AzureRmAccount -Credential $credential | Out-Null
}

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
        $azureConfigPath = ".\!Azure.config.json"
        if (!(Test-Path $azureConfigPath)) {
            throw "Path '$azureConfigPath' was not found."
        }
        $azureConfig = ConvertFrom-Json (Get-Content $azureConfigPath -Raw)
        Login-ToAzure $azureConfig
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

        $webAppName = "tr-b-$($branchFsName.ToLowerInvariant())"
        if ($webAppName.Length -gt 63) {             
             $webAppName = $webAppName.Substring(0, 60) + "-01"; # no uniqueness check at the moment, we can add later
             Write-Output "[WARNING] Name is too long, using '$webAppName'."
        }
        
        $iisSiteName = "$webAppName.tryroslyn.local"
        $url = "http://$iisSiteName"
        &$PublishToIIS -SiteName $iisSiteName -SourcePath $siteMainRoot
        
        if ($azure) {
            &$PublishToAzure `
                -ResourceGroupName $($azureConfig.ResourceGroupName) `
                -AppServicePlanName $($azureConfig.AppServicePlanName) `
                -WebAppName $webAppName `
                -CanCreateWebApp `
                -CanStopWebApp `
                -SourcePath $siteMainRoot `
                -TargetPath "."
            $url = "http://$($webAppName).azurewebsites.net"
        }
        
        # Success!
        $branchesJson += [ordered]@{
            id = $branchFsName -replace '^dotnet-',''
            name = $branchInfo.name
            group = $branchInfo.repository
            url = $url
            commits = $branchInfo.commits
        }
    }
    
    $branchesFileName = "!branches.json"
    Write-Output "Updating $branchesFileName..."
    Set-Content "$sitesBuildRoot\$branchesFileName" $(ConvertTo-Json $branchesJson -Depth 100)

    $brachesJsLocalRoot = "$sourceRoot\Web\wwwroot"
    if (!(Test-Path $brachesJsLocalRoot)) {
        New-Item -ItemType Directory -Path $brachesJsLocalRoot | Out-Null    
    }
    Copy-Item "$sitesBuildRoot\$branchesFileName" "$brachesJsLocalRoot\$branchesFileName" -Force

    if ($azure) {
        &$PublishToAzure `
            -ResourceGroupName $($azureConfig.ResourceGroupName) `
            -AppServicePlanName $($azureConfig.AppServicePlanName) `
            -WebAppName "tryroslyn" `
            -SourcePath "$sitesBuildRoot\$branchesFileName" `
            -TargetPath "wwwroot\$branchesFileName"
    }
}
catch {    
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}