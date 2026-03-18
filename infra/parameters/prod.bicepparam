// prod.bicepparam — Production environment parameters for JJGNet Broadcasting infrastructure
//
// Usage (manual):
//   az deployment group create \
//     --resource-group rg-jjgnet-prod \
//     --template-file infra/main.bicep \
//     --parameters infra/parameters/prod.bicepparam \
//     --parameters sqlAdminPassword=$SQL_ADMIN_PASSWORD
//
// Usage (CI/CD):
//   See .github/workflows/infra-deploy.yml

using '../main.bicep'

param environmentName = 'prod'
param location = 'westus2'

// SQL admin login — the password is supplied securely at deploy time (never stored here).
param sqlAdminLogin = 'jjgnetadmin'

// sqlAdminPassword is @secure() — must be supplied at deploy time via:
//   --parameters sqlAdminPassword=$SQL_ADMIN_PASSWORD
// In GitHub Actions this is read from the INFRA_SQL_ADMIN_PASSWORD repository secret.

// Object ID of the service principal used for Key Vault admin access.
// In CI/CD this is the managed identity / service principal running the deployment.
// Find yours with: az ad sp show --id <client-id> --query id -o tsv
param adminPrincipalObjectId = '00000000-0000-0000-0000-000000000000'  // replace before deploying

param tags = {
  project: 'jjgnet-broadcast'
  environment: 'prod'
  managedBy: 'bicep'
}
