# Link — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2025-07-14 | Fix PR #511 CI — merge main into `feature/s8-328-wire-application-insights` to pick up PR #513 test renames | ✅ Clean merge, pushed successfully. Also resolved workflow conflict in `feature/s8-315-api-dtos` stash pop (kept origin/main Critical-only vuln gate). |
| 2025-07 | S8-328: Wire Application Insights in ServiceDefaults | PR #511 opened — `UseAzureMonitor()` uncommented in ServiceDefaults, package added, redundant calls removed from Api/Web/Functions |

## Learnings

- **Branch-behind-main CI failures**: When a PR branch pre-dates a merge to main that renames symbols (controllers, methods), the CI merge commit will fail to compile. Fix is always `git merge origin/main --no-edit` on the feature branch — no rebase needed when the changes are non-overlapping.
- **Stash pop conflicts in shared workflow files**: Workflow files (`.github/workflows/*.yml`) change frequently across branches. When popping a stash onto a branch that has received main updates, expect conflicts in workflow steps. Always favour the `origin/main` version of vuln-scan logic since those decisions are made intentionally and tracked in PRs.
- **Stash hygiene**: After a conflicted stash pop, git leaves the stash entry in the list. Always `git stash drop stash@{N}` after manually committing the resolution.

### Application Insights / Azure Monitor wiring (S8-328)
- **ServiceDefaults was the gap**: `UseAzureMonitor()` was commented out in `ServiceDefaults/Extensions.cs` AND the `Azure.Monitor.OpenTelemetry.AspNetCore` package was missing from `ServiceDefaults.csproj`.
- **Api and Web had redundant unconditional calls**: Both `Program.cs` files called `services.AddOpenTelemetry().UseAzureMonitor()` unconditionally in their `ConfigureTelemetryAndLogging` — no connection string guard. ServiceDefaults owns this now with a proper `APPLICATIONINSIGHTS_CONNECTION_STRING` guard.
- **Functions uses a different pattern**: The isolated worker model uses `UseFunctionsWorkerDefaults()` (from `Microsoft.Azure.Functions.Worker.OpenTelemetry`) for worker-specific instrumentation. `UseAzureMonitorExporter()` was removed because ServiceDefaults' `UseAzureMonitor()` now handles the exporter centrally — including for Functions.
- **host.json** already had `telemetryMode: OpenTelemetry` — no change needed there.
- **Package versions**: Api and Web both referenced `Azure.Monitor.OpenTelemetry.AspNetCore` v1.4.0; ServiceDefaults now uses the same version for consistency.
