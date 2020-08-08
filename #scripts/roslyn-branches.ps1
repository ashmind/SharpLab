Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

Push-Location "$PSScriptRoot/roslyn-branches"
try {
    $matrix = $null
    npm run generate-run-matrix | % {
        Write-Host $_
        if ($_ -match '^::set-output name=matrix::(.+)') {
            $matrix = ConvertFrom-Json $matches[1]    
            Write-Host ""
            Write-Host "[matrix captured by roslyn-branches.ps1]" -ForegroundColor DarkCyan
        }
    }

    $matrix.include | % {
        $row = $_
        try {
            npm run build-branch -- $row.branch
        }
        catch {
            if (!$row.required) {
                Write-Warning "Branch $($row.branch) failed: $_"
                return
            }
            Write-Error "Branch $($row.branch) failed: $_"
        }
    }
}
finally {
    Pop-Location
}