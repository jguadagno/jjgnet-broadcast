# JJGNet Broadcasting — Bicep Infrastructure

Modular Bicep IaC templates for the JJGNet Broadcasting Azure environment.

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) ≥ 2.40
- [Bicep CLI](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/install) ≥ 0.20 (`az bicep install`)
- Contributor or Owner role on resource group `jjgnet`
- Subscription: `4f42033c-3579-4a94-8023-a3561518ae7f`

## Structure

```
infrastructure/
├── bicep/
│   ├── main.bicep                          # Orchestrator — wires all modules
│   ├── main.parameters.prod.bicepparam     # Production parameter values
│   ├── main.parameters.staging.bicepparam  # Staging parameter values
│   └── modules/
│       ├── compute/
│       │   ├── app-service.bicep           # App Service Plan + API + Web sites (+ staging slots)
│       │   └── function-app.bicep          # Azure Functions site (+ staging slot)
│       ├── data/
│       │   ├── sql-server.bicep            # SQL Server + JJGNet database + firewall rules
│       │   ├── storage-account.bicep       # Storage accounts, queues, and tables
│       │   └── event-grid.bicep            # EventGrid topics
│       ├── security/
│       │   ├── key-vault.bicep             # Key Vault + RBAC secret reader assignments
│       │   └── managed-identity.bicep      # User-assigned managed identities
│       └── monitoring/
│           ├── log-analytics.bicep         # Log Analytics workspace
│           ├── app-insights.bicep          # Application Insights (workspace-based)
│           ├── action-group.bicep          # Alert action group (email)
│           └── alert-rules.bicep           # Smart Detector failure anomaly alerts
└── discovery/                              # gitignored — raw ARM export artifacts
    └── .gitkeep
```

## Deploying

### 1. Login and set subscription

```bash
az login
az account set --subscription 4f42033c-3579-4a94-8023-a3561518ae7f
```

### 2. Validate the template

```bash
az deployment group validate \
  --resource-group jjgnet \
  --template-file infrastructure/bicep/main.bicep \
  --parameters infrastructure/bicep/main.parameters.prod.bicepparam \
  --parameters sqlAdminPassword="<your-password>"
```

### 3. Preview changes (what-if)

```bash
az deployment group what-if \
  --resource-group jjgnet \
  --template-file infrastructure/bicep/main.bicep \
  --parameters infrastructure/bicep/main.parameters.prod.bicepparam \
  --parameters sqlAdminPassword="<your-password>"
```

### 4. Deploy

```bash
az deployment group create \
  --resource-group jjgnet \
  --template-file infrastructure/bicep/main.bicep \
  --parameters infrastructure/bicep/main.parameters.prod.bicepparam \
  --parameters sqlAdminPassword="<your-password>" \
  --name "jjgnet-deploy-$(date +%Y%m%d-%H%M%S)"
```

> **Note:** Never pass `sqlAdminPassword` on the command line in CI. Use a Key Vault reference or inject it from a GitHub Actions secret.

## Parameter Files

| File | Purpose |
|------|---------|
| `main.parameters.prod.bicepparam` | Production — `jjgnet` resource group, `westus`/`westus2` |
| `main.parameters.staging.bicepparam` | Staging — same resource group, `staging` environment tag |

Parameters marked `// TODO` require values that must be obtained from Azure (e.g. `sqlAdminPassword`) or are intentionally left for the operator to supply.

## Phased Rollout (Issue #637)

Deployments are broken into phases to reduce blast radius:

| Phase | Scope | Modules |
|-------|-------|---------|
| 1 | Monitoring foundation | `log-analytics`, `app-insights`, `action-group` |
| 2 | Identity | `managed-identity` |
| 3 | Security | `key-vault` |
| 4 | Data | `sql-server`, `storage-account` |
| 5 | Compute | `app-service`, `function-app` |
| 6 | Networking/EventGrid | `event-grid` |
| 7 | Alerts | `alert-rules` |

To deploy a single phase (example — Phase 1):

```bash
az deployment group create \
  --resource-group jjgnet \
  --template-file infrastructure/bicep/modules/monitoring/log-analytics.bicep \
  --parameters location=westus2 workspaceName=jjgnet-log-workspace
```

## Discovery

Raw ARM export artifacts live in `infrastructure/discovery/` and are gitignored to prevent secrets leaking. To regenerate:

```bash
az group export \
  --name jjgnet \
  --subscription 4f42033c-3579-4a94-8023-a3561518ae7f \
  --include-parameter-default-values \
  > infrastructure/discovery/arm-export.json

az bicep decompile --file infrastructure/discovery/arm-export.json
```

## Known Resource Names (Production)

| Resource | Name | Location |
|----------|------|----------|
| Resource Group | `jjgnet` | — |
| App Service Plan | `jjgnet-broadcast` | westus |
| API App Service | `api-jjgnet-broadcast` | westus |
| Web App Service | `web-jjgnet-broadcast` | westus |
| Azure Functions | `jjgnet-broadcast` | westus |
| SQL Server | `r4bv7wtt6u` | westus |
| SQL Database | `JJGNet` | westus |
| Storage Account (main) | `jjgnet` | westus2 |
| Storage Account (funcs) | `jjgnetbeb6` | westus |
| Key Vault | `jjgnet-broadcasting` | westus2 |
| App Insights | `jjgnet` | westus2 |
| Log Analytics Workspace | `jjgnet-log-workspace` | westus2 |
| Action Group | `jjgnet_broadcasting` | global |
