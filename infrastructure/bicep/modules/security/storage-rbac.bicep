@description('Azure region for reference (not actively used by role assignments).')
param location string

@description('Name of the Functions storage account (jjgnetbeb6).')
param storageAccountName string

@description('Principal ID of the Functions app system-assigned managed identity.')
param functionsPrincipalId string

@description('Resource tags.')
param tags object = {}

// Reference the existing Functions storage account (do not redeploy)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' existing = {
  name: storageAccountName
}

// Built-in Azure Storage role definition IDs
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'

// Grant Storage Blob Data Contributor role to Functions system-assigned identity
resource blobContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionsPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: functionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Grant Storage Queue Data Contributor role to Functions system-assigned identity
resource queueContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionsPrincipalId, storageQueueDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
    principalId: functionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Grant Storage Table Data Contributor role to Functions system-assigned identity
resource tableContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionsPrincipalId, storageTableDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
    principalId: functionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output storageAccountId string = storageAccount.id
output roleAssignmentsGranted array = [
  'Storage Blob Data Contributor'
  'Storage Queue Data Contributor'
  'Storage Table Data Contributor'
]
