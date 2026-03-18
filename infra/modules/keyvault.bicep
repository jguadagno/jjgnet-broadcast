// keyvault.bicep — Azure Key Vault for application secrets

@description('Location for all resources')
param location string

@description('Name suffix / environment tag')
param environmentName string

@description('Resource tags to apply to all resources')
param tags object = {}

@description('Object ID of the AAD principal (user, SP, or managed identity) that gets admin access')
param adminPrincipalObjectId string

@description('Tenant ID for Key Vault')
param tenantId string = subscription().tenantId

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-jjgnet-${environmentName}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Grant the admin principal Key Vault Administrator role
resource adminRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, adminPrincipalObjectId, '00482a5a-887f-4fb3-b363-3b7fe8e74483')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483') // Key Vault Administrator
    principalId: adminPrincipalObjectId
    principalType: 'ServicePrincipal'
  }
}

output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
