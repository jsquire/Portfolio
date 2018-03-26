<#
  .SYNOPSIS
    Performs the actions needed to prepare the deployment for the Order Fulfillment application, including
    it's API and associated Web Jobs

  .DESCRIPTION
    Performs the actions needed to prepare the deployment for the Order Fulfillment application, including
    it's API and associated Web Jobs
#>

# =======================
# == Script Parameters ==
# =======================

[CmdletBinding()]
param
( 
  [Parameter(Mandatory=$true, ParameterSetName="Help", Position=0)]
  [Switch] $Help,

  [Parameter(Mandatory=$false, ParameterSetName="Execute", Position=0)]
  [ValidateScript({Test-Path $_ -PathType 'Container'})] 
  [string] $SourceRootPath = (Split-Path ($MyInvocation.MyCommand.Path)),

  [Parameter(Mandatory=$false, ParameterSetName="Execute", Position=1)]
  [ValidateScript({Test-Path $_ -PathType 'Container'})] 
  [string] $DestinationPath = (Join-Path $SourceRootPath "Deployment"),

  [Parameter(Mandatory=$false, ParameterSetName="Execute", Position=2)]
  [ValidateNotNullOrEmpty()] 
  [string] $BuildConfiguration = "Release"
)

# ========================
# == Global Definitions ==
# ========================

# These values should be kept in sync with the defaults from the parameter block
# above.  Unfortunately, there is no easy way to unify them, so they're defined here
# to improve locality

$Defaults = New-Object PSObject -Property `
@{  
    sourceRootPath     = (Split-Path ($MyInvocation.MyCommand.Path))
    destinationPath    = (Join-Path (Split-Path ($MyInvocation.MyCommand.Path)) "Deployment")
    buildConfiguration = "Release"
}

# ==========================
# == Function Definitions ==
# ==========================

function DisplayHelp
{
  <#
    .SYNOPSIS
      Displays the usage help text.

    .DESCRIPTION
      Displays the help text for usage, including the default parameter
      values.

    .PARAMETER Defaults
      The set of default values for parameters.

    .OUTPUTS
      Help text for usage to the console window.
  #>

  [CmdletBinding()]
  param
  (
    [Parameter(Mandatory=$false)]
    [PSObject] $Defaults
  )

  $indent = "    "

  Write-Host "`n"
  Write-Host "Order Fulfillment Deployment Help"
  Write-Host ""
  Write-Host "$($indent)This script prepares the raw build output from the solution for deployment by"
  Write-Host "$($indent)building the proper folder structure for Azure."
  Write-Host ""
  Write-Host "$($indent)Available Parameters:"
  Write-Host ""
  Write-Host "$($indent)-Help`t`t`tDisplays this message."
  Write-Host ""

  Write-Host "$($indent)-SourceRootPath`t`tThe path of the root of the solution source.  This will be used"
  Write-Host "$($indent)`t`t`t`tto probe the build output in a relative way to build the deployment."
  Write-Host ""
  Write-Host "$($indent)`t`t`t`tDefault: $($Defaults.sourceRootPath)"
  Write-Host ""

  Write-Host "$($indent)-DestinationPath`t`tThe path that the deployment structure will be created in."
  Write-Host ""
  Write-Host "$($indent)`t`t`t`tDefault: $($Defaults.destinationPath)"
  Write-Host ""

  Write-Host "$($indent)-BuildConfiguration`t`tThe build configuration that is being deployed.  This"
  Write-Host "$($indent)`t`t`t`twill typically be either Debug or Release."
  Write-Host ""
  Write-Host "$($indent)`t`t`t`tDefault: $($Defaults.buildConfiguration)"
  Write-Host ""
}

# ====================
# == Script Actions ==
# ====================

if ($Help)
{
  DisplayHelp $Defaults
  exit 0
}

Write-Output ""
Write-Output "Beginning deployment creation..."
Write-Output ""

$WebJobRootPath = "$($DestinationPath)\App_Data\jobs\continuous\"

# Ensure destination path

Write-Output "Creating or cleaning the destination path at: $($DestinationPath)..."

if (Test-Path $DestinationPath)
{
  Remove-Item $DestinationPath -Force -Recurse -ErrorAction Stop| Out-Null
}

New-Item -Path $DestinationPath  -ItemType "directory" -ErrorAction Stop | Out-Null

# Copy the API over as a web application

Write-Output "Creating API deployment..."
Copy-Item "$($SourceRootPath)\Api\bin\" "$($DestinationPath)\bin\" -Recurse -ErrorAction Stop
Copy-Item "$($SourceRootPath)\Api\*.config" $DestinationPath  -Recurse -ErrorAction Stop

# Ensure the web jobs root

Write-Output "Ensuring the root path for the web jobs..."

if (Test-Path $WebJobRootPath)
{
  Remove-Item $WebJobRootPath -Force -Recurse | Out-Null
}

New-Item -Path $WebJobRootPath -ItemType "directory" | Out-Null

# Copy the order processor over as a web job

Write-Output "Creating Order Processor deployment..."
Copy-Item "$($SourceRootPath)\OrderProcessor\bin\$($BuildConfiguration)" "$($WebJobRootPath)\OrderProcessor\" -Recurse -ErrorAction Stop

# Copy the order submitter over as a web job

Write-Output "Creating Order Submitter deployment..."
Copy-Item "$($SourceRootPath)\OrderSubmitter\bin\$($BuildConfiguration)" "$($WebJobRootPath)\OrderSubmitter\" -Recurse -ErrorAction Stop

# Copy the order submitter over as a web job

Write-Output "Creating Notifier deployment..."
Copy-Item "$($SourceRootPath)\Notifier\bin\$($BuildConfiguration)" "$($WebJobRootPath)\Notifier\" -Recurse -ErrorAction Stop

# The deployment is fully prepared.

Write-Output ""
Write-Output "The deployment has been successfully created"
exit 0