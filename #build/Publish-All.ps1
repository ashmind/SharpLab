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
    Write-Output "  Current Path:          $(Get-Location)"    
    Write-Output "  Script Root:           $PSScriptRoot"

    $sourceRoot = Resolve-Path "$PSScriptRoot\..\source"
    Write-Output "  Source Root:           $sourceRoot"

    $roslynArtifactsRoot = Resolve-Path "$PSScriptRoot\..\!roslyn\artifacts"
    Write-Output "  Roslyn Artifacts Root: $roslynArtifactsRoot"

    $sitesRoot = Resolve-Path "$PSScriptRoot\..\!sites"
    Write-Output "  Sites Root:            $sitesRoot"

    $ftpushExe = @(Get-Item "$sourceRoot\#packages\ftpush*\tools\ftpush.exe")
    if ($ftpushExe.Count -gt 1) {
        throw "Found multiple ftpush.exe: $ftpushExe"
    }
    $ftpushExe = $ftpushExe[0]
    Write-Output "  ftpush.exe:            $ftpushExe"

    if ($azure) {
        $azureConfigPath = ".\!Azure.config.json"
        if (!(Test-Path $azureConfigPath)) {
            throw "Path '$azureConfigPath' was not found."
        }
        $azureConfig = ConvertFrom-Json (Get-Content $azureConfigPath -Raw)
        Login-ToAzure $azureConfig
    }  

    $branchesJson = @()
    Get-ChildItem $sitesRoot | ? { $_ -is [IO.DirectoryInfo] } | % {
        $branchFsName = $_.Name

        $siteRoot = $_.FullName
        if (!(Get-ChildItem $siteRoot\bin -Recurse | ? { $_ -is [IO.FileInfo] })) {
            return
        }

        Write-Output ''
        Write-Output "*** $_"

        $siteRoslynArtifactsRoot = Resolve-Path "$roslynArtifactsRoot\$($_.Name)"
        $branchInfo = ConvertFrom-Json ([IO.File]::ReadAllText("$siteRoslynArtifactsRoot\BranchInfo.json"))

        $webAppName = "tr-b-$($branchFsName.ToLowerInvariant())"
        if ($webAppName.Length -gt 60) {
             $webAppName = $webAppName.Substring(0, 57) + "-01"; # no uniqueness check at the moment, we can add later
             Write-Output "[WARNING] Name is too long, using '$webAppName'."
        }

        $iisSiteName = "$webAppName.tryroslyn.local"
        $url = "http://$iisSiteName"
        &$PublishToIIS -SiteName $iisSiteName -SourcePath $siteRoot

        if ($azure) {
            &$PublishToAzure `
                -FtpushExe $ftpushExe `
                -ResourceGroupName $($azureConfig.ResourceGroupName) `
                -AppServicePlanName $($azureConfig.AppServicePlanName) `
                -WebAppName $webAppName `
                -CanCreateWebApp `
                -CanStopWebApp `
                -SourcePath $siteRoot `
                -TargetPath "."
            $url = "https://$($webAppName).azurewebsites.net"
        }

        Start-Sleep -Seconds 3 # sometimes Azure sites do not start straight away
        Write-Host "GET $url/status"
        try {          
            Invoke-RestMethod "$url/status"
        }
        catch {
            Write-Output "  [WARNING] $($_.Exception.Message)"
            return
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
    Set-Content "$sitesRoot\$branchesFileName" $(ConvertTo-Json $branchesJson -Depth 100)

    $brachesJsLocalRoot = "$sourceRoot\Web\wwwroot"
    if (!(Test-Path $brachesJsLocalRoot)) {
        New-Item -ItemType Directory -Path $brachesJsLocalRoot | Out-Null    
    }
    Copy-Item "$sitesRoot\$branchesFileName" "$brachesJsLocalRoot\$branchesFileName" -Force

    if ($azure) {
        &$PublishToAzure `
            -FtpushExe $ftpushExe `
            -ResourceGroupName $($azureConfig.ResourceGroupName) `
            -AppServicePlanName $($azureConfig.AppServicePlanName) `
            -WebAppName "tryroslyn" `
            -SourcePath "$sitesRoot\$branchesFileName" `
            -TargetPath "wwwroot/$branchesFileName"
    }
}
catch {
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}