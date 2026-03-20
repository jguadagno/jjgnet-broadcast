# Decision Inbox: Application Insights / Azure Monitor Wiring (S8-328)

**From:** Link  
**Sprint:** Sprint 8  
**PR:** #511  
**Date:** 2025-07

---

## Findings

### What Was Wrong

`UseAzureMonitor()` was commented out in `ServiceDefaults/Extensions.cs` and the required NuGet package (`Azure.Monitor.OpenTelemetry.AspNetCore`) was absent from `ServiceDefaults.csproj`. In production, no traces, metrics, or logs were flowing to Application Insights.

### Inconsistency Found Across Services

| Service | Before | After |
|---------|--------|-------|
| ServiceDefaults | `UseAzureMonitor()` commented out, package missing | ✅ Uncommented, guarded by `APPLICATIONINSIGHTS_CONNECTION_STRING`, package added |
| Api | Unconditional `UseAzureMonitor()` in `ConfigureTelemetryAndLogging` (no env var guard) | ✅ Removed — ServiceDefaults handles it |
| Web | Same as Api — unconditional `UseAzureMonitor()` | ✅ Removed — ServiceDefaults handles it |
| Functions | `UseAzureMonitorExporter()` in telemetry setup | ✅ Removed — ServiceDefaults handles the exporter; `UseFunctionsWorkerDefaults()` retained |
| Functions host.json | `telemetryMode: OpenTelemetry` | ✅ Already correct — no change needed |

### Design Decision Made

**Centralize Azure Monitor registration in ServiceDefaults.** The conditional guard `if (!string.IsNullOrEmpty(APPLICATIONINSIGHTS_CONNECTION_STRING))` is the right pattern: it's a no-op locally (no env var set) and activates automatically in all Azure-deployed services.

### Risks / Notes

- **Double-registration was the prior state**: Api and Web were calling `UseAzureMonitor()` unconditionally AND ServiceDefaults was supposed to do it (once uncommented). OpenTelemetry's SDK is mostly idempotent here but this is now clean.
- **Functions worker model**: `UseAzureMonitor()` from the AspNetCore package works for isolated worker Functions too. `UseFunctionsWorkerDefaults()` adds the Functions-specific trace source — that's the only Functions-specific piece needed.
- **Package pinned at v1.4.0**: Matches what Api and Web already referenced. Should be reviewed against the latest stable release in a future sprint.

### Recommendation

In a future sprint: audit whether Api and Web still need `Azure.Monitor.OpenTelemetry.AspNetCore` as a direct package reference, since ServiceDefaults is now the only consumer and they'll get it transitively.
