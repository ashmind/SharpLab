param (
    [Parameter(Mandatory=$true)] [string] $ftpushExe,
    [Parameter(Mandatory=$true)] [string] $resourceGroupName,
    [Parameter(Mandatory=$true)] [string] $appServicePlanName,
    [Parameter(Mandatory=$true)] [string] $webAppName,
    [Parameter(Mandatory=$true)] [string] $sourcePath,
    [Parameter(Mandatory=$true)] [string] $targetPath,
    [switch] $canCreateWebApp = $false,
    [switch] $canStopWebApp = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
Write-Output "Deploying to Azure, $webAppName..."

# See https://msdn.microsoft.com/en-us/library/mt125356.aspx for AzureRM installation
Import-Module AzureRM.Resources
Import-Module AzureRM.WebSites

$webApp = ((Get-AzureRmWebApp -ResourceGroupName $resourceGroupName) | ? { $_.Name -eq $webAppName })
if (!$webApp) {
    if (!$canCreateWebApp) {
        throw "Web app $webAppName was not found, and CanCreateWebApp was not set."
    }

    Write-Output "  Creating web app $webAppName"
    $location = (Get-AzureRmResourceGroup -Name $resourceGroupName).Location
    $webApp = (New-AzureRmWebApp `
        -ResourceGroupName $resourceGroupName `
        -AppServicePlan $appServicePlanName `
        -Location $location `
        -Name $webAppName)
    Set-AzureRmWebApp `
        -ResourceGroupName $resourceGroupName `
        -AppServicePlan $appServicePlanName `
        -Location $location `
        -Name $webAppName `
        -WebSocketsEnabled $true
}
else {
    Write-Output "  Found web app $($webApp.Name)"
}

if ($canStopWebApp) {
    Write-Output "  Stopping $($webApp.Name)..."
    Stop-AzureRmWebApp -Webapp $webApp | Out-Null
}

$publishProfileXml = [xml](Get-AzureRMWebAppPublishingProfile -WebApp $($webApp) -OutputFile "$PSScriptRoot\!_profile.xml")
Remove-Item "$PSScriptRoot\!_profile.xml"

$ftpProfileXml = (Select-Xml -Xml $publishProfileXml -XPath "//publishProfile[@publishMethod='FTP']").Node
$ftpUrl = [uri]($ftpProfileXml.publishUrl -replace '^ftp://','ftps://')
$ftpUserName = $ftpProfileXml.userName
$ftpPassword = $ftpProfileXml.userPWD

Write-Output "  FTP"
Write-Output "    URL:    $ftpUrl"
Write-Output "    Server: $($ftpUrl.Authority)"
Write-Output "    Path:   $($ftpUrl.LocalPath)"
Write-Output "    User:   $ftpUserName"
Write-Output "  Transfer:"
try {
    $env:TR_FTP_PASSWORD = $ftpPassword
    &$ftpushExe --source $sourcePath --target "$ftpUrl/$targetPath" --username $ftpUserName --passvar TR_FTP_PASSWORD --parallel 10
    if ($LastExitCode -ne 0) {
        throw "ftpush.exe failed with exit code $LastExitCode"
    }
}
finally {
    Remove-Item "env:TR_FTP_PASSWORD"
}

if ($canStopWebApp) {
    Write-Output "  Starting $($webApp.Name)..."
    Start-AzureRmWebApp -Webapp $webApp | Out-Null
}