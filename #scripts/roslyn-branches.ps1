Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$setMatrix = &"$PSScriptRoot/roslyn-branches/Write-GitHubRunMatrix.ps1"
Write-Host "(captured by roslyn-branches.ps1)"
Write-Host ""

$matrix = ConvertFrom-Json ($setMatrix -replace '^.+::','')
$matrix.include | % {
    $row = $_
    try {
        &"$PSScriptRoot/roslyn-branches/Update-Branch.ps1" $row.branch
    }
    catch {
        if (!$row.required) {
            Write-Warning "Branch $($row.branch) failed: $_"
            return
        }
        Write-Error "Branch $($row.branch) failed: $_"
    }
}