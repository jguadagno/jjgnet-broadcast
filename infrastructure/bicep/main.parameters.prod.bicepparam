using './main.bicep'

// =============================================================================
// JJGNet Broadcasting — Production Parameters
// Subscription : 4f42033c-3579-4a94-8023-a3561518ae7f
// Resource Group: jjgnet
// Populated from: az group export + az resource list (2025-04-05)
// =============================================================================

param environment = 'prod'

param locationPrimary = 'westus'
param locationSecondary = 'westus2'

// --- Monitoring (fully populated from discovery) ---
param logAnalyticsWorkspaceName = 'jjgnet-log-workspace'
param appInsightsName = 'jjgnet'
param actionGroupName = 'jjgnet_broadcasting'
param alertEmailAddress = 'jguadagno@hotmail.com'

// --- SQL Server (fully populated from discovery) ---
param sqlServerName = 'r4bv7wtt6u'
param sqlAdminLogin = 'jguadagno'
param sqlAdminPassword = '' // TODO: inject from Key Vault or CI secret — never commit

// --- Key Vault (fully populated from discovery) ---
param keyVaultName = 'jjgnet-broadcasting'

// --- Storage (fully populated from discovery) ---
param storageAccountName = 'jjgnet'
param functionsStorageAccountName = 'jjgnetbeb6'

// --- Tags ---
param tags = {
  environment: 'prod'
  project: 'jjgnet-broadcasting'
  managedBy: 'bicep'
}
