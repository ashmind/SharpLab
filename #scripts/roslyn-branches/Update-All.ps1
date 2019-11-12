param (
    [switch] $azure
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$LanguageFeatureMapUrl = 'https://raw.githubusercontent.com/dotnet/roslyn/master/docs/Language%20Feature%20Status.md'
$BranchesJsonFileName = 'branches.json'

$root = Resolve-Path "$PSScriptRoot/../.."
$sourceRoot = Join-Path $root 'source'
$buildRoot = Join-Path $root '!roslyn-branches'
if (!(Test-Path $buildRoot)) { New-Item -ItemType Directory -Path $buildRoot | Out-Null }

Write-Host "Environment:"
Write-Host "  Current Path: $(Get-Location)"
Write-Host "  Script Root:  $PSScriptRoot"
Write-Host "  Root:         $root"
Write-Host "  Source Root:  $sourceRoot"
Write-Host "  Build Root:   $buildRoot"

$branchesJsonPath = (Join-Path $buildRoot $BranchesJsonFileName)

function Get-EnvironmentVariableForAzure(
    [Parameter(Mandatory=$true)] [string] $name
) {
    $variable = (Get-Item env:$name -ErrorAction SilentlyContinue)
    if (!$variable) {
        throw "Environment variable $name is required for Azure deployment."
    }
    return $variable.Value
}

function Connect-ToAzure() {
    $tenant = Get-EnvironmentVariableForAzure 'SL_BUILD_AZURE_TENANT'
    $appid = Get-EnvironmentVariableForAzure 'SL_BUILD_AZURE_APP_ID' # service principal application id
    $secret = Get-EnvironmentVariableForAzure 'SL_BUILD_AZURE_SECRET' # service principal secret

    $credential = New-Object System.Management.Automation.PSCredential(
        $appid,
        (ConvertTo-SecureString $secret -AsPlainText -Force)
    )

    "Connecting to Azure..." | Out-Default
    Connect-AzAccount `
        -Tenant $tenant `
        -Credential $credential `
        -ServicePrincipal `
        -Scope Process | Out-Null
}

function ConvertTo-Hashtable([PSCustomObject] $object) {
    $result = @{}
    $object.PSObject.Properties | % { $result[$_.Name] = $_.Value }
    return $result
}

function Get-RoslynBranchFeatureMap() {
    $markdown = (Invoke-WebRequest $LanguageFeatureMapUrl -UseBasicParsing)
    $languageVersions = [regex]::Matches($markdown, '#\s*(?<language>.+)\s*$\s*(?<table>(?:^\|.+$\s*)+)', 'Multiline')

    $mapPath = "$buildRoot/RoslynFeatureMap.json"
    $map = @{}
    if (Test-Path $mapPath) {
        $map = ConvertTo-Hashtable (ConvertFrom-Json (Get-Content $mapPath -Raw))
    }
    $languageVersions | % {
        $language = $_.Groups['language'].Value
        $table = $_.Groups['table'].Value
        [regex]::Matches($table, '^\|(?<rawname>[^|]+)\|.+roslyn/tree/(?<branch>[A-Za-z\d\-/]+)', 'Multiline') | % {
            $name = $_.Groups['rawname'].Value.Trim()
            $branch = $_.Groups['branch'].Value
            $url = ''
            if ($name -match '\[([^\]]+)\]\(([^)]+)\)') {
                $name = $matches[1]
                $url = $matches[2]
            }

            $map[$branch] = [PSCustomObject]@{
                language = $language
                name = $name
                url = $url
            }
        }
    } | Out-Null

    Set-Content $mapPath (ConvertTo-Json $map) -Encoding UTF8
    return $map
}

function Get-PredefinedBranches() {
    $x64Url = "http://localhost:54100"
    $x64ProfiledUrl = "http://localhost:54200"
    $netfxUrl = "http://sl-a-netfx.sharplab.local"
    $netfxX64Url = "http://sl-a-x64.sharplab.local"
    if ($azure) {
        $x64Url = "https://sl-a-core-x64.azurewebsites.net"
        $x64ProfiledUrl = "https://sl-a-core-x64-profiled.azurewebsites.net"
        $netfxUrl = "http://sl-a-netfx.sharplab.local"
        $netfxX64Url = "https://sl-a-x64.azurewebsites.net"
    }

    return @([ordered]@{
        id = 'core-x64'
        name = 'x64'
        url = $x64Url
        group = 'Platforms'
        kind = 'platform'
    }, [ordered]@{
        id = 'core-x64-profiled'
        name = 'x64 (Profiler)'
        url = $x64ProfiledUrl
        group = 'Platforms'
        kind = 'platform'
    }, [ordered]@{
        id = 'netfx'
        name = '.NET Framework (x86)'
        url = $netfxUrl
        group = 'Platforms'
        kind = 'platform'
    }, [ordered]@{
        id = 'x64'
        name = '.NET Framework (x64)'
        url = $netfxX64Url
        group = 'Platforms'
        kind = 'platform'
    })
}

function Get-BranchesJson() {
    if (!$azure) {
        return @{ data = @(Get-PredefinedBranches) }
    }

    Write-Host " (from Azure)"
    $storageContext = New-AzStorageContext -StorageAccountName "slbs"
    $blob = (Get-AzStorageBlob -Container 'public' -Blob 'branches.json' -Context $storageContext).ICloudBlob
    Get-AzStorageBlobContent -CloudBlob $blob -Context $storageContext -Destination $branchesJsonPath -Force | Out-Null

    return @{
        azure = @{
            blob = $blob
            context = $storageContext
        }
        data = ConvertFrom-Json ([IO.File]::ReadAllText($branchesJsonPath))
    }
}

function Update-BranchesJson($branchesJson, $branch) {
    $branchJson = [ordered]@{
        id = $branch.id
        name = $branch.name
        group = 'Roslyn branches'
        kind = 'roslyn'
        url = $branch.url
        feature = $roslynBranchFeatureMap[$branch.name]
        commits = $branch.commits
    }
    if (!$branchJson.feature) {
        $branchJson.Remove('feature')
    }

    $branchesJson.data = @($branchesJson.data | ? { $_.id -ne $branch.id }) + $branchJson
    Set-Content $branchesJsonPath $(ConvertTo-Json $branchesJson.data -Depth 100) -Encoding UTF8

    if ($azure) {
        Write-Host "Uploading $branchesJsonFileName to Azure..."
        Set-AzStorageBlobContent `
            -CloudBlob $branchesJson.azure.blob `
            -File $branchesJsonPath `
            -Context $branchesJson.azure.context `
            -Properties @{
                ContentType = 'application/json'
                CacheControl = 'max-age=43200' # 12 hours
            } `
            -Force | Out-Null
    }
    else {
        $localRoot = "$sourceRoot\WebApp\wwwroot"
        if (!(Test-Path $localRoot)) {
            New-Item -ItemType Directory -Path $localRoot | Out-Null
        }
        Copy-Item $branchesJsonPath "$localRoot\!$BranchesJsonFileName" -Force
    }
}

$config = ConvertFrom-Json (Get-Content "$root\.roslyn-branches.json" -Raw)
Write-Host ""

if ($azure) {
    Connect-ToAzure
}

Write-Host "Getting Roslyn feature map..."
$roslynBranchFeatureMap = Get-RoslynBranchFeatureMap

Write-Host "Getting branches.json..."
$branchesJson = Get-BranchesJson

Write-Host "Getting branches..." -ForegroundColor White
Write-Host "  git ls-remote --heads https://github.com/dotnet/roslyn.git"
$branches = (git ls-remote --heads https://github.com/dotnet/roslyn.git)
if ($LastExitCode -ne 0) { throw "Command 'git ls-remote' failed with exit code $LastExitCode." }

$branches = $branches |
  % { $_ -replace '^.*refs/heads/(\S+).*$','$1' } |
  ? { $_ -match $($config.include) }
Write-Host ""

Write-Host "Updating branches..." -ForegroundColor White
$branches | % {
    Write-Host "*** $_" -ForegroundColor White
    try {
        $result = (&"$PSScriptRoot\Update-Branch.ps1" $_ -Azure:$azure)
        if ($result.updated) {
            Write-Host "* Updating branches.json" -ForegroundColor White
            Update-BranchesJson $branchesJson -Branch $result.info
        }
    }
    catch {
        $ex = $_.Exception
        Write-Warning "$($ex.Message)"
    }
    Write-Host ""
}