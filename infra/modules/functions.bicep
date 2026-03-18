// functions.bicep — Azure Functions App (Consumption plan) + dedicated Functions storage account

@description('Location for all resources')
param location string

@description('Name suffix / environment tag')
param environmentName string

@description('Resource tags to apply to all resources')
param tags object = {}

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Connection string for the application storage account (queues + tables)')
@secure()
param appStorageConnectionString string

// Dedicated storage account for the Functions host runtime (separate from app storage)
resource functionsStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'stfnjjgnet${environmentName}'
  location: location
  kind: 'StorageV2'
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// Consumption plan for Azure Functions
resource functionsPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-fn-jjgnet-${environmentName}'
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {
    reserved: false // Windows
  }
}

var functionsStorageConnStr = 'DefaultEndpointsProtocol=https;AccountName=${functionsStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionsStorage.listKeys().keys[0].value}'

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'jjgnet-broadcast'
  location: location
  tags: union(tags, { 'app-role': 'functions' })
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionsPlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'   // Functions v4 on .NET 8 (isolated worker)
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: functionsStorageConnStr
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: functionsStorageConnStr
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'jjgnet-broadcast'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        // Application storage connection (queues + tables)
        {
          name: 'StorageConnectionString'
          value: appStorageConnectionString
        }
      ]
    }
  }
}

// Grant the Functions app Key Vault Secrets User role
resource fnKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, functionApp.id, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output functionAppName string = functionApp.name
output functionAppId string = functionApp.id
output functionAppPrincipalId string = functionApp.identity.principalId
output functionAppHostname string = functionApp.properties.defaultHostName
output functionsStorageName string = functionsStorage.name
