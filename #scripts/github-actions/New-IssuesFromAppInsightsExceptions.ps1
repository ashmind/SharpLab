#Requires -Version 6

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
    | where type !startswith 'Unbreakable'
    | where not (type == 'System.NotSupportedException' and assembly startswith 'SharpLab')
    | where type !in (
        'MirrorSharp.Advanced.EarlyAccess.RoslynSourceTextGuardException',
        'MirrorSharp.Advanced.EarlyAccess.RoslynCompilationGuardException',
        'SharpLab.Runtime.Internal.JitGenericAttributeException'
      )
    | extend containerType = iif(type == 'System.Exception', extract('Container host repor?ted an error:[\\r\\n]*([^:]+)', 1, outerMessage), '')
    | where containerType != 'SharpLab.Container.Manager.Internal.ContainerAllocationException'
    | extend containerMethod = iif(isnotempty(containerType), extract('[\\r\\n]+\\s*at ([^(]+)', 1, outerMessage), '')
    | project itemCount,
              app=tostring(customDimensions['Web App']),
              type=coalesce(containerType, type),
              method=iif(type != 'System.InvalidProgramException', coalesce(containerMethod, method), '<user code>'),
              query=strcat(
                'exceptions\n  | where type == \'', type,
                iif(type != 'System.InvalidProgramException', strcat('\'\n  | where method == \'', method, '\''), ''),
                iif(isnotempty(containerType), strcat('\n  | where outerMessage contains \'', containerType, '\''), '')
              )
    | summarize _count=sum(itemCount) by type, method, query, app
    | summarize counts=make_list(pack('app', app, 'count', _count), 100) by type, method, query
    | take 150
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
    gh issue list --label $ExceptionLabel --json title,url,number --state all --limit 500
})

Write-Host 'Processing exceptions'
$exceptions | % {
    $exceptionType = $_[0]
    $atMethod = $_[1]
    $query = $_[2]
    $counts = (ConvertFrom-Json $_[3])

    $title = "$exceptionType at $atMethod"
    Write-Host "  $title"
    $existing = $issues | ? { $_.title -eq $title }
    if (!$existing) {
        $body = ("
          AppInsights query:
          ``````Kusto
          $query
          ``````
        " -replace '          ','').Trim()

        Write-Host "    - creating"
        $url = $(gh issue create --title $title --body $body --label $ExceptionLabel)
        if ($LastExitCode -ne 0) {
            Write-Error "Command exited with code $LastExitCode"
        }
        Write-Host "    - $url"
        $issueNumber = $(Invoke-JsonCommand {
            gh issue view $url --json number
        }).number
    }
    else {
        Write-Host "    - found at $($existing.url)"
        $issueNumber = $existing.number
    }

    $comment = "| App | Count (last 24h) |`n| ------------- | ------------- |`n" +
        (($counts | Sort-Object 'app' | % { "| $($_.app) | $($_.count) |" }) -join "`n") +
        "`n| Total | $(($counts | Measure-Object 'count' -Sum).Sum) |"

    Write-Host "    - commenting"
    $commentUrl = (gh issue comment $issueNumber --body $comment)
    if ($LastExitCode -ne 0) {
        Write-Error "Command exited with code $LastExitCode"
    }
    Write-Host "    - $commentUrl"
}