<#
  .SYNOPSIS
    Launches and controls a console test run using PhantomJS

  .DESCRIPTION
    Launches a console test run using PhantomJS by passing a custom runner script.  Output from the test run is captured by this script to allow 
    for a rudimentary colorization syntax such that the console messages can be colored in the Windows command prompt.

    For more detailed help, please use the -Help switch. 
#>

# =======================
# == Script Parameters ==
# =======================

[CmdletBinding()]
param
( 
  [Parameter(Mandatory=$true, ParameterSetName="Help", Position=0)]
  [Switch] $Help,

  [Parameter(Mandatory=$true, ParameterSetName="Execute", Position=0)]
  [ValidateScript({Test-Path $_ -PathType 'Leaf'})] 
  [string] $TestSuitePath,

  [Parameter(Mandatory=$false, ParameterSetName="Execute")]
  [ValidateScript({Test-Path $_ -PathType 'Container'})] 
  [string] $TestScriptContainer = (Split-Path ($MyInvocation.MyCommand.Path)),

  [Parameter(Mandatory=$false, ParameterSetName="Execute")]
  [ValidateScript({Test-Path $_ -PathType 'Leaf'})] 
  [string] $TestScriptPath = (Join-Path $TestScriptContainer "test-runner.js"),

  [Parameter(Mandatory=$false, ParameterSetName="Execute")]
  [ValidateScript({Test-Path $_ -PathType 'Leaf'})] 
  [string] $PhantomPath = (Join-Path $TestScriptContainer "phantomjs.exe")
)

# ========================
# == Global Definitions ==
# ========================

# These values should be kept in sync with the defaults from the parameter block
# above.  Unfortunately, there is no easy way to unify them, so they're defined here
# to improve locality

$Defaults = New-Object PSObject -Property `
@{  
    testScriptContainer = (Split-Path ($MyInvocation.MyCommand.Path))
    testScriptPath      = (Join-Path (Split-Path ($MyInvocation.MyCommand.Path)) "test-runner.js")
    phantomPath         = (Join-Path (Split-Path ($MyInvocation.MyCommand.Path)) "phantomjs.exe")
}

$ColorPrefixes = New-Object PSObject -Property `
@{
  emphasis = "::EM::"
  error    = "::ERR::"
  warn     = "::W::"
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
  Write-Host "PhantomJS Console Runner Help"
  Write-Host ""
  Write-Host "$($indent)This script launches a suite of PhantonJS-based tests from the console"
  Write-Host "$($indent)capturing the output and enabling colored output in the Windows command"
  Write-Host "$($indent)prompt window."
  Write-Host ""
  Write-Host "$($indent)Available Parameters:"
  Write-Host ""
  Write-Host "$($indent)-Help`t`t`tDisplays this message."
  Write-Host ""

  Write-Host "$($indent)-TestSuitePath`t`tRequired.  The path, including filename, to the suite of"
  Write-Host "$($indent)`t`t`t`ttests to be run.  This will typically be an .html file."
  Write-Host ""
    
  Write-Host "$($indent)-TestScriptContainer`tThe path to the directory that contains the test runner script"
  Write-Host "$($indent)`t`t`t`tto be passed to PhantomJS. This parameter is most often used to specify the"
  Write-Host "$($indent)`t`t`t`troot where PhantomJS and the test runner script reside without overriding"
  Write-Host "$($indent)`t`t`t`ttheir default filenames."
  Write-Host ""
  Write-Host "$($indent)`t`t`t`tDefault: $($Defaults.testScriptContainer)"
  Write-Host ""

  Write-Host "$($indent)-TestScriptPath`t`tThe path, including filename, to the test runner script that should"
  Write-Host "$($indent)`t`t`t`tbe passed to PhantomJS.  This will typically be a .js file."
  Write-Host ""
  Write-Host "$($indent)`t`t`t`tDefault: $($Defaults.testScriptPath)"
  Write-Host ""

  Write-Host "$($indent)-PhantonPath`t`tThe path, including filename, to the PhantomJS executable that should"
  Write-Host "$($indent)`t`t`t`tbe used to invoke the test runner script.  This will typically be an .exe file."
  Write-Host ""
  Write-Host "$($indent)`t`t`t`tDefault: $($Defaults.phantomPath)"
  Write-Host ""
}

function ParseOutput             
{ 
   <#
    .SYNOPSIS
      Parses the incoming text to determine the display color
    
    .PARAMETER $target
      The output string to consider

    .OUTPUT
      An array containing the color to use for output (position 0) and the color prefix to be removed (position 1)
  #>

  param
  (
    [Parameter(Mandatory=$true)]
    [string] $target
  )             
  
  $color  = [ConsoleColor]::White    
  $strip  = [String]::Empty

  if ($target -eq $null)
  {
    return $color, $strip
  }
  
  if ($target.StartsWith($ColorPrefixes.error))
  {
    return [ConsoleColor]::Red, $ColorPrefixes.error
  }

  if ($target.StartsWith($ColorPrefixes.warn))
  {
    return [ConsoleColor]::Yellow, $ColorPrefixes.warn
  }

  if ($target.StartsWith($ColorPrefixes.emphasis))
  {
    return [ConsoleColor]::DarkGreen, $ColorPrefixes.emphasis
  }

  return $color, $strip  
}  

function WriteToHost             
{ 
   <#
    .SYNOPSIS
      Writes the output to the host in the correct color after processing it
    
    .PARAMETER $output
      The output string write

    .OUTPUT
      Writes the processed to the console host
  #>

  param
  (
    [Parameter(Mandatory=$false)]
    [string] $output = $null
  )             
  
  if ([String]::IsNullOrEmpty($output))
  {
    return
  }
  
  $parseResults = ParseOutput($output)

  if (-not [String]::IsNullOrEmpty($parseResults[1]))
  {
    $output = $output.Replace([string]$parseResults[1], [String]::Empty)
  }

  Write-Host $output -ForegroundColor $parseResults[0]
}  

# ====================
# == Script Actions ==
# ====================

if ($Help)
{
  DisplayHelp $Defaults
  exit 0
}

$phantomProcess = New-Object System.Diagnostics.Process
$startInfo      = New-Object System.Diagnostics.ProcessStartInfo
$suiteUri       = (New-Object Uri "File:///$($TestSuitePath)").ToString()

try
{
  # Configure the process 

  $startInfo.CreateNoWindow         = $true
  $startInfo.RedirectStandardOutput = $true
  $startInfo.UseShellExecute        = $false
  $startInfo.Arguments              = "$($TestScriptPath) $($suiteUri)"
  $startInfo.FileName               = $PhantomPath
  $startInfo.WorkingDirectory       = $TestScriptContainer

  $phantomProcess.StartInfo = $startInfo

  # Handle the output from the PhantomJS process

  Register-ObjectEvent -InputObject $phantomProcess -EventName "OutputDataReceived" -SourceIdentifier "OutputDataReceivedHandler" -Action `
  {    
    if ($EventArgs -ne $null) 
    {
      WriteToHost $EventArgs.Data
    }

  } | Out-Null

  # Start the PhantomJS process and wait for it to exit.
    
  if ($phantomProcess.Start())
  {  
    $phantomProcess.BeginOutputReadLine()
    $phantomProcess.WaitForExit()
  }
}

finally
{
  Unregister-Event "OutputDataReceivedHandler" -ErrorAction SilentlyContinue
  $phantomProcess.Dispose()
}