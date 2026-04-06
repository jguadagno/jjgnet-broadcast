@description('Azure region for the App Service Plan and App Services.')
param location string

@description('Name of the App Service Plan.')
param appServicePlanName string = 'jjgnet-broadcast'

@description('SKU for the App Service Plan.')
param appServicePlanSku object = {
  name: 'P1v3'
  tier: 'PremiumV3'
  size: 'P1v3'
  family: 'Pv3'
  capacity: 1
}

@description('Name of the API App Service.')
param apiAppName string = 'api-jjgnet-broadcast'

@description('Name of the Web App Service.')
param webAppName string = 'web-jjgnet-broadcast'

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Resource tags.')
param tags object = {}

// Discovered values (production):
//   appServicePlanName = 'jjgnet-broadcast'
//   location           = 'westus'
//   SKU                = P1v3 (PremiumV3, capacity 1)
//   apiAppName         = 'api-jjgnet-broadcast' (httpsOnly, SystemAssigned identity)
//   webAppName         = 'web-jjgnet-broadcast' (httpsOnly, SystemAssigned identity)

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  kind: 'linux'
  sku: appServicePlanSku
  properties: {
    reserved: true
  }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
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
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
      healthCheckPath: '/health'
    }
    clientAffinityEnabled: false
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
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
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
      healthCheckPath: '/health'
    }
    clientAffinityEnabled: false
  }
}

// Staging slots for blue-green deployment (see issue #556)
resource apiStagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = {
  parent: apiApp
  name: 'staging'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false
    }
  }
}

resource webStagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = {
  parent: webApp
  name: 'staging'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false
    }
  }
}

output appServicePlanId string = appServicePlan.id
output apiAppId string = apiApp.id
output apiAppName string = apiApp.name
output apiAppPrincipalId string = apiApp.identity.principalId
output apiAppDefaultHostname string = apiApp.properties.defaultHostName
output webAppId string = webApp.id
output webAppName string = webApp.name
output webAppPrincipalId string = webApp.identity.principalId
output webAppDefaultHostname string = webApp.properties.defaultHostName
