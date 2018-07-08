param (
    [Parameter(Mandatory=$true)] [string] $siteName,
    [Parameter(Mandatory=$true)] [string] $sourcePath
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Import-Module IISAdministration

function Register-HostsRecord([string] $hostName, [string] $ip) {
    $hostsPath = Join-Path ([System.Environment]::GetFolderPath('System')) "drivers\etc\hosts"
    
    $record = "$ip $hostName"
    $content = [IO.File]::ReadAllText($hostsPath)
    if ($content.Contains($record)) {
        Write-Output "  Site already in etc/hosts."
        return
    }
    
    if (!$content.EndsWith("`r`n")) {
        $content += "`r`n"
    }
    
    $content += $record
    [IO.File]::WriteAllText($hostsPath, $content)
    Write-Output "  Added site to etc/hosts."
}

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
Write-Output "Deploying to IIS, $siteName..."

Start-IISCommitDelay
$server = Get-IISServerManager

$appPoolName = $siteName
if ($appPoolName.Length -gt 64) {
    $parts = $appPoolName -split '\.',2
    $first = $parts[0].Substring(0, $parts[0].Length - ($appPoolName.Length - 64) - 3)
    $appPoolName = "$first(…).$($parts[1])"
}

if (!$server.ApplicationPools[$appPoolName]) {
    Write-Output "  Creating app pool..."
    $server.ApplicationPools.Add($appPoolName) | Out-Null
}
else {
    Write-Output "  App pool found."
}

if (!$server.Sites[$siteName]) {
    Write-Output "  Creating web site..."
    New-IISSite `
        -Name $siteName `
        -BindingInformation "*:80:$siteName" `
        -PhysicalPath (Resolve-Path $sourcePath)
    $server.Sites[$siteName].Applications["/"].ApplicationPoolName = $appPoolName
    #Write-Host "WebSite: $website"
}
else {
    Write-Output "  Web site found."
}
Stop-IISCommitDelay

Register-HostsRecord -HostName $siteName -IP '127.0.0.1'