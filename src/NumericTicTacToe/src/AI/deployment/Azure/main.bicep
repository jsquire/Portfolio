// Azure OpenAI Service Deployment Template for Numeric Tic-Tac-Toe AI Player
// This template deploys an Azure OpenAI service with GPT-4.1 model

targetScope = 'subscription'

@description('The name of the resource group to create.')
param resourceGroupName string

@description('The location for the resource group and Azure OpenAI service.')
param location string = 'westus'

@description('The name of the Azure OpenAI service. Must be globally unique.')
param openAiServiceName string = 'numtictactoe-openai-${uniqueString(subscription().id, resourceGroupName)}'

@description('The name of the GPT-4.1 model deployment.')
param modelDeploymentName string = 'gpt-4.1-deployment'

@description('The version of the GPT-4.1 model to deploy.')
param modelVersion string = '2025-04-14'

@description('The SKU name for the Azure OpenAI service.')
@allowed([
  'S0'
])
param skuName string = 'S0'

@description('The capacity (TPM in thousands) for the model deployment.')
param deploymentCapacity int = 10

@description('Tags to apply to the resources.')
param tags object = {
  Project: 'NumericTicTacToe'
  Component: 'AI-Player'
}

@description('The principal ID (object ID) of the user or service principal to grant access.')
param principalId string

// Create the resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Deploy resources into the resource group using a module
module openAiResources './openai-resources.bicep' = {
  name: 'openAiResourcesDeployment'
  scope: rg
  params: {
    openAiServiceName: openAiServiceName
    location: location
    modelDeploymentName: modelDeploymentName
    modelVersion: modelVersion
    skuName: skuName
    deploymentCapacity: deploymentCapacity
    tags: tags
    principalId: principalId
  }
}

// Outputs
@description('The name of the resource group.')
output resourceGroupName string = rg.name

@description('The name of the deployed Azure OpenAI service.')
output openAiServiceName string = openAiResources.outputs.openAiServiceName

@description('The endpoint URI for the Azure OpenAI service.')
output openAiEndpoint string = openAiResources.outputs.openAiEndpoint

@description('The primary API key for the Azure OpenAI service.')
@secure()
output openAiKey1 string = openAiResources.outputs.openAiKey1

@description('The secondary API key for the Azure OpenAI service.')
@secure()
output openAiKey2 string = openAiResources.outputs.openAiKey2

@description('The resource ID of the Azure OpenAI service.')
output openAiResourceId string = openAiResources.outputs.openAiResourceId

@description('The location where the resources were deployed.')
output deploymentLocation string = location

@description('The name of the model deployment.')
output modelDeploymentName string = openAiResources.outputs.modelDeploymentName
