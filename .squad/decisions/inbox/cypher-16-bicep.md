# Cypher: Bicep Infrastructure-as-Code (Issue #16)

**Date:** 2026-03-16  
**Author:** Cypher (DevOps Engineer)  
**Branch:** `issue-16`  
**PR:** `feat(#16): Bicep infrastructure-as-code`

---

## Summary

Replaced manual Azure portal steps with reproducible Bicep templates that provision the full
JJGNet Broadcasting infrastructure. Every resource documented in `infrastructure-needs.md` is now
covered by a parameterized, secret-free Bicep module.

---

## Files Created

| File | Purpose |
|------|---------|
| `infra/main.bicep` | Orchestrator — calls all modules, exposes outputs |
| `infra/modules/monitoring.bicep` | Log Analytics Workspace + Application Insights |
| `infra/modules/keyvault.bicep` | Azure Key Vault (RBAC mode, soft-delete enabled) |
| `infra/modules/storage.bicep` | App Storage Account — all queues + tables from infra doc |
| `infra/modules/sql.bicep` | Azure SQL Server (12.0) + JJGNet database (S1 Standard) |
| `infra/modules/app-service.bicep` | App Service Plan (P1v2) + `api-jjgnet-broadcast` + `web-jjgnet-broadcast` |
| `infra/modules/functions.bicep` | `jjgnet-broadcast` Functions App (Consumption) + dedicated runtime storage |
| `infra/modules/eventgrid.bicep` | `new-source-data` + `scheduled-item-fired` topics + all 6 subscriptions |
| `infra/parameters/dev.bicepparam` | Dev environment parameters |
| `infra/parameters/prod.bicepparam` | Prod environment parameters |
| `infra/deploy.ps1` | Local deployment script (PowerShell) |
| `.github/workflows/infra-deploy.yml` | Manual `workflow_dispatch` CI/CD workflow |

---

## Key Design Decisions

### 1. One resource type per module
Each module file contains a single logical resource group (e.g. storage only, SQL only). This
keeps modules reusable and independently testable with `az bicep build`.

### 2. Secrets are never hardcoded
- `sqlAdminPassword` is marked `@secure()` — never stored in parameter files or logs.
- Storage connection strings are computed with `listKeys()` inside modules and passed as
  `@secure()` between modules.
- App Settings that need social media tokens / API keys use Key Vault references at runtime:
  `@Microsoft.KeyVault(VaultName=...;SecretName=...)` — placeholders documented inline.
- CI/CD reads `INFRA_SQL_ADMIN_PASSWORD` from GitHub repository secrets.

### 3. Key Vault uses RBAC (not Access Policies)
`enableRbacAuthorization: true` is the modern, recommended approach. The admin SP and all
managed identities are granted roles via `Microsoft.Authorization/roleAssignments` resources
in the Bicep, keeping permission grants in code.

### 4. Two storage accounts in the Functions module
Azure Functions requires its own storage account for the host runtime
(`AzureWebJobsStorage`, `WEBSITE_CONTENTAZUREFILECONNECTIONSTRING`). A second dedicated
account (`stfnjjgnet<env>`) is created to isolate runtime state from application data
(queues, tables) in `stjjgnet<env>`.

### 5. App Service Plan matches infrastructure-needs.md
`P1v2` (PremiumV2) on Windows, US West 2 — exactly what the doc specifies. Both
`api-jjgnet-broadcast` and `web-jjgnet-broadcast` share the plan.

### 6. Event Grid subscriptions use system key
The `eventgrid_extension` system key is retrieved at deployment time via `listKeys()` and
embedded in the Function webhook URLs. This is the standard Azure pattern for Event Grid →
Functions integration. The subscriptions depend on the Functions module completing first.

### 7. `workflow_dispatch` only for infra CI/CD
Infrastructure changes are intentional and require human review. The `infra-deploy.yml`
workflow uses `workflow_dispatch` with environment/resource-group inputs. Automatic
deployment on `push: main` is explicitly excluded to prevent accidental infrastructure churn
from application code merges.

### 8. SQL Standard S1
The database SKU uses S1 (Standard, 20 DTUs) — a cost-effective baseline for production.
Scale up via `az sql db update` or re-deploy with a different SKU without recreating the DB.

---

## How to Deploy

```powershell
# Local
.\infra\deploy.ps1 -Environment prod -ResourceGroup rg-jjgnet-prod -SqlAdminPassword (Read-Host -AsSecureString)

# CI/CD
# Trigger the "Deploy Infrastructure (Bicep)" workflow in GitHub Actions.
# Required secrets: INFRA_CLIENT_ID, INFRA_TENANT_ID, INFRA_SUBSCRIPTION_ID,
#                   INFRA_SQL_ADMIN_PASSWORD, INFRA_ADMIN_PRINCIPAL_ID
```

---

## Post-Deploy Steps (Manual)

1. Update `adminPrincipalObjectId` in parameter files with the real SP/user object ID.
2. Populate Key Vault secrets for social API keys (Twitter, Facebook, LinkedIn, Bluesky).
3. Update App Service and Functions app settings with KV reference strings.
4. Run database schema scripts from `scripts/database/` against the new SQL database.
