Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

cmd /c "git checkout master && git merge edge --ff-only && git push origin && git checkout edge"
if ($LastExitCode -ne 0) {
    throw "git failed with exit code $LastExitCode"
}

Write-Host Done