<#
.SYNOPSIS
    Deploys Azure OpenAI service for Numeric Tic-Tac-Toe AI player.

.DESCRIPTION
    This script deploys an Azure OpenAI service with GPT-4.1 model using Azure CLI.
    The Bicep template creates the resource group and all Azure resources.
    Outputs the service endpoint and API keys for use as environment variables.

.PARAMETER ResourceGroupName
    The name of the resource group to create. Required.

.PARAMETER Location
    The Azure region for deployment. Default: 'westus'

.PARAMETER OpenAiServiceName
    Optional custom name for the Azure OpenAI service. If not provided, a unique name will be generated.

.PARAMETER SkipConfirmation
    Skip the confirmation prompt before deployment.

.EXAMPLE
    .\Deploy-AzureOpenAI.ps1 -ResourceGroupName "rg-numtictactoe-ai"

.EXAMPLE
    .\Deploy-AzureOpenAI.ps1 -ResourceGroupName "my-ai-rg" -Location "eastus"

.EXAMPLE
    .\Deploy-AzureOpenAI.ps1 -ResourceGroupName "rg-ai" -SkipConfirmation

.NOTES
    Requires: Azure CLI 2.0 or later
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$Location = "westus",

    [Parameter(Mandatory = $false)]
    [string]$OpenAiServiceName = "numtic-ai",

    [Parameter(Mandatory = $false)]
    [switch]$SkipConfirmation
)

# Set strict mode for better error handling.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Script constants.

$scriptPath = $PSScriptRoot
$templatePath = Join-Path $scriptPath "deployment" "Azure" "main.bicep"

# Helper function to write colored output.

function Write-ColorOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [Parameter(Mandatory = $false)]
        [ConsoleColor]$ForegroundColor = [ConsoleColor]::White
    )

    $originalColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $Host.UI.RawUI.ForegroundColor = $originalColor
}

# Helper function to check if Azure CLI is installed.

function Test-AzureCLI {
    try {
        $null = az version 2>$null
        return $true
    }
    catch {
        return $false
    }
}

# Main deployment logic.

try {
    Write-ColorOutput "==================================================" -ForegroundColor Cyan
    Write-ColorOutput "Azure OpenAI Deployment for Numeric Tic-Tac-Toe" -ForegroundColor Cyan
    Write-ColorOutput "==================================================" -ForegroundColor Cyan
    Write-Output ""

    # Verify Azure CLI is installed.

    Write-ColorOutput "Checking prerequisites..." -ForegroundColor Yellow

    if (-not (Test-AzureCLI)) {
        Write-Error "Azure CLI is not installed or not in PATH. Please install Azure CLI from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        exit 1
    }

    Write-ColorOutput "✓ Azure CLI is installed" -ForegroundColor Green

    # Verify Bicep template exists.

    if (-not (Test-Path $templatePath)) {
        Write-Error "Bicep template not found at: $templatePath"
        exit 1
    }

    Write-ColorOutput "✓ Bicep template found" -ForegroundColor Green
    Write-Output ""

    # Check if user is logged in to Azure.

    Write-ColorOutput "Checking Azure authentication..." -ForegroundColor Yellow

    $accountInfo = az account show 2>$null | ConvertFrom-Json

    if (-not $accountInfo) {
        Write-ColorOutput "Not logged in. Logging in to Azure..." -ForegroundColor Yellow

        az login

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Azure login failed."
            exit 1
        }

        $accountInfo = az account show | ConvertFrom-Json
    }

    Write-ColorOutput "✓ Logged in to Azure" -ForegroundColor Green
    Write-ColorOutput "  Subscription: $($accountInfo.name)" -ForegroundColor Gray
    Write-ColorOutput "  Subscription ID: $($accountInfo.id)" -ForegroundColor Gray
    Write-ColorOutput "  User: $($accountInfo.user.name)" -ForegroundColor Gray
    Write-Output ""

    # Get the current user's object ID for role assignments.

    Write-ColorOutput "Retrieving user identity..." -ForegroundColor Yellow

    $currentUser = az ad signed-in-user show | ConvertFrom-Json
    $principalId = $currentUser.id

    Write-ColorOutput "✓ User identity retrieved" -ForegroundColor Green
    Write-ColorOutput "  Principal ID: $principalId" -ForegroundColor Gray
    Write-Output ""

    # Display deployment configuration.

    Write-ColorOutput "Deployment Configuration:" -ForegroundColor Cyan
    Write-ColorOutput "  Resource Group: $ResourceGroupName" -ForegroundColor White
    Write-ColorOutput "  Location: $Location" -ForegroundColor White
    Write-ColorOutput "  Model: GPT-4.1 (2025-04-14)" -ForegroundColor White

    if ($OpenAiServiceName) {
        Write-ColorOutput "  Service Name: $OpenAiServiceName" -ForegroundColor White
    }
    else {
        Write-ColorOutput "  Service Name: (auto-generated)" -ForegroundColor White
    }

    Write-Output ""

    # Confirmation prompt.

    if (-not $SkipConfirmation) {
        $confirmation = Read-Host "Do you want to proceed with the deployment? (y/N)"

        if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
            Write-Warning "Deployment cancelled by user."
            exit 0
        }
    }

    Write-Output ""

    # Deploy Bicep template at subscription scope.

    Write-ColorOutput "Deploying Azure OpenAI service..." -ForegroundColor Yellow
    Write-ColorOutput "  This will create resource group '$ResourceGroupName' and deploy the OpenAI service..." -ForegroundColor Gray
    Write-ColorOutput "  This may take several minutes..." -ForegroundColor Gray
    Write-Output ""

    $deploymentName = "openai-deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"

    # Build the parameter arguments.

    $paramArgs = @(
        "--parameters", "resourceGroupName=$ResourceGroupName",
        "--parameters", "location=$Location",
        "--parameters", "principalId=$principalId"
    )

    if ($OpenAiServiceName) {
        $paramArgs += "--parameters", "openAiServiceName=$OpenAiServiceName"
    }

    $deploymentOutput = az deployment sub create `
        --name $deploymentName `
        --location $Location `
        --template-file $templatePath `
        @paramArgs `
        --output json

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Deployment failed."
        exit 1
    }

    $deployment = $deploymentOutput | ConvertFrom-Json

    Write-ColorOutput "✓ Deployment completed successfully" -ForegroundColor Green
    Write-Output ""

    # Extract outputs.

    $outputs = $deployment.properties.outputs
    $endpoint = $outputs.openAiEndpoint.value
    $key1 = $outputs.openAiKey1.value
    $key2 = $outputs.openAiKey2.value
    $serviceName = $outputs.openAiServiceName.value
    $modelDeployment = $outputs.modelDeploymentName.value

    # Display results.

    Write-ColorOutput "==================================================" -ForegroundColor Cyan
    Write-ColorOutput "Deployment Complete!" -ForegroundColor Green
    Write-ColorOutput "==================================================" -ForegroundColor Cyan
    Write-Output ""

    Write-ColorOutput "Service Details:" -ForegroundColor Cyan
    Write-ColorOutput "  Service Name: $serviceName" -ForegroundColor White
    Write-ColorOutput "  Endpoint URI: $endpoint" -ForegroundColor White
    Write-ColorOutput "  Model Deployment: $modelDeployment" -ForegroundColor White
    Write-ColorOutput "  Location: $Location" -ForegroundColor White
    Write-Output ""

    Write-ColorOutput "API Keys:" -ForegroundColor Cyan
    Write-ColorOutput "  Primary Key: $key1" -ForegroundColor White
    Write-ColorOutput "  Secondary Key: $key2" -ForegroundColor White
    Write-Output ""

    Write-ColorOutput "Environment Variables (copy these for your application):" -ForegroundColor Cyan
    Write-Output ""
    Write-ColorOutput "# PowerShell" -ForegroundColor Yellow
    Write-Output "`$env:AZURE_OPENAI_ENDPOINT = `"$endpoint`""
    Write-Output "`$env:AZURE_OPENAI_API_KEY = `"$key1`""
    Write-Output "`$env:AZURE_OPENAI_DEPLOYMENT_NAME = `"$modelDeployment`""
    Write-Output ""
    Write-ColorOutput "# Bash/Linux" -ForegroundColor Yellow
    Write-Output "export AZURE_OPENAI_ENDPOINT=`"$endpoint`""
    Write-Output "export AZURE_OPENAI_API_KEY=`"$key1`""
    Write-Output "export AZURE_OPENAI_DEPLOYMENT_NAME=`"$modelDeployment`""
    Write-Output ""
    Write-ColorOutput "# Windows Command Prompt" -ForegroundColor Yellow
    Write-Output "set AZURE_OPENAI_ENDPOINT=$endpoint"
    Write-Output "set AZURE_OPENAI_API_KEY=$key1"
    Write-Output "set AZURE_OPENAI_DEPLOYMENT_NAME=$modelDeployment"
    Write-Output ""

    Write-ColorOutput "Next Steps:" -ForegroundColor Cyan
    Write-ColorOutput "  1. Copy the environment variables above" -ForegroundColor White
    Write-ColorOutput "  2. Add them to your development environment" -ForegroundColor White
    Write-ColorOutput "  3. Use the OpenAI package in your AI player implementation" -ForegroundColor White
    Write-Output ""

    Write-ColorOutput "Resource Management:" -ForegroundColor Cyan
    Write-ColorOutput "  View in Portal: https://portal.azure.com/#@/resource$($deployment.properties.outputResources[0].id)" -ForegroundColor White
    Write-ColorOutput "  Delete Resources: az group delete --name $ResourceGroupName" -ForegroundColor White
    Write-Output ""

}
catch {
    Write-Output ""
    Write-ColorOutput "==================================================" -ForegroundColor Red
    Write-ColorOutput "Deployment Failed" -ForegroundColor Red
    Write-ColorOutput "==================================================" -ForegroundColor Red
    Write-Error "Error: $($_.Exception.Message)"
    Write-Output ""
    Write-ColorOutput "Stack Trace:" -ForegroundColor Red
    Write-ColorOutput $_.ScriptStackTrace -ForegroundColor Red

    exit 1
}
