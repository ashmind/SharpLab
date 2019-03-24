Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

function Download-RoslynBuildPackages($branchName, $artifactsRoot) {
    $roslynBuildsUrl = "https://dev.azure.com/dnceng/public/_apis/build/builds?api-version=5.0&definitions=15&reasonfilter=individualCI&resultFilter=succeeded&`$top=1&branchName=refs/heads/$branchName"
    "GET $roslynBuildsUrl" | Out-Default
    $builds = Invoke-RestMethod $roslynBuildsUrl
    if ($builds.count -eq 0) {
        "No successful Roslyn Azure builds found, skipping." | Out-Default
        return $null
    }
    
    $build = $builds.value[0]

    $packagesRoot = Join-Path $artifactsRoot 'roslyn-packages'
    $result = @{
        commitHash = $build.sourceVersion
        packagesRoot = $packagesRoot
        changed = $false
    }
    
    $lastBuildIdPath = Join-Path $artifactsRoot 'roslyn-last-build-id'
    if (Test-Path $lastBuildIdPath) {
        $id = [IO.File]::ReadAllText($lastBuildIdPath)
        if ($id -eq $build.id) {
            "Roslyn Azure build $($build.id) already downloaded." | Out-Default
            return $result
        }
    }

    $roslynArtifactsUrl = "$($build._links.self.href)/artifacts"
    
    "GET $roslynArtifactsUrl" | Out-Default
    $roslynArtifacts = Invoke-RestMethod $roslynArtifactsUrl
    $roslynPackages = $roslynArtifacts.value | ? { $_.name -eq 'Packages - PreRelease' }
    
    if (!$roslynPackages) {
        Write-Warning "Packages not found within Roslyn Azure build artifacts, skipping."
        return $null
    }
    
    $downloadUrl = $roslynPackages.resource.downloadUrl

    if (!(Test-Path $artifactsRoot)) { New-Item -ItemType Directory -Path $artifactsRoot | Out-Null }
    $zipPath = Join-Path $artifactsRoot "Packages.zip"

    "GET $($downloadUrl) => $zipPath" | Out-Default
    Invoke-WebRequest $downloadUrl -OutFile $zipPath

    if (Test-Path $packagesRoot) { Remove-Item $packagesRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $packagesRoot | Out-Null

    # makes it easier to flatten subdirectories
    $packagesTempRoot = Join-Path $artifactsRoot 'roslyn-packages-temp'
    if (Test-Path $packagesTempRoot) { Remove-Item $packagesRoot -Recurse -Force }

    "Unpacking $zipPath => $packagesTempRoot" | Out-Default
    Expand-Archive $zipPath $packagesTempRoot
    
    "Flattening $packagesTempRoot => $packagesRoot" | Out-Default
    Get-ChildItem $packagesTempRoot -Recurse -File | Copy-Item -Destination $packagesRoot
    Remove-Item $packagesTempRoot -Recurse -Force

    [IO.File]::WriteAllText($lastBuildIdPath, $build.id)

    $result.changed = $true
    return $result
}

function Build-SharpLab($root, $branchRoot, $artifactsRoot, $roslynPackagesRoot, $roslynPackagesChanged) {
    $toolsRoot = "$PSScriptRoot\!tools"
    $sourceRoot = Join-Path $root 'source'
    $branchSharpLabRoot = Join-Path $branchRoot 'sharplab'
    $branchSourceRoot = Join-Path $branchSharpLabRoot 'source'
    if (!(Test-Path $branchSourceRoot)) {
        New-Item -ItemType Directory -Path $branchSourceRoot | Out-Null
    }
    
    "Building Roslyn package map…" | Out-Default
    $roslynVersionMap = @{}
    (Get-Item (Join-Path $roslynPackagesRoot '*.nupkg')) | % {
        $_.Name -match '^([^\d]+)\.(\d.+)\.nupkg$' | Out-Null
        $roslynVersionMap[$matches[1]] = $matches[2]
    }

    "Copying $sourceRoot => $branchSourceRoot" | Out-Default
    robocopy $sourceRoot $branchSourceRoot `
        /xo `
        /xd "bin" /xd "obj" /xd "node_modules" /xd ".vs" `
        /mir /np /ndl /njh | Out-Default

    $restoredPackagesRoot = (Join-Path $branchSharpLabRoot 'packages')
    
    "Updating Roslyn package versions in projects…" | Out-Default    
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

    if ($roslynPackagesChanged) {
        # Important because all Roslyn builds seem to use the exact same version
        "Deleting older Roslyn packages" | Out-Default
        $roslynVersionMap.Keys | % {
            $path = Join-Path $restoredPackagesRoot $_
            if (Test-Path $path) {
                Write-Host "  $path"
                Remove-Item $path -Recurse -Force
            }
        }
    }

    "Restoring $roslynPackagesRoot => $restoredPackagesRoot" | Out-Default
    dotnet restore $branchSourceRoot `
        --packages $restoredPackagesRoot `
        --source "https://api.nuget.org/v3/index.json" `
        --source $roslynPackagesRoot `
        --verbosity minimal | Out-Default
    if ($LastExitCode -ne 0) { throw "dotnet restore exited with error code $LastExitCode" }
    
    "Building SharpLab" | Out-Default
    dotnet msbuild "$branchSourceRoot/Server.Azure/Server.Azure.csproj" `
        /m /nodeReuse:false `
        /p:Configuration=Release `
        /p:UnbreakablePolicyReportEnabled=false | Out-Default
    if ($LastExitCode -ne 0) { throw "dotnet msbuild exited with error code $LastExitCode" }

    return @{
        webConfigRoot = "$branchSourceRoot/Server"
        binRoot = "$branchSourceRoot/Server.Azure/bin/Release"
    }
}

function Update-BranchInfo($name, $commitHash, $artifactsRoot) {
    $commit = Invoke-RestMethod "https://api.github.com/repos/dotnet/roslyn/commits/$commitHash"
    $json = @{
        name = $name
        repository = 'dotnet'
        commits = @(@{
            date = $commit.commit.author.date
            message = $commit.commit.message
            author = $commit.commit.author.name
            hash = $commitHash
        })
    }
    
    Set-Content (Join-Path $artifactsRoot "roslyn-branch-info.json") (ConvertTo-Json $json)
}

function Build-Branch($name, $root, $buildRoot) {    
    $fsName = 'dotnet-' + ($name -replace '[/\\:_]', '-')
    $branchRoot = Join-Path $buildRoot $fsName
    $artifactsRoot = Join-Path $branchRoot 'artifacts'
    $siteRoot = Join-Path $branchRoot 'site'
    
    Write-Host "Downloading Roslyn Azure build…"
    $roslynBuild = (Download-RoslynBuildPackages -BranchName $name -ArtifactsRoot $artifactsRoot)
    if (!$roslynBuild) { return }
    Write-Host ""
    
    Write-Host "Updating and building SharpLab…"
    $siteSource = (Build-SharpLab -Root $root -BranchRoot $branchRoot -RoslynPackagesRoot $roslynBuild.packagesRoot -RoslynPackagesChanged $roslynBuild.changed)
    Write-Host ""
    
    Write-Host "Copying to site"
    $siteBinRoot = Join-Path $siteRoot 'bin'
    if (!(Test-Path $siteBinRoot)) { New-Item -ItemType Directory -Path $siteBinRoot | Out-Null }
    
    robocopy $siteSource.binRoot $siteBinRoot `
        /xo `
        /mir /np /ndl /njh
    robocopy $siteSource.webConfigRoot $siteRoot 'web.config' `
        /xo `
        /np /ndl /njh
        
    Write-Host "Updating branch info"
    Update-BranchInfo $name -CommitHash $roslynBuild.commitHash -ArtifactsRoot $artifactsRoot
}

Write-Output "Environment:"
Write-Output "  Current Path:          $(Get-Location)"
Write-Output "  Script Root:           $PSScriptRoot"
$root = Resolve-Path "$PSScriptRoot/../.."
Write-Output "  Root:                  $root"
$buildRoot = Join-Path $root "!roslyn-branches"
if (!(Test-Path $buildRoot)) { New-Item -ItemType Directory -Path $buildRoot | Out-Null }
Write-Output "  Build Root:            $buildRoot"

$config = ConvertFrom-Json (Get-Content "$root\.roslyn-branches.json" -Raw)
Write-Output ""

Write-Output "Getting branches…"
Write-Output "  git ls-remote --heads https://github.com/dotnet/roslyn.git"
$branches = (git ls-remote --heads https://github.com/dotnet/roslyn.git)
if ($LastExitCode -ne 0) { throw "Command 'git ls-remote' failed with exit code $LastExitCode." }

$branches = $branches |
  % { $_ -replace '^.*refs/heads/(\S+).*$','$1' } |
  ? { $_ -match $($config.include) }
Write-Output ""

Write-Output "Building branches…"
$branches | % {
    Write-Output "*** $_"
    try {
        Build-Branch $_ -Root $root -BuildRoot $buildRoot
    }
    catch {
        $ex = $_.Exception
        Write-Warning "$($ex.Message)"    
    }    
    Write-Output ""
}