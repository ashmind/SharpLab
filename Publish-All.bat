@echo off
set powershell=powershell
set sysnative="%SystemRoot%\sysnative\WindowsPowerShell\v1.0\powershell.exe"
if exist %sysnative% (set powershell=%sysnative%)

%powershell% "Set-ExecutionPolicy RemoteSigned Process; .\#build\Publish-All.ps1 %*"