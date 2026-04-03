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


## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only