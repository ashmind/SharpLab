param (
    [Parameter(Mandatory=$true)] [string] $siteName,
    [Parameter(Mandatory=$true)] [string] $sourcePath
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Import-Module WebAdministration

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

$appPoolName = $siteName
if ($appPoolName.Length -gt 64) {
    $parts = $appPoolName -split '\.',2
    $first = $parts[0].Substring(0, $parts[0].Length - ($appPoolName.Length - 64) - 3)
    $appPoolName = "$first(…).$($parts[1])"
}
$appPool = (Get-ChildItem 'IIS:\AppPools\' | ? { $_.Name -eq $appPoolName })
if (!$appPool) {
    Write-Output "  Creating app pool..."
    $appPool = New-WebAppPool $appPoolName
}
else {
    Write-Output "  App pool found."
}

$website = (Get-Website | ? { $_.Name -eq $siteName })
if (!$website) {
    Write-Output "  Creating web site..."
    $website = New-Website $siteName `
        -HostHeader $siteName `
        -PhysicalPath (Resolve-Path $sourcePath) `
        -ApplicationPool $($appPool.Name)
}
else {
    Write-Output "  Web site found."
}

Register-HostsRecord -HostName $siteName -IP '127.0.0.1'