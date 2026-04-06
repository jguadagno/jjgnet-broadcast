@description('Azure region for managed identities.')
param location string

@description('Resource tags.')
param tags object = {}

// Discovered user-assigned managed identities (production):
//   - 'api-jjgnet-broad-id-8130'   (westus)
//   - 'web-jjgnet-broad-id-8f0f'   (westus)
//   - 'jjgnet-broadcast-id-8d7d'   (westus)
// Note: All three app services also have SystemAssigned identities (managed by the platform).

resource apiManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'api-jjgnet-broad-id-8130'
  location: location
  tags: tags
}

resource webManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'web-jjgnet-broad-id-8f0f'
  location: location
  tags: tags
}

resource functionsManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'jjgnet-broadcast-id-8d7d'
  location: location
  tags: tags
}

output apiManagedIdentityId string = apiManagedIdentity.id
output apiManagedIdentityPrincipalId string = apiManagedIdentity.properties.principalId
output apiManagedIdentityClientId string = apiManagedIdentity.properties.clientId

output webManagedIdentityId string = webManagedIdentity.id
output webManagedIdentityPrincipalId string = webManagedIdentity.properties.principalId
output webManagedIdentityClientId string = webManagedIdentity.properties.clientId

output functionsManagedIdentityId string = functionsManagedIdentity.id
output functionsManagedIdentityPrincipalId string = functionsManagedIdentity.properties.principalId
output functionsManagedIdentityClientId string = functionsManagedIdentity.properties.clientId
