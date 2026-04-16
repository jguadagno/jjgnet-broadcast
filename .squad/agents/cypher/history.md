# Cypher — History

## Core Context

**Role:** DevOps Engineer | CI/CD, GitHub Actions, Azure infrastructure, Bicep IaC, health monitoring

**Key infrastructure facts:**
- App Service names: `api-jjgnet-broadcast`, `web-jjgnet-broadcast`, `jjgnet-broadcast` (Functions)
- All 3 apps on same App Service plan (NOT Consumption) → App Service Health Check `/health` works for all
- Health endpoints (ServiceDefaults): `/health` (readiness) + `/alive` (liveness)
- Resource Group: `jjgnet` | Subscription: `4f42033c-3579-4a94-8023-a3561518ae7f`
- SQL Server: `r4bv7wtt6u` (westus) | DB: `JJGNet` | Key Vault: `jjgnet-broadcasting` (westus2)
- Storage (main): `jjgnet` (westus2, RAGRS) | Storage (functions): `jjgnetbeb6` (westus, LRS)
- App Insights/Log Analytics: `jjgnet`/`jjgnet-log-workspace` (westus2) | App Service Plan: `jjgnet-broadcast` (P1v3, westus)
- Managed Identities: `api-jjgnet-broad-id-8130`, `web-jjgnet-broad-id-8f0f`, `jjgnet-broadcast-id-8d7d`

**Bicep IaC patterns:**
- `targetScope = 'resourceGroup'` at main.bicep; modules under `infrastructure/bicep/modules/{compute,data,security,monitoring}/`
- NEVER `listKeys()` in module outputs (exposes in ARM history); `@secure()` on all connection string params
- NEVER `-preview` API versions in production
- GA versions: actionGroups `@2023-01-01`, components `@2020-02-02`, workspaces `@2022-10-01`, metricAlerts `@2018-03-01`, eventGrid `@2022-06-15`, sql/servers `@2021-11-01`, managedIdentities `@2023-01-31`
- `allowBlobPublicAccess: false`; `StorageV2` over legacy `Storage`; `ConnectionString` over deprecated `InstrumentationKey`
- Circular dependency fix: identify dead params (declared but not used in resources) and remove to break cycle
- Hardcode nothing — parameterise emails, notification targets; `event-grid.bicep` is in `modules/data/` not `modules/monitoring/`

**GitHub Actions patterns:**
- `environment: production` creates approval gate if GitHub Settings has required reviewers
- "Stop staging slot" step after swap: `az webapp stop --slot staging` / `az functionapp stop --slot staging`

**Completed work:**
- PR #557: Production approval gate + staging slot cleanup in all 3 workflows
- Issue #635: App Service Health Check + Availability Tests runbook posted  
- Issue #637: Full modular Bicep scaffold (PR #645); circular dependency fixed; preview APIs pinned to GA

**Team standing rules:** Only Joseph merges PRs; All mapping via AutoMapper; Paging at data layer only
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

## Learnings

### 2026-04-16: OTel Logging — Single Provider Rule
- `AddServiceDefaults()` (via `ServiceDefaults/Extensions.cs`) already registers the OTel logging provider — never call `loggingBuilder.AddOpenTelemetry()` again in `Program.cs`
- Never add `.WriteTo.OpenTelemetry()` to Serilog config — it creates a third export path and duplicates log volume
- Symptom: 2-3x duplicate log entries in Azure Monitor / OTLP pipelines
- Fix: removed `loggingBuilder.AddOpenTelemetry(...)` block from `Api/Program.cs`, removed `.WriteTo.OpenTelemetry()` from `Broadcasting.Serilog/LoggingExtensions.cs`, removed now-unused `Serilog.Sinks.OpenTelemetry` package and `using OpenTelemetry.Logs` directive
- Decision filed: `.squad/decisions/inbox/cypher-otel-logging.md`

