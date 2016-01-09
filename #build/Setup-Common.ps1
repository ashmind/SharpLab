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

$global:MSBuild = ${env:ProgramFiles(x86)} + '\MSBuild\14.0\bin\MSBuild.exe'