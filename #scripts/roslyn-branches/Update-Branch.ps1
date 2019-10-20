param (
    [Parameter(Mandatory=$true)] [string] $branchName,
    [switch] $azure
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$BranchVersionFileName = "branch-version.json"

[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$root = Resolve-Path "$PSScriptRoot/../.."
$sourceRoot = Join-Path $root 'source'
$buildRoot = Join-Path $root "!roslyn-branches"
if (!(Test-Path $buildRoot)) { New-Item -ItemType Directory -Path $buildRoot | Out-Null }

$branchFsName = 'dotnet-' + ($branchName -replace '[/\\:_]', '-')

$webAppName = "sl-b-$($branchFsName.ToLowerInvariant())"
if ($webAppName.Length -gt 60) {
    $webAppName = $webAppName.Substring(0, 57) + "-01"; # no uniqueness check at the moment, we can add later
    Write-Host "[WARNING] Name is too long, using '$webAppName'."
}

$iisSiteName = "$webAppName.sharplab.local"
$webAppUrl = "http://$iisSiteName"
if ($azure) { $webAppUrl = "https://$($webAppName).azurewebsites.net" }

$branchRoot = Join-Path $buildRoot $branchFsName
$branchArtifactsRoot = Join-Path $branchRoot 'artifacts'

$branchSiteRoot = Join-Path $branchRoot 'site'

Write-Host "  Root:                  $root"
Write-Host "  Source Root:           $sourceRoot"
Write-Host "  Build Root:            $buildRoot"
Write-Host "  Branch FS Name:        $branchFsName"
Write-Host "  Branch Root:           $branchRoot"
Write-Host "  Branch Artifacts Root: $branchArtifactsRoot"
Write-Host "  Branch Site Root:      $branchSiteRoot"
Write-Host "  Web App Name:          $webAppName"
Write-Host "  URL:                   $webAppUrl"
Write-Host ""

function Build-Branch() {
    function Update-RoslynBuildPackages([string] $currentBuildId) {
        $ProgressPreference = 'SilentlyContinue'

        $roslynBuildsUrl = "https://dev.azure.com/dnceng/public/_apis/build/builds?api-version=5.0&definitions=15&reasonfilter=individualCI&resultFilter=succeeded&`$top=1&branchName=refs/heads/$branchName"
        Write-Host "GET $roslynBuildsUrl"
        $builds = Invoke-RestMethod $roslynBuildsUrl
        if ($builds.count -eq 0) {
            Write-Host "No successful Roslyn Azure builds found, skipping."
            return $null
        }

        $build = $builds.value[0]
        if ($build.id -eq $currentBuildId) {
            Write-Host "Roslyn Azure build $($build.id) same as current, skipping."
            return $null
        }

        if (!(Test-Path $branchArtifactsRoot)) { New-Item -ItemType Directory -Path $branchArtifactsRoot | Out-Null }
        $packagesRoot = Join-Path $branchArtifactsRoot 'roslyn-packages'
        $result = @{
            buildId = $build.id
            commitHash = $build.sourceVersion
            packagesRoot = $packagesRoot
        }

        $roslynArtifactsUrl = "$($build._links.self.href)/artifacts"

        Write-Host "GET $roslynArtifactsUrl"
        $roslynArtifacts = Invoke-RestMethod $roslynArtifactsUrl
        $roslynPackages = $roslynArtifacts.value | ? { $_.name -eq 'Packages - PreRelease' }

        if (!$roslynPackages) {
            Write-Warning "Packages not found within Roslyn Azure build artifacts, skipping."
            return $null
        }

        $downloadUrl = $roslynPackages.resource.downloadUrl

        $zipPath = Join-Path $branchArtifactsRoot "Packages.$($build.id).zip"

        if (!(Test-Path $zipPath)) { # Optimization for local only
            Write-Host "GET $($downloadUrl) => $zipPath"
            Invoke-WebRequest $downloadUrl -OutFile $zipPath
        }
        else {
            Write-Host "Found cached $zipPath, no need to download"
        }

        if (Test-Path $packagesRoot) { Remove-Item $packagesRoot -Recurse -Force }
        New-Item -ItemType Directory -Path $packagesRoot | Out-Null

        # makes it easier to flatten subdirectories
        $packagesTempRoot = Join-Path $branchArtifactsRoot 'roslyn-packages-temp'
        if (Test-Path $packagesTempRoot) { Remove-Item $packagesRoot -Recurse -Force }

        Write-Host "Unpacking $zipPath => $packagesTempRoot"
        Expand-Archive $zipPath $packagesTempRoot

        Write-Host "Flattening $packagesTempRoot => $packagesRoot"
        Get-ChildItem $packagesTempRoot -Recurse -File | Copy-Item -Destination $packagesRoot
        Remove-Item $packagesTempRoot -Recurse -Force

        return $result
    }

    function Get-SharpLabCommitHash() {
        $hash = $(git -C $sourceRoot rev-parse HEAD)
        if ($LastExitCode -ne 0) {
            throw "git failed with code $LastExitCode"
        }

        return $hash
    }

    function Build-SharpLab($roslynPackagesRoot) {
        $Runtime = 'win-x86'

        $branchSharpLabRoot = Join-Path $branchRoot 'sharplab'
        $branchSourceRoot = Join-Path $branchSharpLabRoot 'source'
        if (!(Test-Path $branchSourceRoot)) {
            New-Item -ItemType Directory -Path $branchSourceRoot | Out-Null
        }

        "Building Roslyn package map..." | Out-Default
        $roslynVersionMap = @{}
        (Get-Item (Join-Path $roslynPackagesRoot '*.nupkg')) | % {
            $_.Name -match '^([^\d]+)\.(\d.+)\.nupkg$' | Out-Null
            $roslynVersionMap[$matches[1]] = $matches[2]
        }

        "Copying $sourceRoot => $branchSourceRoot" | Out-Default
        robocopy $sourceRoot $branchSourceRoot `
            /mir `
            /xo `
            /xd "bin" /xd "obj" /xd "node_modules" /xd ".vs" `
            /np /ndl /nfl /njh | Out-Default

        $restoredPackagesRoot = (Join-Path $branchSharpLabRoot 'packages')

        "Updating Roslyn package versions in projects..." | Out-Default
        Get-ChildItem $branchSourceRoot *.csproj -Recurse | % {
            # sigh: dotnet.exe should do this, but of course it does not

            $projectName = $_.Name
            $projectPath = $_.FullName

            $projectXml = [xml][IO.File]::ReadAllText($projectPath)
            $changed = $false
            Select-Xml -Xml $projectXml -XPath '//PackageReference' | % {
                $id = $_.Node.GetAttribute('Include', '')
                $currentVersion = $_.Node.GetAttribute('Version', '')
                $roslynVersion = $roslynVersionMap[$id]

                if (!$roslynVersion -or ($roslynVersion -eq $currentVersion)) {
                    return
                }

                if (!$changed) {
                    "  $projectName" | Out-Default
                }

                $_.Node.SetAttribute('Version', $roslynVersion)
                "    $id`: $currentVersion => $roslynVersion" | Out-Default
                $changed = $true
                return
            }
            if ($changed) {
                $projectXml.Save($_.FullName)
            }
        }

        # Important because all Roslyn builds seem to use the exact same version
        "Deleting older Roslyn packages" | Out-Default
        $roslynVersionMap.Keys | % {
            $path = Join-Path $restoredPackagesRoot $_
            if (Test-Path $path) {
                Write-Host "  $path"
                Remove-Item $path -Recurse -Force
            }
        }

        "Restoring $roslynPackagesRoot => $restoredPackagesRoot" | Out-Default
        dotnet restore $branchSourceRoot `
            --runtime $Runtime `
            --packages $restoredPackagesRoot `
            --source "https://api.nuget.org/v3/index.json" `
            --source $roslynPackagesRoot `
            --verbosity minimal | Out-Default
        if ($LastExitCode -ne 0) { throw "dotnet restore exited with error code $LastExitCode" }

        "Building SharpLab" | Out-Default
        dotnet msbuild "$branchSourceRoot/Server/Server.csproj" `
            /m /nodeReuse:false `
            /t:Publish `
            /p:SelfContained=True `
            /p:AspNetCoreHostingModel=OutOfProcess `
            /p:RuntimeIdentifier=$Runtime `
            /p:Configuration=Release `
            /p:UnbreakablePolicyReportEnabled=false `
            /p:TreatWarningsAsErrors=false | Out-Default
        if ($LastExitCode -ne 0) { throw "dotnet msbuild exited with error code $LastExitCode" }

        return @{
            publishRoot = "$branchSourceRoot/Server/bin/Release/netcoreapp3.0/$runtime/publish"
        }
    }

    function Get-BranchVersionFromWebApp() {
        try {
            $currentVersion = Invoke-RestMethod "$webAppUrl/$BranchVersionFileName"
            Write-Host (ConvertTo-Json $currentVersion)
        }
        catch {
            Write-Warning "Failed to get branch version from $webAppUrl/$($BranchVersionFileName):`r`n  $($_.Exception.Message)"
            $currentVersion = @{
                roslyn = @{ buildId = $null };
                sharplab = @{ commitHash = $null }
            }
        }
        return $currentVersion
    }

    function Update-BranchVersionArtifact($roslynBuild, $sourceCommitHash) {
        $newVersion = [ordered]@{
            name = $branchName
            roslyn = @{
                buildId = $roslynBuild.buildId
                commitHash = $roslynBuild.commitHash
            }
            sharplab = @{
                commitHash = $sourceCommitHash
            }
        }
        Write-Host (ConvertTo-Json $newVersion)

        $versionArtifactPath = (Join-Path $branchArtifactsRoot $BranchVersionFileName)
        Set-Content $versionArtifactPath (ConvertTo-Json $newVersion)
        return $versionArtifactPath
    }

    function Get-BranchInfo($commitHash) {
        $commit = Invoke-RestMethod "https://api.github.com/repos/dotnet/roslyn/commits/$commitHash"
        return @{
            id = $branchFsName -replace '^dotnet-',''
            name = $branchName
            url = $webAppUrl
            repository = 'dotnet'
            commits = @(@{
                date = $commit.commit.author.date
                message = $commit.commit.message
                author = $commit.commit.author.name
                hash = $commitHash
            })
        }
    }

    Write-Host "Getting branch version from Web App..."
    $currentVersion = Get-BranchVersionFromWebApp
    Write-Host ""

    Write-Host "Comparing SharpLab version..."
    $sourceCommitHash = Get-SharpLabCommitHash
    $sourceChanged = $sourceCommitHash -ne $currentVersion.sharplab.commitHash
    Write-Host "  old hash: $($currentVersion.sharplab.commitHash)"
    Write-Host "  new hash: $sourceCommitHash"
    Write-Host "  changed:  $($sourceChanged.ToString().ToLowerInvariant())"
    Write-Host ""

    Write-Host "Downloading Roslyn Azure build..."
    # if source hash changed, we need to redownload to rebuild anyways (as GitHub actions will not cache in FS)
    $roslynBuildIdToCheck = $(if (!$sourceChanged) { $currentVersion.roslyn.buildId } else { $null })
    $roslynBuild = (Update-RoslynBuildPackages -CurrentBuildId $roslynBuildIdToCheck)
    if (!$roslynBuild) {
        # either nothing has changed, or no builds
        return @{ built = $false }
    }
    Write-Host ""

    Write-Host "Updating and building SharpLab..."
    $siteSource = (Build-SharpLab -RoslynPackagesRoot $roslynBuild.packagesRoot)
    Write-Host ""

    Write-Host "Copying to site"
    robocopy $siteSource.publishRoot $branchSiteRoot `
        /xo /mir /np /ndl /njh | Out-Default

    Write-Host "Updating branch version for Web App"
    $branchVersionPath = Update-BranchVersionArtifact `
        -RoslynBuild $roslynBuild `
        -SourceCommitHash $sourceCommitHash
    $branchSiteContentRoot = Join-Path $branchSiteRoot 'wwwroot'
    if (!(Test-Path $branchSiteContentRoot)) { New-Item $branchSiteContentRoot -Type Directory | Out-Null }
    Copy-Item $branchVersionPath $branchSiteContentRoot -Force

    Write-Host "Preparing branch info"
    $info = Get-BranchInfo -CommitHash ($roslynBuild.commitHash)

    return @{
        built = $true
        info = $info
    }
}

function Publish-Branch() {
    $ResourceGroupName = 'SharpLab'
    $AppServicePlanName = 'SharpLab-Main'

    function Test-BranchWebApp() {
        Write-Host "GET $webAppUrl/status"
        $ok = $false
        $tryPermanent = 1
        $tryTemporary = 1
        while ($tryPermanent -le 3 -and $tryTemporary -le 30) {
            try {
                Invoke-RestMethod "$webAppUrl/status"
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

        return $ok
    }

    function Get-EnvironmentVariableForAzure(
        [Parameter(Mandatory=$true)] [string] $name
    ) {
        $variable = (Get-Item env:$name -ErrorAction SilentlyContinue)
        if (!$variable) {
            throw "Environment variable $name is required for Azure deployment."
        }
        return $variable.Value
    }

    function Publish-ToAzure {
        $telemetryKey = Get-EnvironmentVariableForAzure 'SHARPLAB_TELEMETRY_KEY'

        Write-Host "Deploying to Azure, $webAppName..."
        $webApp = ((Get-AzWebApp -ResourceGroupName $ResourceGroupName) | ? { $_.Name -eq $webAppName })
        if (!$webApp) {
            Write-Host "  Creating web app $webAppName"
            $location = (Get-AzResourceGroup -Name $ResourceGroupName).Location
            $webApp = (New-AzWebApp `
                -ResourceGroupName $ResourceGroupName `
                -AppServicePlan $AppServicePlanName `
                -Location $location `
                -Name $webAppName)
            Set-AzWebApp `
                -ResourceGroupName $ResourceGroupName `
                -Name $webAppName `
                -WebSocketsEnabled $true | Out-Null
            Set-AzWebApp `
                -ResourceGroupName $ResourceGroupName `
                -Name $webAppName `
                -AppSettings @{
                    SHARPLAB_WEBAPP_NAME = $webAppName
                    SHARPLAB_TELEMETRY_KEY = $telemetryKey
                } | Out-Null
        }
        else {
            Write-Host "  Found web app $($webApp.Name)"
        }

        Write-Host "  Zipping..."
        $zipPath = Join-Path $branchArtifactsRoot "Site.zip"
        Write-Host "    => $zipPath"
        Compress-Archive -Path $branchSiteRoot/* -DestinationPath $zipPath -Force

        Write-Host "  Publishing..."
        Write-Host "    ⏱️ $([DateTime]::Now.ToString('HH:mm:ss'))"
        Publish-AzWebApp -WebApp $webApp -ArchivePath $zipPath -Force | Out-Null
        Write-Host "    ✔️ $([DateTime]::Now.ToString('HH:mm:ss'))"

        Write-Host "  Done."
    }

    if ($azure) {
        Publish-ToAzure
    }
    else {
        &"$PSScriptRoot\Publish-ToIIS.ps1" -SiteName $iisSiteName -SourcePath $branchSiteRoot
    }

    return (Test-BranchWebApp)
}

Write-Host "* Building" -ForegroundColor White

$buildResult = (Build-Branch)
if (!$buildResult.built) {
    # Up-to-date, or missing something, e.g. Roslyn build
    return @{ updated = $false }
}

Write-Host ""

Write-Host "* Publishing" -ForegroundColor White

$published = (Publish-Branch)
if (!$published) {
    # Site didn't respond correctly
    return @{ updated = $false }
}

Write-Host ""

return @{
    updated = $true
    info = $buildResult.info
}