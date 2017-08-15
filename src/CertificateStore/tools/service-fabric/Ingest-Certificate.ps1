<#
    .SYNOPSIS
        Imports an existing certificate into an Azure KeyVault in the format used by Azure Service Fabric.
  
    .DESCRIPTION
        Imports an existing certificate into an Azure KeyVault in the format used by Azure Service Fabric.
  
    .PARAMETER SubscriptionName
        The name of the Azure Resource Manager subscription in which the KeyVault is located.
  
    .PARAMETER VaultName
        The name of the Azure KeyVaulti into which the certificate should be installed.

    .PARAMETER CertificatePassword
        The password for the certificate
  
    .PARAMETER CertificateFile
        The fully-qualified path and filename to the certificate to be imported.
  
    .PARAMETER CertificateMetadataFile
        The fully-qualified path and filename to the certificate information file; if omitted the name of the certificate with .json will be assumed.
  
    .PARAMETER OutputCertificateMetadataFile
        The fully-qualified path and filename to which the new certificate information file, containing the KeyVault information, should
        be written; if ommited, the certificate information file used as input will be assumed (and overwitten)
    
    .OUTPUTS    
        Writes the details of the key vault entry into the certificate to the standard output stream.  
  
    .EXAMPLE
      ./ingest-certificate "Example-Dev" "Example-KeyVault" "abc1234" ".\local.pfx" "local.pfx.json" "local.pfx.json.new"     
#>

[CmdletBinding()]
param
(
    [Parameter(Mandatory=$true, HelpMessage="Please enter the name of the Azure subscription that you'd like to use.", Position=0)]
    [ValidateNotNullOrEmpty()]
    [string] $SubscriptionName,
  
    [Parameter(Mandatory=$true, HelpMessage="Please enter the name of the Azure KeyVault that you'd like to import the certificate into.", Position=2)]
    [ValidateNotNullOrEmpty()]
    [string] $VaultName,

    [Parameter(Mandatory=$true, HelpMessage="Please enter the password for the certificate.", Position=3)]
    [ValidateNotNull()]
    [string] $CertificatePassword,
  
    [Parameter(Mandatory=$true, HelpMessage="Please enter the filename, including any path necessary, to the certificate file that you'd like to import.", Position=4)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string] $CertificateFile,
  
    [Parameter(Mandatory=$false, HelpMessage="Please enter the filename, including any path necessary, to the certificate metadata file.  If not provided, the path of the certificate will be used with a .json extension added.", Position=5)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string] $CertificateMetadataFile = "$($CertificateFile).json",
  
    [Parameter(Mandatory=$false, HelpMessage="Please enter the filename, including any path necessary, for the updated certificate information file to be output to.  If not provided, the input certificate information file will be overwritten.", Position=6)]
    [string] $OutputCertificateMetadataFile = "$($CertificateMetadataFile)"
)

# ====================
# == Script Actions ==
# ====================

# Set the Azure subscription context.

$subscription = (Set-AzureRmContext -SubscriptionName $SubscriptionName)

if ($subscription -eq $null)
{
    throw "Subscription [$($SubscriptionName)] was not found"
}

# Read the certificate information.

$certInfo  = ((Get-Content $CertificateMetadataFile) -join "") | ConvertFrom-Json
$certName  = ([regex]"[^a-zA-Z1-9 ]").Replace($certInfo.Certificate, "").Replace(" ", "-")
$certPath  = (Get-ChildItem "$CertificateFile" ).FullName 
$certBytes = [System.IO.File]::ReadAllBytes($certPath)

# Construct the data to be stored in Key Vault

$keyVaultData          = @{}
$keyVaultData.data     = [System.Convert]::ToBase64String($certBytes)
$keyVaultData.dataType = [System.IO.Path]::GetExtension($certPath).ToLower()
$keyVaultData.password = $CertificatePassword

# Package the structure for ingestion.

$contentbytes = [System.Text.Encoding]::UTF8.GetBytes(($keyVaultData | ConvertTo-Json))
$content      = [System.Convert]::ToBase64String($contentbytes)
$secretValue  = ConvertTo-SecureString -String $content -AsPlainText -Force

# If we captured the leading dot in the file extension, strip it.

if ($keyVaultData.dataType.StartsWith("."))
{
    $keyVaultData.dataType = $keyVaultData.dataType.SubString(1)
}

# Add the certificate to KeyVault

$certResult = Set-AzureKeyVaultSecret -VaultName $VaultName -Name $certName -SecretValue $secretValue

if ($certResult -eq $null)
{
    throw "The certificate could not be added to the requested vault"
}

# Retrieve the certificate and KeyVault so that metadata properties can be captured.

$cert  = New-Object "System.Security.Cryptography.X509Certificates.X509Certificate2" "$certPath", "$CertificatePassword"
$vault =  Get-AzureRmKeyVault -VaultName $VaultName

# Update the certificate metadata information

$certProperties                    = @{}
$certProperties.FriendlyName       = $cert.FriendlyName
$certProperties.Thumbprint         = $cert.Thumbprint
$certProperties.Expires            = $cert.NotAfter.ToString( "yyyy-MM-ddTHH:mm:ss.fffffffZ" )
$certProperties.KeyVaultSecretName = $certName
$certProperties.KeyVaultUrl        = $certResult.Id 
$certProperties.KeyVault           = $certResult.VaultName
$certProperties.Subscription       = $SubscriptionName
$certProperties.ResourceGroup      = $vault.ResourceGroupName
$certProperties.Region             = $vault.Location

$certProperties.Keys | ForEach { $certInfo | Add-Member -MemberType NoteProperty -Name $_  -Value "$($certProperties[$_])" -Force }

# Write the metadata to the certificate file and return it so that it can be displayed/pipelined.

ConvertTo-Json $certInfo | Out-File $OutputCertificateMetadataFile
$certInfo
