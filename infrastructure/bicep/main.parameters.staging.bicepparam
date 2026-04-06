using './main.bicep'

// =============================================================================
// JJGNet Broadcasting — Staging Parameters (stub)
// Create a separate resource group (e.g. jjgnet-staging) before deploying.
// =============================================================================

param environment = 'staging'

param locationPrimary = 'westus'
param locationSecondary = 'westus2'

// --- Monitoring ---
param logAnalyticsWorkspaceName = 'jjgnet-log-workspace-staging'
param appInsightsName = 'jjgnet-staging'
param actionGroupName = 'jjgnet_broadcasting_staging'

// --- SQL Server ---
param sqlServerName = 'jjgnet-sql-staging' // TODO: choose staging server name
param sqlAdminLogin = 'jguadagno'
param sqlAdminPassword = '' // TODO: inject from CI secret

// --- Key Vault ---
param keyVaultName = 'jjgnet-broadcasting-stg' // TODO: confirm or provision staging vault

// --- Storage ---
param storageAccountName = 'jjgnetstg' // TODO: confirm or provision staging storage
param functionsStorageAccountName = 'jjgnetfuncstg' // TODO: confirm or provision

// --- Tags ---
param tags = {
  environment: 'staging'
  project: 'jjgnet-broadcasting'
  managedBy: 'bicep'
}
