// Azure OpenAI Resources Module
// This module deploys the OpenAI service and related resources within a resource group

@description('The name of the Azure OpenAI service. Must be globally unique.')
param openAiServiceName string

@description('The location for the Azure OpenAI service.')
param location string

@description('The name of the GPT-5.2 model deployment.')
param modelDeploymentName string

@description('The version of the GPT-5.2 model to deploy.')
param modelVersion string

@description('The SKU name for the Azure OpenAI service.')
param skuName string

@description('The capacity (TPM in thousands) for the model deployment.')
param deploymentCapacity int

@description('Tags to apply to the resources.')
param tags object

@description('The principal ID (object ID) of the user or service principal to grant access.')
param principalId string

// Get the current user's object ID for role assignments
var currentUserObjectId = principalId

// Azure OpenAI Service Account
resource openAiAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiServiceName
  location: location
  kind: 'OpenAI'
  sku: {
    name: skuName
  }
  properties: {
    customSubDomainName: openAiServiceName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
  tags: tags
}

// GPT-4.1 Model Deployment
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAiAccount
  name: modelDeploymentName
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-5.2'
      version: modelVersion
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: deploymentCapacity
  }
}

// Resource Group Owner Role Assignment
resource resourceGroupOwnerAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, currentUserObjectId, 'Owner')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8e3af657-a8ff-443c-a75c-2fe8c4bcb635') // Owner role
    principalId: currentUserObjectId
    principalType: 'User'
  }
}

// Cognitive Services Contributor Role Assignment
resource cognitiveServicesContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAiAccount.id, currentUserObjectId, 'CognitiveServicesContributor')
  scope: openAiAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68') // Cognitive Services Contributor role
    principalId: currentUserObjectId
    principalType: 'User'
  }
}

// Outputs
@description('The name of the deployed Azure OpenAI service.')
output openAiServiceName string = openAiAccount.name

@description('The endpoint URI for the Azure OpenAI service.')
output openAiEndpoint string = openAiAccount.properties.endpoint

@description('The primary API key for the Azure OpenAI service.')
@secure()
output openAiKey1 string = openAiAccount.listKeys().key1

@description('The secondary API key for the Azure OpenAI service.')
@secure()
output openAiKey2 string = openAiAccount.listKeys().key2

@description('The resource ID of the Azure OpenAI service.')
output openAiResourceId string = openAiAccount.id

@description('The name of the model deployment.')
output modelDeploymentName string = modelDeployment.name
