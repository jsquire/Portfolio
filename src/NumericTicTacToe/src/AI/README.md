# AI Player Deployment

This directory contains deployment scripts and infrastructure templates for provisioning cloud-based AI services to support the Numeric Tic-Tac-Toe AI player implementation.

## Available Deployment Options

The AI player can be deployed using different cloud providers and AI services. Currently supported platforms:

### Azure OpenAI Service

Deploy using Azure's managed OpenAI service with GPT-5.2 model support.

- **Model**: GPT-5.2
- **Deployment Method**: PowerShell script with Bicep template
- **Prerequisites**: Azure CLI, Azure subscription
- **Deployment Script**: `Deploy-AzureOpenAI.ps1`

For detailed instructions, see [Azure Deployment Documentation](deployment/Azure/README.md).

## Quick Start

### Azure OpenAI Deployment

1. Verify prerequisites:

```powershell
az --version
```

2. Navigate to the AI directory:

```powershell
cd src\AI
```

3. Run the deployment script:

```powershell
.\deployment\Deploy-AzureOpenAI.ps1
```

The script will guide you through the deployment process and output the necessary configuration values upon completion.

### Script Parameters

Each deployment script supports various parameters for customization. Run with `-?` or `-Help` for parameter details:

```powershell
Get-Help .\Deploy-AzureOpenAI.ps1 -Detailed
```

Common parameters for Azure deployment:

- `ResourceGroupName`: Name of the resource group (default: rg-numtictactoe-ai)
- `Location`: Azure region (default: westus)
- `OpenAiServiceName`: Custom service name (optional)
- `SkipConfirmation`: Skip confirmation prompt

Example with custom parameters:

```powershell
.\Deploy-AzureOpenAI.ps1 -ResourceGroupName "my-ai-rg" -Location "eastus"
```

## Post-Deployment Configuration

After successful deployment, the script will output:

- Service endpoint URI
- API authentication keys
- Model deployment name
- Environment variable commands

Save these values for use in your AI player implementation. Example configuration:

```csharp
// Configuration in your application
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");
```

## Deployment Details

For platform-specific details including:

- Detailed prerequisites and installation steps
- Template parameter reference
- Security and network configuration
- Cost estimates and management
- Troubleshooting guidance
- Cleanup procedures

See the respective platform documentation:

- [Azure OpenAI Deployment Details](deployment/Azure/README.md)

## Security Considerations

- API keys are sensitive credentials. Store them securely using environment variables or secret management services.
- Never commit API keys or secrets to source control.
- Regularly rotate API keys according to your security policies.
- Review and apply appropriate network access controls for production deployments.

## Cost Management

Cloud AI services operate on a pay-per-use model. Monitor usage and costs through:

- Azure Portal Cost Management (for Azure deployments)
- Setting budget alerts and spending limits
- Regular review of token consumption metrics

Refer to platform-specific documentation for detailed pricing information.

## Cleanup

To remove deployed resources and avoid ongoing costs:

**Azure**:
```powershell
az group delete --name rg-numtictactoe-ai --yes
```

This will delete the resource group and all contained resources.
