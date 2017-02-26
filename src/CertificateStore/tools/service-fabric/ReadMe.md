# Service Fabric Tools #

### Overview  ###

This set of tools is intended to help manage KeyVault-held certificates in the format used by by an Azure Service Fabric cluster.  This format is also understood by Azure Resource Manager (ARM) templates with respect to certificates used in VM Scale Sets that are not part of a Service Fabric cluster.

### Usage ###

These scripts should be used from an Azure PowerShell prompt, where authentication was performed by running `Login-AzureRmAccount` prior to invoking the scripts.  The scripts themselves are fully doc-commented and help for them can be queried by running `get-help` against the script.

### Items ###

* **Create-CertificateMetadata.ps1**
  <br />_This script is used to create a skeleton structure for the metadata associated with a certificate.  It should be run before ingesting a certificate._
  
* **Ingest-Certificate.ps1**
  <br />_This script is used to ingest a certificate into an existing Azure KeyVault store._
  
* **Retrieve-Certificate.ps1**
  <br />_This script is used to retrieve a certificate that had previously been ingested using the `Ingest-Certificate.ps1` script or the Service Fabric utilities.  An option is provided to retrieve the needed parameter values from a certificate metadata file._
   
* **Retrieve-Certificate.ps1**
  <br />_This script is used to retrieve the password to a certificate that had previously been ingested using the `Ingest-Certificate.ps1` script or the Service Fabric utilities.  An option is provided to retrieve the needed parameter values from a certificate metadata file._
