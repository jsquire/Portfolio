<#
    .SYNOPSIS
        Retrieves the password of an existing certificate from Azure KeyVault, assuming the format used by Azure Service Fabric.
  
    .DESCRIPTION
        Retrieves the password for an existing certificate from Azure KeyVault, assuming the format used by Azure Service Fabric.  After
        being retrieved, the password is echoed to the output stream in plain text.
  
    .PARAMETER UseMetadata
        Signifies that the parameters to retrieve the certificate password should be read from the certificate metadata file.  
        If this parameter is used, it will obviate the use of the remaining parameters.

    .PARAMETER SubscriptionName
        The name of the Azure Resource Manager subscription in which the KeyVault is located.

    .PARAMETER VaultName
        The name of the Azure KeyVault in which the certificate is located.
  
    .PARAMETER CertificateName
        The name of the certificate defined in Azure KeyVault; this is typically set when the certifcate is ingested.

    .OUTPUTS    
        Writes the password to the output stream in plain text.  
  
    .EXAMPLE
      ./retrieve-password "Mimeo Dev" "Dev-KeyVault" "Localhost-SSL" 
#>

[CmdletBinding()]
param
(
    [Parameter(Mandatory=$true, HelpMessage="Please enter the filename, including any path necessary, to the certificate metadata file.", ParameterSetName="Metadata", Position=0)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string] $UseMetadata,
    
    [Parameter(Mandatory=$true, HelpMessage="Please enter the name of the Azure subscription that you'd like to use.", ParameterSetName="Default", Position=0)]
    [ValidateNotNullOrEmpty()]
    [string] $SubscriptionName,
  
    [Parameter(Mandatory=$true, HelpMessage="Please enter the name of the Azure KeyVault that you'd like to retrieve the certificate from.", ParameterSetName="Default", Position=2)]
    [ValidateNotNullOrEmpty()]
    [string] $VaultName,

    [Parameter(Mandatory=$true, HelpMessage="Please enter the name of the certificate, as it was created in Azure KeyVault.", ParameterSetName="Default", Position=3)]
    [ValidateNotNullOrEmpty()]
    [string] $CertificateName
)

# ====================
# == Script Actions ==
# ====================

# Determine if certificate metadata should be used, if so, populate the necessary parameter 
# values from the metadata.

if (-not [String]::IsNullOrEmpty($UseMetadata))
{   
    $metadata         = ((Get-Content $UseMetadata) -join "") | ConvertFrom-Json
    $SubscriptionName = $metadata.Subscription
    $VaultName        = $metadata.KeyVault
    $CertificateName  = $metadata.KeyVaultSecretName
}

# Set the Azure subscription context.

$subscriptionId = (Get-AzureRmSubscription | where { $_.SubscriptionName -eq $SubscriptionName }).SubscriptionId

if ($subscriptionId -eq $null)
{
    throw "Subscription [$($SubscriptionName)] was not found"
}

Set-AzureRmContext -SubscriptionId $subscriptionId | Out-Null

# Retrieve and decode the certficate information structure

$certResult = Get-AzureKeyVaultSecret -VaultName "$VaultName" -Name "$CertificateName"

if ($certResult -eq $null)
{
    throw "The certificate [$($CertificateName)] was not found"
}

$certBytes = [System.Convert]::FromBase64String($certResult.SecretValueText);
$certInfo  = [System.Text.Encoding]::UTF8.GetString($certBytes) | ConvertFrom-Json

# Extract the password 

Write-Output $certInfo.password
