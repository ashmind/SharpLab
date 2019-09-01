param (
    [Parameter(Mandatory=$true)] [string] $webAppName,
    [Parameter(Mandatory=$true)] [string] $sourcePath,
    [Parameter(Mandatory=$true)] [string] $targetPath
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$resourceGroupName = $env:SL_BUILD_AZURE_GROUP
if (!$resourceGroupName) { throw 'Environment variable SL_BUILD_AZURE_GROUP is required for Azure deployment.' }

Write-Output "Downloading ftpush..."
$nugetExe = "$toolsRoot\nuget.exe"
if (!(Test-Path $nugetExe)) {
    Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetExe
}
&$nugetExe install ftpush -Pre -OutputDirectory $toolsRoot
$ftpushExe = @(Get-Item "$toolsRoot\ftpush*\tools\ftpush.exe")[0].FullName
Write-Output "  $ftpushExe"

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
Write-Output "Deploying to Azure, $webAppName..."

$webApp = Get-AzWebApp -ResourceGroupName $resourceGroupName -Name $webAppName

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