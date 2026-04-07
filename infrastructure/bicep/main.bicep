targetScope = 'resourceGroup'

// =============================================================================
// JJGNet Broadcasting — Azure Infrastructure Orchestrator
// Resource Group : jjgnet
// Subscription   : 4f42033c-3579-4a94-8023-a3561518ae7f
// =============================================================================

@description('Primary Azure region for most resources (App Services, SQL, Functions).')
param locationPrimary string = 'westus'

@description('Secondary Azure region for storage, monitoring, EventGrid, and Key Vault.')
param locationSecondary string = 'westus2'

@description('Environment identifier (prod, staging).')
@allowed(['prod', 'staging'])
param environment string = 'prod'

@description('Resource tags applied to all resources.')
param tags object = {
  environment: environment
  project: 'jjgnet-broadcasting'
  managedBy: 'bicep'
}

// --- SQL Server parameters ---
@description('SQL Server name.')
param sqlServerName string

@description('SQL administrator login name.')
param sqlAdminLogin string

@description('SQL administrator login password.')
@secure()
param sqlAdminPassword string

// --- Key Vault parameters ---
@description('Key Vault name.')
param keyVaultName string

// --- Storage parameters ---
@description('Main storage account name (queues, tables, blobs).')
param storageAccountName string

@description('Functions-dedicated storage account name.')
param functionsStorageAccountName string

// --- Monitoring parameters ---
@description('Log Analytics workspace name.')
param logAnalyticsWorkspaceName string = 'jjgnet-log-workspace'

@description('Application Insights component name.')
param appInsightsName string = 'jjgnet'

@description('Action group name.')
param actionGroupName string = 'jjgnet_broadcasting'

@description('Email address for alert notifications.')
param alertEmailAddress string

// =============================================================================
// Modules
// =============================================================================

module logAnalytics 'modules/monitoring/log-analytics.bicep' = {
  name: 'deploy-log-analytics'
  params: {
    location: locationSecondary
    workspaceName: logAnalyticsWorkspaceName
    retentionInDays: 30
    sku: 'PerGB2018'
    tags: tags
  }
}

module appInsights 'modules/monitoring/app-insights.bicep' = {
  name: 'deploy-app-insights'
  params: {
    location: locationSecondary
    appInsightsName: appInsightsName
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
  }
}

module actionGroup 'modules/monitoring/action-group.bicep' = {
  name: 'deploy-action-group'
  params: {
    actionGroupName: actionGroupName
    groupShortName: 'jjgnet'
    emailReceivers: [
      {
        name: 'Notify Joe_-EmailAction-'
        emailAddress: alertEmailAddress
        useCommonAlertSchema: true
      }
    ]
    tags: tags
  }
}

module storage 'modules/data/storage-account.bicep' = {
  name: 'deploy-storage'
  params: {
    location: locationSecondary
    storageAccountName: storageAccountName
    storageSkuName: 'Standard_RAGRS'
    functionsStorageAccountName: functionsStorageAccountName
    functionsStorageSkuName: 'Standard_LRS'
    tags: tags
  }
}

module sqlServer 'modules/data/sql-server.bicep' = {
  name: 'deploy-sql-server'
  params: {
    location: locationPrimary
    sqlServerName: sqlServerName
    databaseName: 'JJGNet'
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    tags: tags
  }
}

module eventGrid 'modules/data/event-grid.bicep' = {
  name: 'deploy-event-grid'
  params: {
    location: locationSecondary
    tags: tags
  }
}

module keyVault 'modules/security/key-vault.bicep' = {
  name: 'deploy-key-vault'
  params: {
    location: locationSecondary
    keyVaultName: keyVaultName
    tenantId: tenant().tenantId
    skuName: 'standard'
    secretReaderPrincipalIds: [
      appServices.outputs.apiAppPrincipalId
      appServices.outputs.webAppPrincipalId
      functionApp.outputs.functionAppPrincipalId
    ]
    tags: tags
  }
  dependsOn: [
    appServices
    functionApp
  ]
}

module appServices 'modules/compute/app-service.bicep' = {
  name: 'deploy-app-services'
  params: {
    location: locationPrimary
    appServicePlanName: 'jjgnet-broadcast'
    appServicePlanSku: {
      name: 'P1v3'
      tier: 'PremiumV3'
      size: 'P1v3'
      family: 'Pv3'
      capacity: 1
    }
    apiAppName: 'api-jjgnet-broadcast'
    webAppName: 'web-jjgnet-broadcast'
    appInsightsConnectionString: appInsights.outputs.connectionString
    tags: tags
  }
  dependsOn: [
    appInsights
  ]
}

module functionApp 'modules/compute/function-app.bicep' = {
  name: 'deploy-function-app'
  params: {
    location: locationPrimary
    functionAppName: 'jjgnet-broadcast'
    appServicePlanId: appServices.outputs.appServicePlanId
    functionsStorageAccountName: functionsStorageAccountName
    appInsightsConnectionString: appInsights.outputs.connectionString
    tags: tags
  }
  dependsOn: [
    appServices
    storage
    appInsights
  ]
}

module alertRules 'modules/monitoring/alert-rules.bicep' = {
  name: 'deploy-alert-rules'
  params: {
    actionGroupId: actionGroup.outputs.actionGroupId
    appInsightsId: appInsights.outputs.appInsightsId
    tags: tags
  }
  dependsOn: [
    actionGroup
    appInsights
    appServices
    functionApp
  ]
}

// =============================================================================
// Outputs
// =============================================================================

output resourceGroupName string = resourceGroup().name
output apiAppUrl string = 'https://${appServices.outputs.apiAppDefaultHostname}'
output webAppUrl string = 'https://${appServices.outputs.webAppDefaultHostname}'
output functionAppUrl string = 'https://${functionApp.outputs.functionAppDefaultHostname}'
output keyVaultUri string = keyVault.outputs.keyVaultUri
output sqlServerFqdn string = sqlServer.outputs.sqlServerFqdn
output appInsightsConnectionString string = appInsights.outputs.connectionString
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId
