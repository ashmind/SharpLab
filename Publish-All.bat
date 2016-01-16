@echo off
powershell "Set-ExecutionPolicy RemoteSigned Process; .\#build\Publish-All.ps1"