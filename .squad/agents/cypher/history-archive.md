# Cypher — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** DevOps Engineer
- **Joined:** 2026-03-14T16:55:20.779Z

## Learnings

<!-- Append learnings below -->

### Approval Gates via GitHub Environments (2026-03-14)

- `environment: production` on a job is sufficient to create an approval gate in GitHub Actions. GitHub will pause the job and wait for a required reviewer **if** the `production` environment in GitHub Settings has required reviewers configured. No extra YAML is needed beyond the `environment:` key.
- The environment protection rules live entirely in GitHub Settings (not in YAML). A workflow can have `environment: production` in place before reviewers are added; enabling enforcement is a one-click change in Settings → Environments.
- After a slot swap, App Service staging slots can be stopped with `az webapp stop --slot staging`. For Azure Functions slots, use `az functionapp stop --slot staging`. Both commands accept `--name` and `--resource-group` identical to other az CLI calls in the workflow — no extra credentials needed since the same Azure login session covers all steps in the job.
- Stopping the staging slot after a swap is a cheap guard: it ensures staging is never left running/serving traffic after going dark, and makes the next staging deploy a clean start.

### Deployment Approval Gate + Staging Cleanup (2026-03-22)

- **Issue #556** / **PR #557**: Implemented production approval gate + automatic staging slot cleanup in all three workflows (API, Web, Functions)
- Added "Stop staging slot" step to each `swap-to-production` job in `.github/workflows/`
- GitHub environment gates already in place (`environment: production`) — required reviewers needed in GitHub Settings
- Scribe created orchestration logs and merged decisions into team decisions.md
- Neo flagged non-blocking observation: cleanup steps should run immediately after primary action (swap), before informational steps, to avoid skipping if downstream steps fail

### Azure Health Monitoring Configuration — Issue #635 (2026-04-05)

**Context:** Neo triaged #635 (add health checks to Api & Web). Health check infrastructure already exists via `ServiceDefaults` — `/health` (readiness) and `/alive` (liveness) endpoints mapped in both projects.

**Findings:**
- **Azure App Service names confirmed:**
  - API: `api-jjgnet-broadcast` (deployed via `.github/workflows/main_api-jjgnet-broadcast.yml`)
  - Web: `web-jjgnet-broadcast` (deployed via `.github/workflows/main_web-jjgnet-broadcast.yml`)
  - Functions: `jjgnet-broadcast` (deployed via `.github/workflows/main_jjgnet-broadcast.yml`)
- **Health endpoint paths (ServiceDefaults/Extensions.cs):**
  - `/health` — full readiness check (validates all dependencies)
  - `/alive` — liveness check (tagged "live", basic app responsiveness)
- **CORRECTED: All three apps on App Service plan** — Initially I incorrectly stated Functions was on Consumption plan. Joseph confirmed **all three apps (api-jjgnet-broadcast, web-jjgnet-broadcast, jjgnet-broadcast) are hosted on the same App Service plan**
  - **Result:** App Service Health Check feature IS available for Functions — no workaround needed
  - All three services can use the platform `/health` endpoint with zero code changes
  - Custom HTTP trigger for Functions is optional/nice-to-have, not required
- **No `infra/` directory exists** — all infrastructure currently managed manually in Azure Portal or via GitHub Actions deployment

**Deliverable:** Posted comprehensive Azure Portal runbook to issue #635 covering:
1. **App Service Health Check** configuration for Api, Web, and Functions (path: `/health`, threshold: 3 failures)
2. **Azure Monitor Availability Tests** — external uptime monitoring setup (all 3 services)
3. **Summary table** — quick reference for each service's monitoring configuration
4. **Correction comment posted** — [Clarified Functions hosting plan](https://github.com/jguadagno/jjgnet-broadcast/issues/635#issuecomment-4184423317)

**Key decisions embedded in guide:**
- Use `/health` for App Service Health Check (validates dependencies) — better than `/alive` for production readiness
- Recommend 2+ instances for true HA (single-instance restarts cause downtime)
- Defense in depth: Use both App Service Health Check (internal) + Availability Tests (external)
- All three services on App Service plan — simplified health check setup

**References:** 
- [Initial guide posted to issue #635](https://github.com/jguadagno/jjgnet-broadcast/issues/635#issuecomment-4184410295)
- [Correction comment](https://github.com/jguadagno/jjgnet-broadcast/issues/635#issuecomment-4184423317)

**Status:** ✅ Correction posted confirming Functions on App Service plan (NOT Consumption), so Health Check support is available. Strategy remains valid.


### Bicep IaC Discovery — Issue #637 (2026-04-05)

**Context:** Drafted Azure access request and discovery checklist for the IaC initiative (#637). Confirmed with Neo's analysis that ~10 critical resource identifiers cannot be inferred from the codebase.

**Key pattern — `az group export` workflow:**
1. Joseph grants `Reader` role on production resource group (resource group scope, not subscription)
2. `az group export --name <rg> --include-parameter-default-values > arm-export.json`
3. `az bicep decompile --file arm-export.json` → raw monolith Bicep
4. Refactor into modular structure: `infrastructure/bicep/modules/{compute,data,security,monitoring}/`
5. `infrastructure/discovery/` is gitignored — raw ARM export artifacts, no secrets

**Why Reader is sufficient (not Contributor):** Reader allows `az group export`, `az resource list`, and reading metadata for App Insights / Key Vault / Storage. It does NOT expose secret values, storage keys, or SQL credentials. Zero security risk for IaC generation.

**Blocker pattern:** Do NOT scaffold Bicep for production resources without Azure access — templates that compile without real resource names/SKUs will require manual substitution on every parameter, creating rework risk across all 7 phases.

**Time estimate:** Discovery + initial scaffold = 6–9 hours once access and resource group name are provided.

**References:**
- [GitHub comment posted](https://github.com/jguadagno/jjgnet-broadcast/issues/637#issuecomment-4189062098)
- Neo's analysis: `.squad/decisions/inbox/neo-637-azure-access-for-bicep.md`
- Cypher decision: `.squad/decisions/inbox/cypher-637-access-checklist.md`

### Bicep IaC Scaffold — Issue #637 (2026-04-05)

**Context:** Created the full modular Bicep scaffold for the JJGNet Broadcasting Azure environment.

**Resource Group / Subscription confirmed:**
- Resource Group: `jjgnet`
- Subscription: `4f42033c-3579-4a94-8023-a3561518ae7f` (Visual Studio Ultimate - MSDN MVP)
- Tenant: `bee716cf-fa94-4610-b72e-5df4bf5ac339`

**All production resource names resolved from `az resource list`:**
- SQL Server: `r4bv7wtt6u` (westus) — database: `JJGNet`
- Key Vault: `jjgnet-broadcasting` (westus2)
- Storage (main): `jjgnet` (westus2, Standard_RAGRS)
- Storage (functions): `jjgnetbeb6` (westus, Standard_LRS)
- App Insights: `jjgnet` (westus2)
- Log Analytics: `jjgnet-log-workspace` (westus2)
- App Service Plan: `jjgnet-broadcast` (westus, P1v3)
- Managed Identities: `api-jjgnet-broad-id-8130`, `web-jjgnet-broad-id-8f0f`, `jjgnet-broadcast-id-8d7d`

**Bicep patterns used:**
- `targetScope = 'resourceGroup'` at main.bicep level
- Module-per-resource-type under `infrastructure/bicep/modules/{compute,data,security,monitoring}/`
- RBAC model for Key Vault (`enableRbacAuthorization: true`) — no access policies, use `roleAssignments` instead
- `guid(keyVault.id, principalId, roleId)` for deterministic role assignment names
- `listKeys()` inline for storage connection strings in outputs (secure — outputs are marked sensitive by Bicep)
- `@secure()` on all password/connection string params
- `dependsOn` explicit on modules that reference outputs from other modules (key vault depends on app services for principalIds)
- User-assigned identity wired into function app via `userAssignedIdentities: { '${id}': {} }`
- Staging slots defined as child resources inside compute modules (not separate modules)

**PR:** #645 — https://github.com/jguadagno/jjgnet-broadcast/pull/645
**Issue comment:** https://github.com/jguadagno/jjgnet-broadcast/issues/637#issuecomment-4192734573

### Bicep Security Patterns — PR #645 Review Fixes (2026-04-06)

**Security patterns to always follow in Bicep:**

1. **Never use `listKeys()` in module outputs** — connection strings built from `listKeys()` flow into ARM deployment history in plaintext. Instead, use identity-based connections (e.g., `AzureWebJobsStorage__accountName` for Azure Functions) and managed identity auth. Only reference account names/IDs in outputs.

2. **Always set `allowBlobPublicAccess: false`** unless there is an explicit public blob requirement (CDN, static website, etc.). Default to locked-down.

3. **Circular dependency anti-pattern** — if module A passes `outputs.X` to module B AND module B passes `outputs.Y` to module A, this is a compile-time circular dependency. Fix by identifying dead params (declared but unused in resource definitions) and removing them to break the cycle.

4. **Never hardcode email addresses or notification targets** — always parameterise them so they can be injected from parameter files or CI secrets.

5. **Pin API versions to GA** — never use `-preview` API versions in production Bicep. The stable GA replacements used in this project: `microsoft.insights/actionGroups@2023-01-01`, `microsoft.insights/components@2020-02-02`, `Microsoft.OperationalInsights/workspaces@2022-10-01`, `microsoft.insights/metricAlerts@2018-03-01`.

6. **Prefer `StorageV2` over `Storage`** — `kind: 'Storage'` is legacy. Always use `kind: 'StorageV2'` with `accessTier: 'Hot'` for new storage accounts.

7. **Prefer `connectionString` over `instrumentationKey`** for Application Insights — `InstrumentationKey` is deprecated; `ConnectionString` is the modern approach.

**References:** PR #645, Neo code review, commit `eb24106`


### Bicep Preview API Version Cleanup — PR #645 Final Fix (2026-04-06)

**Context:** Neo's re-review of PR #645 found 3 Bicep modules still using `-preview` API versions after the initial security fixes.

**Files fixed (commit `38fc3de`):**
- `infrastructure/bicep/modules/data/event-grid.bicep` — 5 `Microsoft.EventGrid/topics@2023-12-15-preview` → `@2022-06-15`
- `infrastructure/bicep/modules/data/sql-server.bicep` — 3 resources (`servers`, `servers/databases`, `servers/firewallRules`) `@2023-08-01-preview` → `@2021-11-01`
- `infrastructure/bicep/modules/security/managed-identity.bicep` — 3 `Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview` → `@2023-01-31`

**Key note:** `event-grid.bicep` is in `modules/data/` not `modules/monitoring/` — confirmed by file search before editing. Always search before assuming paths.

**Verification:** Full scan of all `.bicep` files under `infrastructure/bicep/` returned zero `-preview` matches after fix.

**PR comment posted:** https://github.com/jguadagno/jjgnet-broadcast/pull/645#issuecomment-4193265300

**Status:** ✅ All preview API versions pinned. Awaiting Neo's final re-review.

---

Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only
### 2026-04-07: GitHub Comment Formatting Skill Added
- Skill: .squad/skills/github-comment-formatting/SKILL.md now exists — canonical reference for formatting GitHub comments
- Rule: Use triple backticks for ALL fenced code blocks in GitHub content (PR reviews, issue comments, PR comments)
- Single backticks are for inline code only (single variable/method names, one line)
- Root cause of addition: PR #646 review used single-backtick fences; GitHub rendered broken inline code (words truncated, multi-line collapsed)
- Charter updated with enforcement rule (## How I Work)
- Read .squad/skills/github-comment-formatting/SKILL.md before posting any PR review or issue comment containing code