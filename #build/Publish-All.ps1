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
    
    Set-Utf8Content $mapPath (ConvertTo-Json $map)
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
    if ($azure) {
        $x64Url = "https://sl-a-x64.azurewebsites.net"
    }
    
    return @([ordered]@{
        id = 'x64'
        name = 'x64'
        url = $x64Url
    })
}

function Set-Utf8Content($file, $content) {
    if ($HOST.Version.Major -ge 6) {
        # PowerShell Core 6.0+ natively support UTF-8 output but no more -Encoding Byte support
        return Set-Content $file $content
    } else {
        return Set-Content $file -Encoding Byte ([Text.Encoding]::UTF8.GetBytes($content))
    }
}

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "Deploy SharpLab" # prevents title > 1024 char errors
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    Write-Output "Environment:"
    Write-Output "  Current Path:          $(Get-Location)"    
    Write-Output "  Script Root:           $PSScriptRoot"

    $sourceRoot = Resolve-Path "$PSScriptRoot\..\source"
    Write-Output "  Source Root:           $sourceRoot"

    $roslynArtifactsRoot = Resolve-Path "$PSScriptRoot\..\!roslyn\artifacts"
    Write-Output "  Roslyn Artifacts Root: $roslynArtifactsRoot"

    $sitesRoot = Resolve-Path "$PSScriptRoot\..\!sites"
    Write-Output "  Sites Root:            $sitesRoot"

    &"$PSScriptRoot\#tools\nuget" install ftpush -Pre -OutputDirectory "$sourceRoot\!tools"
    $ftpushExe = @(Get-Item "$sourceRoot\!tools\ftpush*\tools\ftpush.exe")[0].FullName
    Write-Output "  ftpush.exe:            $ftpushExe"

    Write-Output "Getting Roslyn feature map..."
    $roslynBranchFeatureMap = Get-RoslynBranchFeatureMap -ArtifactsRoot $roslynArtifactsRoot

    if ($azure) {
        $azureConfigPath = ".\!Azure.config.json"
        if (!(Test-Path $azureConfigPath)) {
            throw "Path '$azureConfigPath' was not found."
        }
        $azureConfig = ConvertFrom-Json (Get-Content $azureConfigPath -Raw)
        Login-ToAzure $azureConfig
    }

    $branchesJson = @(Get-PredefinedBranches)
    Get-ChildItem $sitesRoot | ? { $_ -is [IO.DirectoryInfo] } | % {
        $branchFsName = $_.Name

        $siteRoot = $_.FullName
        if (!(Get-ChildItem $siteRoot\bin -Recurse | ? { $_ -is [IO.FileInfo] })) {
            return
        }

        Write-Output ''
        Write-Output "*** $_"

        $siteRoslynArtifactsRoot = Resolve-Path "$roslynArtifactsRoot\$($_.Name)"
        $branchInfo = ConvertFrom-Json ([IO.File]::ReadAllText((Resolve-Path "$siteRoslynArtifactsRoot\BranchInfo.json")))

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
            group = $branchInfo.repository
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
    Set-Utf8Content "$sitesRoot\$branchesFileName" $(ConvertTo-Json $branchesJson -Depth 100)

    $brachesJsLocalRoot = "$sourceRoot\WebApp\wwwroot"
    if (!(Test-Path $brachesJsLocalRoot)) {
        New-Item -ItemType Directory -Path $brachesJsLocalRoot | Out-Null    
    }
    Copy-Item "$sitesRoot\$branchesFileName" "$brachesJsLocalRoot\$branchesFileName" -Force

    if ($azure) {
        &$PublishToAzure `
            -FtpushExe $ftpushExe `
            -ResourceGroupName $($azureConfig.ResourceGroupName) `
            -AppServicePlanName $($azureConfig.AppServicePlanName) `
            -WebAppName "sharplab" `
            -SourcePath "$sitesRoot\$branchesFileName" `
            -TargetPath "wwwroot/$branchesFileName"
    }
}
catch {
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}