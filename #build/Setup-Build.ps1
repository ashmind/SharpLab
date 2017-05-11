Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = "SilentlyContinue" # https://www.amido.com/powershell-win32-error-handle-invalid-0x6/

Add-Type @"
    public class BranchBuildException : System.Exception {
        public BranchBuildException(string message, string logPath = null) : base(message) {
            LogPath = logPath;
        }
        
        public string LogPath { get; private set; }
    }
"@ -Language CSharp

function Invoke-Git(
    [string] $path
) {
    $command = ($args | % { "`"$_`"" }) -join ' '
    Push-Location $path
    try {
        Invoke-Expression "git $command"
        if ($LastExitCode -ne 0) {
            throw "Command 'git $command' failed with exit code $LastExitCode (in $path)."
        }
    }
    finally {
        Pop-Location
    }
}

$VisualStudioPath = (Get-ItemProperty HKLM:\SOFTWARE\wow6432node\Microsoft\VisualStudio\SxS\VS7)."15.0"
if (!$VisualStudioPath) {
    Write-Error "Failed to find Visual Studio 2017 (buuild currently uses it for MSBuild path)."
}
$global:MSBuild = $VisualStudioPath + 'MSBuild\15.0\Bin\MSBuild.exe'