@description('Azure region for the Key Vault.')
param location string

@description('Name of the Key Vault.')
param keyVaultName string

@description('Azure AD Tenant ID.')
param tenantId string = tenant().tenantId

@description('SKU tier for the Key Vault.')
@allowed(['standard', 'premium'])
param skuName string = 'standard'

@description('Resource IDs of managed identities that need Get/List secret access (e.g. App Services).')
param secretReaderPrincipalIds array = []

@description('Enable soft delete protection.')
param enableSoftDelete bool = true

@description('Soft delete retention period in days.')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 90

@description('Resource tags.')
param tags object = {}

// Discovered values (production):
//   keyVaultName                = 'jjgnet-broadcasting'
//   location                    = 'westus2'
//   skuName                     = 'standard'
//   tenantId                    = 'bee716cf-fa94-4610-b72e-5df4bf5ac339'
//   enabledForDeployment        = false

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: skuName
    }
    tenantId: tenantId
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enableRbacAuthorization: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Grant Key Vault Secrets User role to each provided principal (RBAC model)
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource secretReaderRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in secretReaderPrincipalIds: {
  name: guid(keyVault.id, principalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]

output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
