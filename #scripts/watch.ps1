Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$env:CORECLR_ENABLE_PROFILING = 1
$env:CORECLR_PROFILER = '{67fb642f-51cd-4745-8b21-aacd2ec74e62}'
$env:CORECLR_PROFILER_PATH = Resolve-Path '.\source\Server.AspNetCore\bin\Debug\netcoreapp3.0\SharpLab.Native.Profiler.dll'

dotnet watch --project source/Server.AspNetCore run --urls "http://localhost:54100"