param (
    [switch] [boolean] $azure
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Write-Host, Write-Error and Write-Warning didn't function properly in Azure, so this mostly used Write-Output
# However new code can use other ones

$LangugageFeatureMapUrl = 'https://raw.githubusercontent.com/dotnet/roslyn/master/docs/Language%20Feature%20Status.md'

$PublishToIIS = Resolve-Path "$PSScriptRoot\Publish-ToIIS.ps1"
$PublishToAzure = Resolve-Path "$PSScriptRoot\Publish-ToAzure.ps1"

function ConvertTo-Hashtable([PSCustomObject] $object) {
    $result = @{}
    $object.PSObject.Properties | % { $result[$_.Name] = $_.Value }
    return $result
}

function Get-RoslynBranchFeatureMap($artifactsRoot) {
    $markdown = (Invoke-WebRequest $LangugageFeatureMapUrl -UseBasicParsing)
    $languageVersions = [regex]::Matches($markdown, '#\s*(?<language>.+)\s*$\s*(?<table>(?:^\|.+$\s*)+)', 'Multiline')

    $mapPath = "$artifactsRoot/RoslynFeatureMap.json"
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

function Get-PredefinedBranches() {
    $x64Url = "http://sl-a-x64.sharplab.local"
    $coreX64Url = "http://sl-a-core-x64.sharplab.local"
    $coreX64ProfiledUrl = "http://localhost:54100"
    if ($azure) {
        $x64Url = "https://sl-a-x64.azurewebsites.net"
        $coreX64Url = "https://sl-a-core-x64.azurewebsites.net"
        $coreX64ProfiledUrl = "https://sl-a-core-x64-profiled.azurewebsites.net"
    }
    
    return @([ordered]@{
        id = 'x64'
        name = 'x64'
        url = $x64Url
        group = 'Platforms'
    }, [ordered]@{
        id = 'core-x64'
        name = '.NET Core (x64)'
        url = $coreX64Url
        group = 'Platforms'
    }, [ordered]@{
        id = 'core-x64-profiled'
        name = '.NET Core (x64, Profiler)'
        url = $coreX64Url
        group = 'Platforms'
    })
}

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "Deploy SharpLab" # prevents title > 1024 char errors
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    Write-Output "Environment:"
    Write-Output "  Current Path:          $(Get-Location)"    
    Write-Output "  Script Root:           $PSScriptRoot"

    $sourceRoot = Resolve-Path "$PSScriptRoot\..\..\source"
    Write-Output "  Source Root:           $sourceRoot"    
    
    $toolsRoot = Join-Path $PSScriptRoot '!tools'
    if (!(Test-Path $toolsRoot)) {
        New-Item -ItemType Directory -Path $toolsRoot | Out-Null
    }
    Write-Output "  Tools Root:            $toolsRoot"

    $roslynBranchesRoot = Resolve-Path "$PSScriptRoot\..\..\!roslyn-branches"
    Write-Output "  Roslyn Branches Root:  $roslynBranchesRoot"
    
    $nugetExe = "$toolsRoot\nuget.exe"
    if (!(Test-Path $nugetExe)) {
        Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetExe
    }
    &$nugetExe install ftpush -Pre -OutputDirectory $toolsRoot
    $ftpushExe = @(Get-Item "$toolsRoot\ftpush*\tools\ftpush.exe")[0].FullName
    Write-Output "  ftpush.exe:            $ftpushExe"

    Write-Output "Getting Roslyn feature map..."
    $roslynBranchFeatureMap = Get-RoslynBranchFeatureMap -ArtifactsRoot $roslynBranchesRoot

    if ($azure) {
        $azureConfigPath = ".\!roslyn-branches-azure.json"
        if (!(Test-Path $azureConfigPath)) {
            throw "Path '$azureConfigPath' was not found."
        }
        $azureConfig = ConvertFrom-Json (Get-Content $azureConfigPath -Raw)
        Login-ToAzure $azureConfig
    }

    $branchesJson = @(Get-PredefinedBranches)
    Get-ChildItem $roslynBranchesRoot | ? { $_ -is [IO.DirectoryInfo] } | % {
        $branchFsName = $_.Name

        $siteRoot = Join-Path $_.FullName 'site'
        if (!(Test-PAth $siteRoot) -or !(Get-ChildItem $siteRoot\bin -Recurse | ? { $_ -is [IO.FileInfo] })) {
            return
        }

        Write-Output ''
        Write-Output "*** $_"

        $branchArtifactsRoot = Join-Path $_.FullName 'artifacts'
        $branchInfo = ConvertFrom-Json ([IO.File]::ReadAllText((Join-Path $branchArtifactsRoot 'roslyn-branch-info.json')))

        $webAppName = "sl-b-$($branchFsName.ToLowerInvariant())"
        if ($webAppName.Length -gt 60) {
             $webAppName = $webAppName.Substring(0, 57) + "-01"; # no uniqueness check at the moment, we can add later
             Write-Output "[WARNING] Name is too long, using '$webAppName'."
        }

        $iisSiteName = "$webAppName.sharplab.local"
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

        Write-Host "GET $url/status"
        $ok = $false
        $tryPermanent = 1
        $tryTemporary = 1
        while ($tryPermanent -le 3 -and $tryTemporary -le 30) {
            try {
                Invoke-RestMethod "$url/status"
                $ok = $true
                break
            }
            catch {
                $ex = $_.Exception
                $temporary = ($ex -is [Net.WebException] -and $ex.Response -and $ex.Response.StatusCode -eq 503)
                if ($temporary) {
                    $tryTemporary += 1
                }
                else {
                    $tryPermanent += 1
                }
                Write-Warning ($ex.Message)
            }
            Start-Sleep -Seconds 1
        }
        if (!$ok) {
            return
        }

        # Success!
        $branchJson = [ordered]@{
            id = $branchFsName -replace '^dotnet-',''
            name = $branchInfo.name
            group = 'Roslyn branches'
            url = $url
            feature = $roslynBranchFeatureMap[$branchInfo.name]
            commits = $branchInfo.commits
        }
        if (!$branchJson.feature) {
            $branchJson.Remove('feature')
        }

        $branchesJson += $branchJson
    }

    $branchesFileName = "!branches.json"
    Write-Output "Updating $branchesFileName..."
    Set-Content "$roslynBranchesRoot\$branchesFileName" $(ConvertTo-Json $branchesJson -Depth 100) -Encoding UTF8

    $brachesJsLocalRoot = "$sourceRoot\WebApp\wwwroot"
    if (!(Test-Path $brachesJsLocalRoot)) {
        New-Item -ItemType Directory -Path $brachesJsLocalRoot | Out-Null    
    }
    Copy-Item "$roslynBranchesRoot\$branchesFileName" "$brachesJsLocalRoot\$branchesFileName" -Force

    if ($azure) {
        &$PublishToAzure `
            -FtpushExe $ftpushExe `
            -ResourceGroupName $($azureConfig.ResourceGroupName) `
            -AppServicePlanName $($azureConfig.AppServicePlanName) `
            -WebAppName "sharplab" `
            -SourcePath "$roslynBranchesRoot\$branchesFileName" `
            -TargetPath "wwwroot/$branchesFileName"
    }
}
catch {
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}