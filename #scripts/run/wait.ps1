Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$webappServerUrl = 'http://localhost:44100'

Write-Host "Waiting for all services to start" -ForegroundColor White
Write-Host "(This might take several minutes)"
Write-Host ''

Start-Sleep -Seconds 5 # pointless to request before that
$ready = $false
while (!$ready) {
  try {
      Write-Host '.' -NoNewLine
      Invoke-RestMethod $webappServerUrl | Out-Null
      $ready = $true
  }
  catch {
      Start-Sleep -Seconds 1
  }
}

Write-Host ''
Write-Host "Opening $webappServerUrl" -ForegroundColor White
Start-Process $webappServerUrl

exit