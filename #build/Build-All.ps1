Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

# Write-Host, Write-Error and Write-Warning do not function properly in Azure
# So this mostly uses Write-Output for now
$BuildRoslynBranchIfModified = Resolve-Path "$PSScriptRoot\Build-RoslynBranchIfModified.ps1"
."$PSScriptRoot\Setup-Build.ps1"

function Update-RoslynSource($directoryPath, $repositoryUrl) {
    Write-Output "Updating $directoryPath"
    if (Test-Path "$directoryPath\.git") {
        Invoke-Git $directoryPath config user.email "tryroslyn@github.test"
        Invoke-Git $directoryPath config user.name "TryRoslyn"
        Invoke-Git $directoryPath fetch --prune origin
        @(Invoke-Git $directoryPath branch -vv) |
            ? { $_ -match '\s*(\S+)\s.*: gone\]' } |
            % { $matches[1] } |
            % { Invoke-Git $directoryPath branch -D $_ }
        Invoke-Git $directoryPath gc --auto
    }
    else {
        Invoke-Git . clone $repositoryUrl $directoryPath
    }
}

function Ensure-ResolvedPath($path) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType directory -Path $path | Out-Null
    }
    return Resolve-Path $path
}

function ConvertTo-Hashtable([PSCustomObject] $object) {
    $result = @{}
    $object.PSObject.Properties | % { $result[$_.Name] = $_.Value }
    return $result
}

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "TryRoslyn Build" # prevents title > 1024 char errors

    #Write-Output "Killing VBCSCompiler instances"
    #taskkill /IM VBCSCompiler.exe /F

    Write-Output "Environment:"
    Write-Output "  Current Path:          $(Get-Location)"
    Write-Output "  Script Root:           $PSScriptRoot"

    $root = Resolve-Path "$PSScriptRoot\.."
    Write-Output "  Root:                  $root"

    $sourceRoot = Resolve-Path "$root\source"
    Write-Output "  Source Root:           $sourceRoot"

    $roslynSourcesRoot = Ensure-ResolvedPath "$root\!roslyn\sources"
    Write-Output "  Roslyn Sources Root:   $roslynSourcesRoot"
    
    $roslynArtifactsRoot = Ensure-ResolvedPath "$root\!roslyn\artifacts"
    Write-Output "  Roslyn Artifacts Root: $roslynArtifactsRoot"

    $sitesRoot = Ensure-ResolvedPath "$root\!sites"
    Write-Output "  Sites Root:            $sitesRoot"

    $buildConfig = ConvertFrom-Json (Get-Content "$root\Build.config.json" -Raw)

    ${env:$HOME} = $PSScriptRoot
    Invoke-Git . --version
    
    Write-Output "Building TryRoslyn..."
    Write-Output "  Restoring packages..."
    &"$PSScriptRoot\#tools\nuget" restore "$sourceRoot\TryRoslyn.sln"
    Write-Output "  Server.csproj"
    &$MSBuild "$sourceRoot\Server\Server.csproj" `
        /p:AllowedReferenceRelatedFileExtensions=.pdb `
        /p:Configuration=Release
    if ($LastExitCode -ne 0) {
        Write-Error "TryRoslyn build failed."
    }

    Write-Output "Building AssemblyResolver..."
    $assemblyResolverSln = "$PSScriptRoot\#tools\AssemblyResolver\AssemblyResolver.sln"
    Write-Output "  Restoring packages..."
    &"$PSScriptRoot\#tools\nuget" restore $assemblyResolverSln
    &$MSBuild $assemblyResolverSln /p:Configuration=Debug
    if ($LastExitCode -ne 0) {
        Write-Error "AssemblyResolver build failed."
    }
    $assemblyResolver = Resolve-Path "$PSScriptRoot\#tools\AssemblyResolver\bin\Debug\AssemblyResolver.exe"

    $failedListPath = "$sitesRoot\!failed.txt"
    if (Test-Path $failedListPath) {
        Write-Output "Deleting $failedListPath..."
        Remove-Item $failedListPath
    }
    
    $buildConfig.RoslynRepositories | % {
        Write-Output "Building repository '$($_.Name)' ($($_.Url))..."
        $repositorySourceRoot = Ensure-ResolvedPath "$roslynSourcesRoot\$($_.Name)"
        Write-Output "  Repository Source Root: $repositorySourceRoot"

        Write-Output "Updating..."
        Update-RoslynSource -DirectoryPath $repositorySourceRoot -RepositoryUrl $($_.Url)

        Write-Output "Getting branches..."
        $branchesRaw = @(Invoke-Git $repositorySourceRoot branch --remote)
        $branches = $branchesRaw |
            ? { $_ -notmatch '^\s+origin/HEAD' } |
            % { ($_ -match 'origin/(.+)$') | Out-Null; $matches[1] }

        $repositoryConfig = ConvertTo-Hashtable $_
        $repositoryName = $repositoryConfig.Name
        $include = $repositoryConfig['Include']
        $exclude = $repositoryConfig['Exclude']

        Write-Output "  $branches"
        $branches | % {
            Write-Output ''
            Write-Output "*** $repositoryName`: $_"
            if ($include -and $_ -notmatch $include) {
                Write-Output "Does not match inclusion rules, skipped."
                return
            }
            if ($exclude -and $_ -match $exclude) {
                Write-Output "Matches exclusion rule, skipped."
                return
            }

            $Host.UI.RawUI.WindowTitle = "TryRoslyn Build: $_"
            $branchFsName = $repositoryName + "-" + ($_ -replace '[/\\:_]', '-')

            $siteRoot            = Ensure-ResolvedPath "$sitesRoot\$branchFsName"
            $branchArtifactsRoot = Ensure-ResolvedPath "$roslynArtifactsRoot\$branchFsName"

            try {
                &$BuildRoslynBranchIfModified `
                    -SourceRoot $repositorySourceRoot `
                    -BranchName $_ `
                    -ArtifactsRoot $branchArtifactsRoot `
                    -IfBuilt {
                        Write-Output "Getting branch info..."
                        $branchInfo = @{
                            name    = $_
                            repository = $repositoryConfig.Name
                            commits = @(@{
                                hash    =  (Invoke-Git $repositorySourceRoot log "$_" -n 1 --pretty=format:"%H" )
                                date    =  (Invoke-Git $repositorySourceRoot log "$_" -n 1 --pretty=format:"%aI")
                                author  =  (Invoke-Git $repositorySourceRoot log "$_" -n 1 --pretty=format:"%aN")
                                message = @(Invoke-Git $repositorySourceRoot log "$_" -n 1 --pretty=format:"%B" ) -join "`r`n"
                            })
                        }
                        Set-Content "$branchArtifactsRoot\BranchInfo.json" (ConvertTo-Json $branchInfo -Depth 100)
                    }

                Write-Output "Copying Server\Web.config to $siteRoot\Web.config..."
                Copy-Item "$sourceRoot\Server\Web.config" "$siteRoot\Web.config" -Force
                
                Write-Output "Resolving and copying assemblies..."
                $resolverLogPath = "$branchArtifactsRoot\AssemblyResolver.log"
                $resolverCommand = "&""$assemblyResolver""" +
                  " --source-bin ""$sourceRoot\Server\bin"" " +
                  " --roslyn-bin ""$branchArtifactsRoot\Binaries""" +
                  " --target ""$(Ensure-ResolvedPath $siteRoot\bin)""" +
                  " --target-app-config ""$siteRoot\Web.config""" +
                  " >> ""$resolverLogPath"""
                $resolverCommand | Out-File $resolverLogPath -Encoding 'Unicode'
                Invoke-Expression $resolverCommand
                if ($LastExitCode -ne 0) {
                    throw New-Object BranchBuildException("AssemblyResolver failed with code $LastExitCode, see $resolverLogPath.")
                }
                Write-Output "All done, looks OK."
            }
            catch {
                $ex = $_.Exception
                if ($ex -isnot [BranchBuildException]) {
                    throw
                }

                Write-Output "  [WARNING] $($ex.Message)"
                Add-Content $failedListPath @("$branchFsName ($repositoryName)", $ex.Message, '')
            }
        }
    }
}
catch {
    Write-Output "[ERROR] $_"
    Write-Output 'Returning exit code 1'
    exit 1
}