@echo off
cd Jobs\Build-Roslyn
mkdir ..\..\!roslyn-build-tools
xcopy "%PROGRAMFILES(X86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\ildasm.exe" ..\..\!roslyn-build-tools\ /Y

SET WEBROOT_PATH=..\..\Web
powershell .\Build-Roslyn.ps1
cd ..\..