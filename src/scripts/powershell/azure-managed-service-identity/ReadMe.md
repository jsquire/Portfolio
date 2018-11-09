# Azure Managed Service Identity Utilities #

### Overview ###

Developed in early 2018, these scripts were used for management operations against [Azure](https://azure.microsoft.com) virtual machines using an early version of the managed service identity.

### Items ###

* **msi-key-vault.ps1**
  <br />_This script shows the process for granting an MSI access to a specific Azure Key Vault and then retrieving a secret from it using the MSI.  It is intended to be run under an account with management permissions to the target Key Vault._
  
* **msi-managed-sql.ps1**
  <br />_This script shows the process for adding an MSI to an Azure SQL Server hosted instance.  It is intended to be run under an account with management permissions to the Azure subscription, as it makes changes to the associated Azure Active Directory instance as well as SQL Server._
  
* **msi-search-subscription-resources.ps1**
  <br />_This script shows the process for granting an MSI permissions to enumerate resources contained in a specific resource group.  It is intended to be run under an account with management permissions to the Azure subscription, as it makes changes to the Azure role assignments._
  
* **msi-sql-query.ps1**
  <br />_This script serves as an example of how to run queries against an Azure SQL Server instance using an MSI that was previously granted permissions.  This script requires no special account considerations when run; it relies exclusively on the rights of the host machine's managed service identity._