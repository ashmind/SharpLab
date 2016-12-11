@echo off
mkdir roslyn_build_root\!tools
xcopy "%PROGRAMFILES(X86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\ildasm.exe" roslyn_build_root\!tools /Y

powershell "Set-ExecutionPolicy RemoteSigned Process; .\#build\Build-All.ps1"