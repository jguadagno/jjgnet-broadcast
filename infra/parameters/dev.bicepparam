// dev.bicepparam — Development environment parameters for JJGNet Broadcasting infrastructure
//
// Usage:
//   az deployment group create \
//     --resource-group rg-jjgnet-dev \
//     --template-file infra/main.bicep \
//     --parameters infra/parameters/dev.bicepparam

using '../main.bicep'

param environmentName = 'dev'
param location = 'westus2'

// SQL credentials: stored in GitHub Actions secrets / Key Vault — never hardcoded here.
// Set via --parameters sqlAdminLogin=... sqlAdminPassword=... at deploy time,
// or through a CI/CD secret reference.
param sqlAdminLogin = 'jjgnetadmin'

// sqlAdminPassword is @secure() — must be supplied at deploy time.
// Example: az deployment group create ... --parameters sqlAdminPassword=$SQL_ADMIN_PASSWORD

// Object ID of the service principal or user that should be Key Vault Administrator.
// Find yours with: az ad signed-in-user show --query id -o tsv
param adminPrincipalObjectId = '00000000-0000-0000-0000-000000000000'  // replace before deploying

param tags = {
  project: 'jjgnet-broadcast'
  environment: 'dev'
  managedBy: 'bicep'
}
