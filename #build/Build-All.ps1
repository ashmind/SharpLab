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

function Format-Xml($xml) {
    $stringWriter = New-Object IO.StringWriter

    $xmlWriter = New-Object Xml.XmlTextWriter $stringWriter
    $xmlWriter.Formatting = [Xml.Formatting]::Indented
    $xml.WriteTo($xmlWriter)
    $xmlWriter.Flush()

    return $stringWriter.ToString()
}

function ConvertTo-Hashtables($inputObject) {    
    if ($inputObject -isnot [PSObject]) {
        return $inputObject
    }
    
    $hash = @{}
    foreach ($property in $inputObject.PSObject.Properties) {
        $hash[$property.Name] = ConvertTo-Hashtables $property.Value
    }
    return $hash
}

function Get-PackageVersion($projectJsonPath, $name) {
    $content = [IO.File]::ReadAllText((Resolve-Path $projectJsonPath))
    $json = $content | ConvertFrom-Json
    if (!($json.dependencies.PSObject.Properties | ? { $_.Name -eq $name })) {
        return $null
    }

    return $json.dependencies.$name
}

function Map-PackageVersions($name, $projectJsonPaths, $framework, $maps) {
    $projectJsonPath = ($projectJsonPaths | ? { Test-Path $_ } | select -first 1)
    if (!$projectJsonPath) {
        Write-Output "  [WARNING] None of project.json paths found for $name"
        return
    }

    $version = Get-PackageVersion $projectJsonPath $name
    if (!$version) { return }
    $maps.referencePaths[$name] = "..\#packages\$name.$version\lib\$framework\$name.dll"
    $maps.packageVersions[$name] = $version
    $maps.assemblyVersions[$name] = '<mapped after package restore>'
}

function Rewrite-PackageVersions($packagesPath, $maps) {
    $packagesPath = (Resolve-Path $packagesPath)

    $content = [IO.File]::ReadAllText($packagesPath)
    $contentXml = [xml]$content
    $maps.packageVersions.GetEnumerator() | % {
        $name = $_.Key
        $version = $_.Value
        Select-Xml $contentXml -XPath '//package' |
            ? { $_.Node.id -eq $name } |
            % { $_.Node.version = $version }
    }
    $rewritten = Format-Xml $contentXml
    if ($rewritten -eq $content) {
        return
    }
    Set-Content $packagesPath $rewritten
}

function Rewrite-ProjectReferences($projectPath, $maps) {
    $xmlNamespaces = @{msbuild='http://schemas.microsoft.com/developer/msbuild/2003'}
    $projectPath = (Resolve-Path $projectPath)

    $content = [IO.File]::ReadAllText($projectPath)
    $contentXml = [xml]$content
    $maps.referencePaths.GetEnumerator() | % {
        $name = $_.Key
        $path = $_.Value
        Select-Xml -Xml $contentXml -XPath '//msbuild:Reference' -Namespace $xmlNamespaces |
            ? { $_.Node.Include -match "^$([regex]::Escape($name))(,|$)" } |
            % {
                $_.Node.Include = $name
                @(Select-Xml -Xml $_.Node -XPath 'descendant::msbuild:HintPath' -Namespace $xmlNamespaces)[0].Node.InnerText = $path
            }
    }

    $rewritten = Format-Xml $contentXml
    if ($rewritten -eq $content) {
        return
    }
    Set-Content $projectPath $rewritten
}

function Map-AssemblyVersions($siteBuildRoot, $maps) {
    @($maps.assemblyVersions.Keys) | % {
        $name = $_
        $path = Join-Path (Join-Path $siteBuildRoot '_') ($maps.referencePaths[$name])        
        $assembly = [Reflection.Assembly]::LoadFrom($path)
        $version = $assembly.GetName().Version
        $maps.assemblyVersions[$name] = $version
    }
}

function Rewrite-BindingRedirects($webConfigPath, $maps) {
    $xmlNamespaces = @{a='urn:schemas-microsoft-com:asm.v1'}
    $webConfigPath = (Resolve-Path $webConfigPath)
    
    $content = [IO.File]::ReadAllText($webConfigPath)
    $contentXml = [xml]$content

    $maps.assemblyVersions.GetEnumerator() | % {
        $name = $_.Key
        $version = $_.Value

        Select-Xml -Xml $contentXml -XPath "//a:dependentAssembly[a:assemblyIdentity[@name='$name']]/a:bindingRedirect" -Namespace $xmlNamespaces |
            % {
                $_.Node.SetAttribute('oldVersion', "0.0.0.0-$version")
                $_.Node.SetAttribute('newVersion', $version)
            }
    }

    $rewritten = Format-Xml $contentXml
    if ($rewritten -eq $content) {
        return
    }
    Set-Content $webConfigPath $rewritten
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

function Find-FirstFilePathDeep($path, $name) {
    $file = Get-ChildItem -Path $path -Filter $name -Recurse | select -first 1
    if (!$file) {
        return "$path\$name"
    }
    
    return $file.FullName
}

# Code ------
try {
    $Host.UI.RawUI.WindowTitle = "TryRoslyn Build" # prevents title > 1024 char errors

    #Write-Output "Killing VBCSCompiler instances"
    #taskkill /IM VBCSCompiler.exe /F

    Write-Output "Environment:"
    Write-Output "  Current Path:       $(Get-Location)"
    Write-Output "  Script Root:        $PSScriptRoot"

    $root = Resolve-Path "$PSScriptRoot\.."
    Write-Output "  Root:               $root"

    $sourceRoot = Resolve-Path "$root\source"
    Write-Output "  Source Root:        $sourceRoot"

    $roslynBuildRoot = Ensure-ResolvedPath "$root\!roslyn"
    Write-Output "  Roslyn Build Root:  $roslynBuildRoot"

    $sitesBuildRoot = Ensure-ResolvedPath "$root\!sites"
    Write-Output "  Sites Build Root:   $sitesBuildRoot"

    $buildConfig = ConvertFrom-Json (Get-Content "$root\Build.config.json" -Raw)

    ${env:$HOME} = $PSScriptRoot
    Invoke-Git . --version

    Write-Output "Restoring TryRoslyn packages..."
    &"$PSScriptRoot\#tools\nuget" restore "$sourceRoot\TryRoslyn.sln"

    $failedListPath = "$sitesBuildRoot\!failed.txt"
    if (Test-Path $failedListPath) {
        Write-Output "Deleting $failedListPath..."
        Remove-Item $failedListPath
    }

    $buildConfig.RoslynRepositories | % {
        Write-Output "Building repository '$($_.Name)' ($($_.Url))..."
        $roslynSourceRoot = Ensure-ResolvedPath "$roslynBuildRoot\$($_.Name)"
        Write-Output "  Roslyn Source Root: $roslynSourceRoot"

        Write-Output "Updating..."
        Update-RoslynSource -DirectoryPath $roslynSourceRoot -RepositoryUrl $($_.Url)

        Write-Output "Getting branches..."
        $branchesRaw = @(Invoke-Git $roslynSourceRoot branch --remote)
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

            $siteBuildRoot     = Ensure-ResolvedPath "$sitesBuildRoot\$branchFsName"
            $roslynBinaryRoot  = Ensure-ResolvedPath "$siteBuildRoot\!roslyn"
            $siteBuildTempRoot = Ensure-ResolvedPath "$siteBuildRoot\!temp"
            $siteCopyRoot      = Ensure-ResolvedPath "$siteBuildRoot\!site"
            
            $branchMapsPath = "$roslynBinaryRoot\!BranchMaps.json"

            try {
                Write-Output "  Copying $sourceRoot => $siteBuildRoot"
                robocopy $sourceRoot $siteBuildRoot /njh /njs /ndl /np /ns /xo /e /purge `
                    /xd "$roslynBinaryRoot" "$siteCopyRoot" "$siteBuildTempRoot" "$sourceRoot\Web"
                Write-Output ""

                Push-Location $roslynBuildRoot
                try {
                    &$BuildRoslynBranchIfModified `
                        -SourceRoot $roslynSourceRoot `
                        -BranchName $_ `
                        -OutputRoot $roslynBinaryRoot `
                        -IfBuilt {
                            Write-Output "Getting branch info..."
                            $branchInfo = @{
                                name    = $_
                                repository = $repositoryConfig.Name
                                commits = @(@{
                                    hash    =  (Invoke-Git $roslynSourceRoot log "$_" -n 1 --pretty=format:"%H" )
                                    date    =  (Invoke-Git $roslynSourceRoot log "$_" -n 1 --pretty=format:"%aI")
                                    author  =  (Invoke-Git $roslynSourceRoot log "$_" -n 1 --pretty=format:"%aN")
                                    message = @(Invoke-Git $roslynSourceRoot log "$_" -n 1 --pretty=format:"%B" ) -join "`r`n"
                                })
                            }
                            Set-Content "$roslynBinaryRoot\!BranchInfo.json" (ConvertTo-Json $branchInfo -Depth 100)
                            
                            Write-Output "Mapping references..."
                            $referenceMaps = @{
                                packageVersions = @{}
                                assemblyVersions = @{}
                                referencePaths = @{
                                    'Microsoft.CodeAnalysis'             = Find-FirstFilePathDeep $roslynBinaryRoot "Microsoft.CodeAnalysis.dll"
                                    'Microsoft.CodeAnalysis.CSharp'      = Find-FirstFilePathDeep $roslynBinaryRoot "Microsoft.CodeAnalysis.CSharp.dll"
                                    'Microsoft.CodeAnalysis.VisualBasic' = Find-FirstFilePathDeep $roslynBinaryRoot "Microsoft.CodeAnalysis.VisualBasic.dll"
                                }
                            }
                            $portableProjectJsonPath = "$roslynSourceRoot\src\Compilers\Core\Portable\project.json"
                            Map-PackageVersions 'System.Reflection.Metadata' `
                                -ProjectJsonPaths @($portableProjectJsonPath, "$roslynSourceRoot\src\Dependencies\Metadata\project.json") `
                                -Framework 'portable-net45+win8' -Maps $referenceMaps
                            Map-PackageVersions 'System.Collections.Immutable' `
                                -ProjectJsonPaths @($portableProjectJsonPath) `
                                -Framework 'portable-net45+win8+wp8+wpa81' -Maps $referenceMaps
                            Map-PackageVersions 'System.IO.FileSystem' `
                                -ProjectJsonPaths @($portableProjectJsonPath) `
                                -Framework 'net46' -Maps $referenceMaps
                            Map-PackageVersions 'System.Security.Cryptography.Algorithms' `
                                -ProjectJsonPaths @($portableProjectJsonPath) `
                                -Framework 'net46' -Maps $referenceMaps
                            Map-PackageVersions 'System.ValueTuple' `
                                -ProjectJsonPaths @($portableProjectJsonPath) `
                                -Framework 'netstandard1.0' -Maps $referenceMaps
                            Set-Content $branchMapsPath (ConvertTo-Json $referenceMaps -Depth 100)
                        }
                }
                finally {
                    Pop-Location
                }

                Push-Location $siteBuildRoot
                try {
                    $buildLogPath = "$siteBuildRoot\!build.log"

                    Write-Output "Loading reference maps..."
                    $referenceMaps = ConvertTo-Hashtables (ConvertFrom-Json (Get-Content $branchMapsPath -Raw))
                    @(
                        $referenceMaps.packageVersions.GetEnumerator(),
                        $referenceMaps.referencePaths.GetEnumerator()
                    ) | % { $_ } | % {
                        Write-Output "  $($_.Key) $($_.Value)"
                    }

                    Write-Output "Rewriting packages.config files..."
                    Get-ChildItem packages.config -Recurse -ErrorAction SilentlyContinue | % {
                        Rewrite-PackageVersions $_ $referenceMaps
                        Write-Output "  $($_.Directory.Name)\$($_.Name)"
                    }

                    Write-Output "Rewriting *.csproj files..."
                    Get-ChildItem *.csproj -Recurse -ErrorAction SilentlyContinue | % {
                        Rewrite-ProjectReferences $_ $referenceMaps
                        Write-Output "  $($_.Name)"
                    }

                    Write-Output "Restoring site packages..."
                    &"$PSScriptRoot\#tools\nuget" restore "$siteBuildRoot\TryRoslyn.sln"

                    Write-Output "Mapping assembly versions..."
                    Map-AssemblyVersions $siteBuildRoot $referenceMaps
                    $referenceMaps.assemblyVersions.GetEnumerator() | % {
                        Write-Output "  $($_.Key) $($_.Value)"
                    }

                    Write-Output "Rewriting Web.config file..."
                    Get-Item Web.Api\Web.config | % {
                        Rewrite-BindingRedirects $_ $referenceMaps
                        Write-Output "  $($_.Directory.Name)\$($_.Name)"
                    }

                    Write-Output "Building Web.Api.csproj..."
                    &$MSBuild Web.Api\Web.Api.csproj > $buildLogPath `
                        /p:OutputPath="""$siteCopyRoot\bin\\""" `
                        /p:IntermediateOutputPath="""$siteBuildTempRoot\\""" `
                        /p:AllowedReferenceRelatedFileExtensions=.pdb

                    if ($LastExitCode -ne 0) {
                        throw New-Object BranchBuildException("Build failed, see $buildLogPath", $buildLogPath)
                    }

                    Copy-Item "Web.Api\Global.asax" "$siteCopyRoot\Global.asax" -Force
                    Copy-Item "Web.Api\Web.config" "$siteCopyRoot\Web.config" -Force
                    Write-Output "TryRoslyn build done."
                }
                finally {
                    Pop-Location
                }
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