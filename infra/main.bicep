// main.bicep — JJGNet Broadcasting infrastructure orchestrator
//
// Deploys:
//   - Application Insights + Log Analytics
//   - Key Vault
//   - Azure Storage (queues + tables)
//   - Azure SQL Server + JJGNet database
//   - App Service Plan + API App + Web App
//   - Azure Functions App
//   - Event Grid topics + subscriptions
//
// Usage:
//   az deployment group create \
//     --resource-group <rg> \
//     --template-file infra/main.bicep \
//     --parameters infra/parameters/prod.bicepparam

targetScope = 'resourceGroup'

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Short environment identifier (dev | prod). Used in resource names.')
@allowed(['dev', 'prod'])
param environmentName string = 'prod'

@description('Azure region for all resources. Matches existing infrastructure in West US 2.')
param location string = 'westus2'

@description('SQL Server administrator login name.')
param sqlAdminLogin string

@description('SQL Server administrator password. Passed as a secure parameter — never hardcoded.')
@secure()
param sqlAdminPassword string

@description('Object ID of the Azure AD principal (service principal or user) that becomes Key Vault Administrator.')
param adminPrincipalObjectId string

@description('Resource tags applied to every resource.')
param tags object = {
  project: 'jjgnet-broadcast'
  environment: environmentName
  managedBy: 'bicep'
}

// ── Modules ──────────────────────────────────────────────────────────────────

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    environmentName: environmentName
    tags: tags
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    environmentName: environmentName
    tags: tags
    adminPrincipalObjectId: adminPrincipalObjectId
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    environmentName: environmentName
    tags: tags
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    location: location
    environmentName: environmentName
    tags: tags
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
  }
}

module appService 'modules/app-service.bicep' = {
  name: 'appService'
  params: {
    location: location
    environmentName: environmentName
    tags: tags
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultUri: keyVault.outputs.keyVaultUri
  }
}

module functions 'modules/functions.bicep' = {
  name: 'functions'
  params: {
    location: location
    environmentName: environmentName
    tags: tags
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    appStorageConnectionString: storage.outputs.storageConnectionStringSecretRef
  }
}

module eventGrid 'modules/eventgrid.bicep' = {
  name: 'eventGrid'
  params: {
    location: location
    tags: tags
    functionAppName: functions.outputs.functionAppName
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────

output apiAppUrl string = 'https://${appService.outputs.apiAppHostname}'
output webAppUrl string = 'https://${appService.outputs.webAppHostname}'
output functionAppUrl string = 'https://${functions.outputs.functionAppHostname}'
output keyVaultUri string = keyVault.outputs.keyVaultUri
output storageAccountName string = storage.outputs.storageAccountName
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString
output newSourceDataTopicEndpoint string = eventGrid.outputs.newSourceDataTopicEndpoint
output scheduledItemFiredTopicEndpoint string = eventGrid.outputs.scheduledItemFiredTopicEndpoint
