param (
    [Parameter(Mandatory=$true)] [string] $ftpushExe,
    [Parameter(Mandatory=$true)] [string] $webAppName,
    [Parameter(Mandatory=$true)] [string] $sourcePath,
    [Parameter(Mandatory=$true)] [string] $targetPath,
    [switch] $canCreateWebApp = $false,
    [switch] $canStopWebApp = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$resourceGroupName = $env:SL_BUILD_AZURE_GROUP
if (!$resourceGroupName) { throw 'Environment variable SL_BUILD_AZURE_GROUP is required for Azure deployment.' }

$appServicePlanName = $env:SL_BUILD_AZURE_PLAN
if (!$appServicePlanName) { throw 'Environment variable SL_BUILD_AZURE_PLAN is required for Azure deployment.' }

$telemetryKey = $env:SHARPLAB_TELEMETRY_KEY
if (!$telemetryKey) { throw 'Environment variable SHARPLAB_TELEMETRY_KEY is required for Azure deployment.' }

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
Write-Output "Deploying to Azure, $webAppName..."

$webApp = ((Get-AzWebApp -ResourceGroupName $resourceGroupName) | ? { $_.Name -eq $webAppName })
if (!$webApp) {
    if (!$canCreateWebApp) {
        throw "Web app $webAppName was not found, and CanCreateWebApp was not set."
    }

    Write-Output "  Creating web app $webAppName"
    $location = (Get-AzResourceGroup -Name $resourceGroupName).Location
    $webApp = (New-AzWebApp `
        -ResourceGroupName $resourceGroupName `
        -AppServicePlan $appServicePlanName `
        -Location $location `
        -Name $webAppName)
    Set-AzWebApp `
        -ResourceGroupName $resourceGroupName `
        -Name $webAppName `
        -WebSocketsEnabled $true | Out-Null
    Set-AzWebApp `
        -ResourceGroupName $resourceGroupName `
        -Name $webAppName `
        -AppSettings @{
            SHARPLAB_WEBAPP_NAME = $webAppName
            SHARPLAB_TELEMETRY_KEY = $env:SHARPLAB_TELEMETRY_KEY
        } | Out-Null
}
else {
    Write-Output "  Found web app $($webApp.Name)"
}

if ($canStopWebApp) {
    Write-Output "  Stopping $($webApp.Name)..."
    Stop-AzWebApp -Webapp $webApp | Out-Null
}

$publishProfileXml = [xml](Get-AzWebAppPublishingProfile -WebApp $($webApp) -OutputFile "$PSScriptRoot\!_profile.xml")
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
    $env:SL_BUILD_FTP_PASSWORD = $ftpPassword
    &$ftpushExe --source $sourcePath --target "$ftpUrl/$targetPath" --username $ftpUserName --passvar SL_BUILD_FTP_PASSWORD --parallel 10
    if ($LastExitCode -ne 0) {
        throw "ftpush.exe failed with exit code $LastExitCode"
    }
}
finally {
    Remove-Item "env:SL_BUILD_FTP_PASSWORD"
}

if ($canStopWebApp) {
    Write-Output "  Starting $($webApp.Name)..."
    Start-AzWebApp -Webapp $webApp | Out-Null
    for ($try = 1; $try -lt 10; $try++) {
        $app = Get-AzWebApp -ResourceGroupName $resourceGroupName -Name $webAppName
        if ($app.State -eq 'Running') {
            break;
        }
        Start-Sleep -Seconds 1
    }
}