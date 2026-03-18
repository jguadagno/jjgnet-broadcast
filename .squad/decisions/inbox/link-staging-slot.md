# Decision: Staging Deployment Slots and Production Approval Gate (S4-6)

**Date:** 2025-07-15
**Authors:** Link (Platform & DevOps) + Cypher (DevOps Engineer)
**Branch:** `feature/s4-6-staging-slot`
**Ticket:** S4-6

---

## Problem

Every merge to `main` deployed directly to production with no approval gate. One bad merge could break live broadcasting across all social platforms (Twitter, Facebook, LinkedIn, Bluesky).

---

## Pulumi Resources Added (`eng/infra/JjgnetStack.cs`)

### App Service Plan

| Resource | Type | Tier | Purpose |
|---|---|---|---|
| `plan-web` | AppServicePlan | P1v3 (PremiumV3) | Single shared plan for API, Web, and Functions apps. P1v3 natively supports deployment slots — no plan upgrade required. |

> **Note:** All three apps (`api-jjgnet-broadcast`, `web-jjgnet-broadcast`, `jjgnet-broadcast`) share the existing `jjgnet` App Service plan (P1v3, US West 2), consistent with `infrastructure-needs.md`. No separate plan for Functions is needed.

### New Web App Resources

| Resource | Pulumi Name | Azure Name | Notes |
|---|---|---|---|
| `WebApp` | `api-jjgnet-broadcast` | `api-jjgnet-broadcast` | API App Service, P1v3, `ASPNETCORE_ENVIRONMENT=Production` |
| `WebAppSlot` | `api-staging` | `api-jjgnet-broadcast/staging` | Staging slot, `ASPNETCORE_ENVIRONMENT=Staging` |
| `WebApp` | `web-jjgnet-broadcast` | `web-jjgnet-broadcast` | Web App Service, P1v3, `ASPNETCORE_ENVIRONMENT=Production` |
| `WebAppSlot` | `web-staging` | `web-jjgnet-broadcast/staging` | Staging slot, `ASPNETCORE_ENVIRONMENT=Staging` |
| `WebApp` | `jjgnet-broadcast` | `jjgnet-broadcast` | Functions App, P1v3 (shared plan), `AZURE_FUNCTIONS_ENVIRONMENT=Production` |
| `WebAppSlot` | `functions-staging` | `jjgnet-broadcast/staging` | Staging slot, `AZURE_FUNCTIONS_ENVIRONMENT=Staging` |

### Also Fixed

- `FUNCTIONS_EXTENSION_VERSION` corrected from `~3` → `~4` (infrastructure drift per Link's charter).
- `eng/infra/jjgnet.csproj` target framework updated from `netcoreapp3.1` → `net8.0` (Pulumi.AzureNative 1.x requires net6.0+; previous TFM caused restore failure).
- Pulumi stack now exports `ResourceGroupName` as a stack output, enabling CI/CD to resolve the RG without hardcoding it.

---

## GitHub Actions Workflow Changes

All three workflows (`.github/workflows/`) now follow the same three-job pattern:

```
build → deploy-to-staging → swap-to-production
```

### Job: `deploy-to-staging`
- Runs immediately after `build` — no approval gate here.
- Deploys artifact to the `staging` slot using `azure/webapps-deploy@v3` (API/Web) or `Azure/functions-action@v1` (Functions) with `slot-name: staging`.
- Uses the same OIDC credentials as before (`*_CLIENT_ID`, `*_TENANT_ID`, `*_SUBSCRIPTION_ID`).

### Job: `swap-to-production`
- Depends on `deploy-to-staging`.
- Declares `environment: production` — this is the **approval gate**. GitHub will pause here and wait for a required reviewer to approve before continuing.
- On approval, runs `az webapp deployment slot swap` (API/Web) or `az functionapp deployment slot swap` (Functions) to atomically promote staging → production.
- No redeploy: the already-validated artifact in the staging slot is swapped in.

### Also Fixed (Functions workflow)
- Removed `environment: production` from the `build-and-test` job (it was incorrectly placed on the build step, not just the deploy step).

---

## GitHub Environment Setup — Required Manual Steps

The `production` environment **must be configured in GitHub repository settings** before the approval gate will work. GitHub Actions YAML can *reference* an environment by name, but it cannot *create* the environment or its protection rules.

### Steps (GitHub UI → Repository → Settings → Environments):

1. **Create environment**: Click **New environment**, name it `production`.
2. **Add required reviewers**: Under *Protection rules*, enable *Required reviewers* and add the repo owner (e.g., `@jguadagno`) and any other approvers.
3. **Optionally set a deployment branch rule**: Restrict to `main` branch only.
4. **Add the `AZURE_RESOURCE_GROUP` secret**: Under the `production` environment secrets (or as a repository-level secret), add `AZURE_RESOURCE_GROUP` = the Pulumi-provisioned resource group name (e.g., `rg-jjgnet-prod`). All three workflows use this secret in the slot swap `az` command.

---

## Slot Swap Strategy

We use **Azure's atomic slot swap** mechanism:

1. Code is deployed to `staging` slot (warm-up happens there).
2. After approval, Azure swaps the routing — `staging` becomes `production` and vice versa.
3. The old production is now in `staging` and can be swapped back instantly if needed (**zero-downtime rollback**).

Slot-sticky settings (`ASPNETCORE_ENVIRONMENT`, `AZURE_FUNCTIONS_ENVIRONMENT`) stay with their respective slot and do NOT travel with the code during swaps. Production always gets `Production`; staging always gets `Staging`.

---

## OIDC Credential Compatibility

Existing OIDC federated credentials continue to work. Staging slot deployments and slot swap commands operate under the same subscription-level service principal. No new App Registrations required — Ghost confirmation not needed for this change.

---

## Limitations and Follow-Up

| Item | Detail |
|---|---|
| **Existing resources** | API and Web apps (`api-jjgnet-broadcast`, `web-jjgnet-broadcast`) were likely created manually outside Pulumi. Before running `pulumi up`, import them: `pulumi import azure-native:web:WebApp api-jjgnet-broadcast /subscriptions/.../resourceGroups/.../providers/Microsoft.Web/sites/api-jjgnet-broadcast`. |
| **Staging slot warm-up** | No custom warm-up rules configured. Consider adding `applicationInitialization` in `SiteConfig` for healthcheck-based warm-up before swap. |
| **Staging secrets** | Staging slots share Key Vault references but point to production secrets. A separate Key Vault staging policy or separate secrets may be needed if staging must use different credentials. Coordinate with Ghost. |
| **`AZURE_RESOURCE_GROUP` secret** | Must be added to GitHub — either as a repo-level secret or environment-level secret on `production`. Value = the Pulumi resource group name. |
