$subscription = "<< THE NAME OF YOUR AZURE SUBSCRIPTION >>"
$rg           = "<< YOUR RESOURCE GROUP NAME >>"
$vmName       = "<< YOUR VM NAME >>"
$tag          = "<< THE TAG TO QUERY FOR >>"

# Grant "Reader" permissions to the resource group (THIS HAS TO BE DONE FROM AN ACCOUNT THAT CAN MANAGE THE SUBSCRIPTION)
$msiObjectId = (Get-AzureRMVM -ResourceGroupName $rg -Name $vmName).Identity.PrincipalId
New-AzureRmRoleAssignment -ObjectId $msiObjectId -RoleDefinitionName "Reader" -ResourceGroupName $rg

# Retrieve a management-enabled token
$realm = "https://management.azure.com/"
$token = ((Invoke-WebRequest -Uri http://localhost:50342/oauth2/token -Method GET -Body @{resource=$realm} -Headers @{Metadata="true"}).Content | ConvertFrom-Json).access_token

# List tagged resources
Login-AzureRmAccount -AccessToken $token -AccountId "$msiObjectId" 
Select-AzureRmSubscription -SubscriptionName $subscription
Get-AzureRmResource | where { $_.Tags -ne $null -and  $_.Tags.Contains("$tag") }
