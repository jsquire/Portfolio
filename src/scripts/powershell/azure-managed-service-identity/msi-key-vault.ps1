$rg         = "<< YOUR RESOURCE GROUP NAME >>"
$vaultName  = "<< YOUR VAULT NAME >>"
$secretName = "<< YOUR SECRET NAME >>"
$secretUri  = (Get-AzureKeyVaultSecret -VaultName "$vaultName" -Name "$secretName").Id
$vmName     = "<< YOUR VM NAME >>"
 
# Permissions Grant
$msiObjectId = ((Get-AzureRMVM -ResourceGroupName $rg -Name $vmName).Identity.PrincipalId)
Set-AzureRmKeyVaultAccessPolicy -VaultName $vaultName -ObjectId $msiObjectId -PermissionsToSecrets "Get","List"

# Create the bearer token
$realm = ""https://vault.azure.net""
$token = ((Invoke-WebRequest -Uri http://localhost:50342/oauth2/token -Method GET -Body @{resource=$realm} -Headers @{Metadata="true"}).Content | ConvertFrom-Json).access_token

# Retrieve via REST
$result = (Invoke-WebRequest -Uri "$($secretUri)?api-version=2016-10-01" -Method "GET" -Headers @{Authorization="Bearer $token"})
($result.Content | ConvertFrom-Json).Value
