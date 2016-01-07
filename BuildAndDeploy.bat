@echo off
mkdir !roslyn\!tools
xcopy "%PROGRAMFILES(X86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\ildasm.exe" !roslyn\!tools /Y

SET TR_BUILD_ROOT_PATH=.
powershell "Set-ExecutionPolicy RemoteSigned Process; .\BuildAndDeploy.ps1"