Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

function Invoke-JsonCommand($command) {
    $json = @($command.Invoke()) -join "`n"
    if ($LastExitCode -ne 0) {
        Write-Error "Command exited with code $LastExitCode"
    }

    if ($json.Trim() -eq '') {
        return
    }

    return ConvertFrom-Json $json
}

$ExceptionLabel = ":boom: exception"

$query = "
  exceptions
    | where client_Type != 'Browser'
    | where assembly !startswith 'Unbreakable'
    | where type !in ('MirrorSharp.Advanced.EarlyAccess.RoslynSourceTextGuardException', 'MirrorSharp.Advanced.EarlyAccess.RoslynCompilationGuardException')
    | summarize _count=sum(itemCount) by type, method
    | sort by _count desc
    | take 50
" -replace '\s+',' '

# Cannot use AZ PowerShell due to login performance issue
# https://github.com/Azure/login/issues/20
Write-Host 'Getting exceptions from App Insights'
$exceptions = (Invoke-JsonCommand {
  az monitor app-insights query `
    --analytics-query $query `
    --apps sharplab-insights `
    --resource-group SharpLab `
    --offset 24h
}).tables[0].rows

Write-Host 'Getting current issues from GitHub'
$issues = @(Invoke-JsonCommand {
  gh issue list --label $ExceptionLabel --json title,url --limit 500
})

Write-Host 'Processing exceptions'
$exceptions | % {
    $exceptionType = $_[0]
    $atMethod = $_[1]

    $title = "$exceptionType at $atMethod"
    Write-Host "  $title"
    $existing = $($issues | ? { $_.title -eq $title })
    if ($existing) {
        Write-Host "    - already exists at $($existing.url)"
        return
    }

    $body = "
      AppInsights query:
      ``````Kusto
      exceptions
        | where type = '$exceptionType'
        | where method = '$atMethod'
      ``````
    " -replace '      ',''

    Write-Host "    - creating"
    $url = $(gh issue create --title $title --body $body --label $ExceptionLabel)
    if ($LastExitCode -ne 0) {
        Write-Error "Command exited with code $LastExitCode"
    }
    Write-Host "    - $url"
}