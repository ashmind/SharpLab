Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$credential = Get-Credential -Message "Please enter your Azure login details.`r`nA separate user account is recommended."
Login-AzureRMAccount -Credential $credential

$key = New-Object byte[](32)
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($key)

$json = [ordered]@{
    UserName = $credential.UserName
    Password = $credential.Password | ConvertFrom-SecureString -Key $key
}
$resourceGroups = @(Get-AzureRmResourceGroup)

$index = 0
$options = $resourceGroups | % {
    New-Object Management.Automation.Host.ChoiceDescription("&$($index+1). $($_.ResourceGroupName)")
    $index += 1
}
$result = $Host.UI.PromptForChoice("Choose Azure resource group", "", $options, 0)
$json.ResourceGroupName = $resourceGroups[$result].ResourceGroupName

$appServicePlans = @(Get-AzureRMAppServicePlan)
$index = 0
$options = $appServicePlans | % {
    New-Object Management.Automation.Host.ChoiceDescription("&$($index+1). $($_.Name)")
    $index += 1
}
$result = $Host.UI.PromptForChoice("Choose Azure app service plan", "", $options, 0)
$json.AppServicePlanName = $appServicePlans[$result].Name

Set-Content '.\!Azure.config.json' (ConvertTo-Json $json)

Write-Host "Saved config at .\!Azure.config.json. Environment variable for build:"
Write-Host "SET TR_AZURE_PASSWORD_KEY=$([Convert]::ToBase64String($key))"