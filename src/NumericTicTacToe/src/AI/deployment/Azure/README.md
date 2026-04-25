# Azure OpenAI Deployment for Numeric Tic-Tac-Toe AI Player

This directory contains the infrastructure-as-code (Bicep) template for deploying an Azure OpenAI service to support the AI player implementation.

## Overview

The Bicep template deploys:
- **Azure OpenAI Service** with GPT-5.2 model
- **Role Assignments** for the deploying user:
  - Owner role on the resource group
  - Cognitive Services Contributor role on the OpenAI service
- **Model Deployment** configured for standard capacity

## Prerequisites

- **Azure CLI** version 2.0 or later ([Installation Guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- **Azure Subscription** with appropriate permissions
- **PowerShell** 5.1 or later (for the deployment script)

## Model Selection

This template uses **GPT-5.2** which:
- Does not require registration or special approval
- Is available in multiple regions including West US
- Supports text and image input, text output
- Has a 1,047,576 token context window (128,000 for provisioned deployments)
- Maximum output of 32,768 tokens

## Deployment

### Using PowerShell Script (Recommended)

1. Navigate to the AI project directory:
   ```powershell
   cd src\AI
   ```

2. Run the deployment script:
   ```powershell
   .\Deploy-AzureOpenAI.ps1
   ```

3. The script will:
   - Verify Azure CLI is installed
   - Check Azure login status
   - Display deployment configuration
   - Prompt for confirmation
   - Create the resource group
   - Deploy the Bicep template
   - Display the endpoint URI and API keys

### Using Azure CLI Directly

1. Log in to Azure:
   ```bash
   az login
   ```

2. Create a resource group:
   ```bash
   az group create --name rg-numtictactoe-ai --location westus
   ```

3. Get your principal ID:
   ```bash
   PRINCIPAL_ID=$(az ad signed-in-user show --query id -o tsv)
   ```

4. Deploy the template:
   ```bash
   az deployment group create \
     --name openai-deployment \
     --resource-group rg-numtictactoe-ai \
     --template-file src/AI/deployment/Azure/main.bicep \
     --parameters principalId=$PRINCIPAL_ID location=westus
   ```

5. Retrieve outputs:
   ```bash
   az deployment group show \
     --name openai-deployment \
     --resource-group rg-numtictactoe-ai \
     --query properties.outputs
   ```

## Template Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `openAiServiceName` | string | Auto-generated | Globally unique name for the OpenAI service |
| `location` | string | `westus` | Azure region for deployment |
| `modelDeploymentName` | string | `gpt-5.2-deployment` | Name for the model deployment |
| `modelVersion` | string | `2026-03-11` | Version of GPT-5.2 to deploy |
| `skuName` | string | `S0` | SKU for the OpenAI service (S0 only) |
| `deploymentCapacity` | int | `10` | TPM capacity in thousands |
| `tags` | object | See template | Tags to apply to resources |
| `principalId` | string | Required | Object ID of user/service principal |

## Outputs

The deployment produces the following outputs:

- **openAiServiceName**: Name of the deployed service
- **openAiEndpoint**: HTTPS endpoint URI
- **openAiKey1**: Primary API key (secure)
- **openAiKey2**: Secondary API key (secure)
- **openAiResourceId**: Full resource ID
- **deploymentLocation**: Deployment region
- **modelDeploymentName**: Name of the model deployment

## Environment Variables

After deployment, configure these environment variables for your application:

```bash
# Bash/Linux
export AZURE_OPENAI_ENDPOINT="https://your-service.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key-here"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-5.2-deployment"

# PowerShell
$env:AZURE_OPENAI_ENDPOINT = "https://your-service.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY = "your-api-key-here"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-5.2-deployment"

# Windows CMD
set AZURE_OPENAI_ENDPOINT=https://your-service.openai.azure.com/
set AZURE_OPENAI_API_KEY=your-api-key-here
set AZURE_OPENAI_DEPLOYMENT_NAME=gpt-5.2-deployment
```

## Security Considerations

1. **API Keys**: The template outputs API keys. Store them securely:
   - Use Azure Key Vault for production
   - Never commit keys to source control
   - Rotate keys regularly

2. **Network Access**: The template enables public network access by default
   - For production, consider private endpoints
   - Configure network ACLs as needed

3. **Role Assignments**: The template grants:
   - Owner role on the resource group (full access)
   - Cognitive Services Contributor role (manage service)

## Cost Management

- **Pricing**: Standard (S0) SKU with pay-per-use model
- **Monitor Usage**: Check Azure Portal → Cost Management
- **Set Budgets**: Configure alerts in Azure Cost Management
- **Token Usage**: GPT-5.2 pricing varies by region

See [Azure OpenAI Pricing](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/) for details.

## Cleanup

To remove all deployed resources:

```bash
# Delete the resource group and all contained resources
az group delete --name rg-numtictactoe-ai --yes --no-wait
```

## Troubleshooting

### Deployment Fails with "DeploymentQuotaExceeded"
- Your subscription may have quota limits
- Request a quota increase in Azure Portal
- Try a different region

### "InvalidTemplate" Error
- Verify Bicep CLI is up to date: `az bicep version`
- Upgrade if needed: `az bicep upgrade`

### "PrincipalNotFound" Error
- Ensure you're logged in: `az login`
- Verify your principal ID: `az ad signed-in-user show`

### Role Assignment Fails
- Check your subscription permissions
- You need Owner or User Access Administrator role

## Additional Resources

- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [GPT-5.2 Model Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#gpt-5-series)
- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure CLI Reference](https://learn.microsoft.com/en-us/cli/azure/)

## Support

For issues specific to this deployment template, please refer to the project's main README or open an issue in the project repository.
