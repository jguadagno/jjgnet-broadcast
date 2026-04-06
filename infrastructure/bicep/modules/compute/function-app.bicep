@description('Azure region for the Function App.')
param location string

@description('Name of the Function App.')
param functionAppName string = 'jjgnet-broadcast'

@description('Resource ID of the App Service Plan to host the Function App.')
param appServicePlanId string

@description('Name of the Functions storage account (AzureWebJobsStorage).')
param functionsStorageAccountName string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Resource ID of the user-assigned managed identity for the Function App.')
param userAssignedIdentityId string

@description('Resource tags.')
param tags object = {}

// Discovered values (production):
//   functionAppName  = 'jjgnet-broadcast'
//   location         = 'westus'
//   kind             = 'functionapp,linux'
//   identity         = SystemAssigned + UserAssigned ('jjgnet-broadcast-id-8d7d')
//   httpsOnly        = true

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: functionsStorageAccountName
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
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
      ]
      healthCheckPath: '/health'
    }
    clientAffinityEnabled: false
  }
}

// Staging slot for blue-green deployment (see issue #556)
resource stagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = {
  parent: functionApp
  name: 'staging'
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      alwaysOn: false
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: functionsStorageAccountName
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
      ]
    }
  }
}

output functionAppId string = functionApp.id
output functionAppName string = functionApp.name
output functionAppPrincipalId string = functionApp.identity.principalId
output functionAppDefaultHostname string = functionApp.properties.defaultHostName
