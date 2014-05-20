@echo off
cd Jobs\Build-Roslyn
SET WEBROOT_PATH=..\..\Web
powershell .\Build-Roslyn.ps1
cd ..