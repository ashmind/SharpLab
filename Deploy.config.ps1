Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
    
# This is an example build configuration -- feel free to change it
$global:DeployConfig = @{
    Target = 'Azure' # No other choices for now
    Azure = @{
        ProfileFileName = '!azureprofile' # Create by using Save-AzureRmProfile
        ResourceGroupName = 'Default-Web-EastUS' # You probably want to change this if you deploy to Azure
    }
}
