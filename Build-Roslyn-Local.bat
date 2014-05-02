@echo off
cd Jobs
SET WEBROOT_PATH=..\Web
powershell .\Build-Roslyn.ps1
cd ..