// app-service.bicep — App Service Plan + API App Service + Web App Service

@description('Location for all resources')
param location string

@description('Name suffix / environment tag')
param environmentName string

@description('Resource tags to apply to all resources')
param tags object = {}

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault URI for Key Vault reference app settings')
param keyVaultUri string

@description('.NET runtime version for App Services')
param dotnetVersion string = 'v10.0'

// App Service Plan — P1v2, matching infrastructure-needs.md
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-jjgnet-${environmentName}'
  location: location
  tags: tags
  sku: {
    name: 'P1v2'
    tier: 'PremiumV2'
    size: 'P1v2'
    family: 'Pv2'
    capacity: 1
  }
  kind: 'app'
  properties: {
    reserved: false // Windows
  }
}

// API App Service
resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'api-jjgnet-broadcast'
  location: location
  tags: union(tags, { 'app-role': 'api' })
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: dotnetVersion
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        // Secrets are injected at deploy time or via Key Vault references.
        // Example Key Vault reference pattern:
        // { name: 'MySecret', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=MySecret)' }
      ]
    }
  }
}

// Web App Service
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'web-jjgnet-broadcast'
  location: location
  tags: union(tags, { 'app-role': 'web' })
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: dotnetVersion
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
      ]
    }
  }
}

// Grant API and Web apps Key Vault Secrets User role so they can read secrets via KV references
resource apiKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, apiApp.id, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource webKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, webApp.id, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output appServicePlanId string = appServicePlan.id
output apiAppName string = apiApp.name
output apiAppPrincipalId string = apiApp.identity.principalId
output apiAppHostname string = apiApp.properties.defaultHostName
output webAppName string = webApp.name
output webAppPrincipalId string = webApp.identity.principalId
output webAppHostname string = webApp.properties.defaultHostName
