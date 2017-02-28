<#
    .SYNOPSIS
       Creates a new certificate metadata file, with default entries to capture ingested properties.
  
    .DESCRIPTION
        Creates a new certificate metadata file, with default entries to capture ingested properties.
  
    .PARAMETER CertificateName
        The plain-text name that the certificate is known by.
  
    .PARAMETER CertificatePurpose
        The plain-text description (brief) of the purpose of the certificate.
    
    .PARAMETER OutFilename
        The fully-qualified path and filename that the file should be written to.
    
    .OUTPUTS
        Writes the certificate metadatea to the requesed file.  
  
    .EXAMPLE
      ./create-certificatemetadata "GoofyFoot Cluster" "Allows for communication between Service Fabric nodes" "cluster.pfx.json"     
#>

[CmdletBinding()]
param
(
    [Parameter(Mandatory=$true, HelpMessage="Please enter the plain-text name that the certificate is known by.", Position=0)]
    [ValidateNotNullOrEmpty()]
    [string] $CertificateName,
  
    [Parameter(Mandatory=$true, HelpMessage="Please enter the plain-text description (brief) of the purpose of the certificate.", Position=1)]
    [ValidateNotNullOrEmpty()]
    [string] $CertificatePurpose,
   
    [Parameter(Mandatory=$true, HelpMessage="Please enter the filename, including any path necessary, to the certificate information file.  If not provided, the path of the certificate will be used with a .json extension added.", Position=6)]
    [ValidateNotNullOrEmpty()]
    [string] $OutFilename
)

'{
    "Certificate"       : "' + $CertificateName + '",    
    "Purpose"           : "' + $CertificatePurpose +'",
    "FriendlyName"      : "",
    "Expires"           : "",
    "Subscription"      : "",
    "ResourceGroup"     : "",
    "KeyVaultUrl"       : "",
    "KeyVault"          : "",
    "KeyVaultSecretName": "" 
}' | Out-File $OutFilename
