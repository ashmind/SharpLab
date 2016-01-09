param (
    [Parameter(Mandatory=$true)] [string] $resourceGroupName,
    [Parameter(Mandatory=$true)] [string] $webAppName,
    [Parameter(Mandatory=$true)] [string] $sourcePath,    
    [Parameter(Mandatory=$true)] [string] $targetPath,
    [switch] $canCreateWebApp = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
Write-Output "Deploying to Azure, $webAppName..."

try {
    winscp.com /help | Out-Null
}
catch {
    throw New-Object Exception("This script requires WinSCP to be in PATH. WinSCP test failed: $_", $_.Exception)
}

# See https://msdn.microsoft.com/en-us/library/mt125356.aspx for AzureRM installation
Import-Module AzureRM.Resources
Import-Module AzureRM.WebSites

$webApp = ((Get-AzureRmWebApp -ResourceGroupName $resourceGroupName) | ? { $_.Name -eq $webAppName })
if (!$webApp) {
    if (!$canCreateWebApp) {
        throw "Web app $webAppName was not found, and CanCreateWebApp was not set."
    }

    Write-Output "  Creating web app $webAppName"
    $location = (Get-AzureRMResourceGroup -Name $resourceGroupName).Location
    $webApp = (New-AzureRmWebApp `
        -ResourceGroupName $resourceGroupName `
        -Location $location `
        -Name $webAppName)
}
else {
    Write-Output "  Found web app $($webApp.Name)"
}
$publishProfileXml = [xml](Get-AzureRMWebAppPublishingProfile -WebApp $($webApp) -OutputFile "$PSScriptRoot\!_profile.xml")
Remove-Item "$PSScriptRoot\!_profile.xml"

$ftpProfileXml = (Select-Xml -Xml $publishProfileXml -XPath "//publishProfile[@publishMethod='FTP']").Node
$ftpUrl = [uri]$ftpProfileXml.publishUrl
$ftpUserName = $ftpProfileXml.userName
$ftpPassword = $ftpProfileXml.userPWD

Write-Output "  FTP"
Write-Output "    URL:    $ftpUrl"
Write-Output "    Server: $($ftpUrl.Authority)"
Write-Output "    Path:   $($ftpUrl.LocalPath)"
Write-Output "    User:   $ftpUserName"

$script = @"
open ftp://$($ftpUserName):$($ftpPassword)@$($ftpUrl.Authority)
binary
cd $($ftpUrl.LocalPath)
$(if ((Get-Item $sourcePath) -is [IO.DirectoryInfo]) {
    "synchronize remote `"$sourcePath`" `"$targetPath`" -delete"
} else {
    "put `"$sourcePath`" `"$targetPath`" -neweronly"
})
exit
"@

$script = $script.Trim()
Set-Content '!_scpscript.txt' $script

Write-Output "  Transfer:"
winscp.com /script="$(Resolve-Path "!_scpscript.txt")"