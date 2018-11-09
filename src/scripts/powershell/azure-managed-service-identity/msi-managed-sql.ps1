# The Azure Active Directory module must be available.  (NEEDS TO BE RUN AS ADMIN)
Install-Module AzureAd -AllowClobber -Force
Import-Module AzureAD

# Just some variables to reduce typing.
$rg                         = "<< YOUR RESOURCE GROUP >>"
$dbServer                   = "<< YOUR SQL SERVER MACHINE NAME >>"
$db                         = "<< YOUR DATABASE NAME >>"
$vmName                     = "<< YOUR VM NAME >>"
$dbAdminEmail               = "<< SQL ADMIN User >>"
$dbAdminPass                = "<< SQL ADMIN Password >>"
$managementSubscriptionName = "<< YOUR AZURE SUBSCRIPTION NAME >>"
$managementUser             = "<< YOUR MANAGEMENT/DEVOPS USER >>"
$managementPass             = "<< YOUR MANAGEMENT/DEVOPS PASSWORD >>"

# In addition to having logged in for Azure RM, you must also connect to the Azure Active Directory to manage it.
$managementTenant = (Get-AzureRmSubscription -SubscriptionName $managementSubscriptionName).TenantId
$managementCreds  = New-Object System.Management.Automation.PSCredential ($managementUser, (ConvertTo-SecureString $managementPass -AsPlainText -Force))
$managementUser   = Connect-AzureAD -Credential $managementCreds -TenantId $managementTenant

# Set an active directory administrator for the SQL Server  (THIS ONLY NEEDS TO BE DONE ONCE)
$dbAdmin = (Get-AzureRmAdUser -mail "$dbAdminEmail")
Set-AzureRmSqlServerActiveDirectoryAdministrator -resourcegroupname $rg -servername $dbServer -objectid $dbAdmin.Id -DisplayName $dbAdmin.DisplayName | Out-Null

# Create an AD group to hold the service identities that will be accessing SQL Server. (THIS ONLY NEEDS TO BE DONE ONCE)
$sqlGroup = New-AzureADGroup -DisplayName "VM SqlServer Users" -MailEnabled $false -SecurityEnabled $true -MailNickName "NotSet"

# Add the VM's managed service identity to the Sql User AD group.
$vmObjectId = ((Get-AzureRMVM -ResourceGroupName $rg -Name $vmName).Identity.PrincipalId)
Add-AzureAdGroupMember -ObjectId $sqlGroup.ObjectId -RefObjectId $vmObjectId

# Grant the group membership  (THIS ONLY NEEDS TO BE DONE ONCE)
$dbConnectionString = "Data Source=$($dbServer).database.windows.net; Authentication=Active Directory Password; Initial Catalog=$($db); UID=$($dbAdminEmail); PWD=$($dbAdminPass)";

$dbConnection = New-Object System.Data.SqlClient.SqlConnection
$dbConnection.ConnectionString = $dbConnectionString 
$dbConnection.Open()

$dbCommand = $dbConnection.CreateCommand()
$dbCommand.CommandTimeout = 120

$dbCommand.CommandText = "create user [$($sqlGroup.DisplayName)] from external provider with default_schema = [dbo]"
$dbCommand.ExecuteNonQuery() | Out-Null

# Grant the group permissions to perform database actions.
$dbCommand.CommandText = "alter role db_datareader add member [$($sqlGroup.DisplayName)]"
$dbCommand.ExecuteNonQuery() | Out-Null

$dbConnection.Close()
$dbCommand.Dispose()
$dbConnection.Dispose()
