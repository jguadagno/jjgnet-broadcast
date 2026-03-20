# Link — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2025-07 | S8-328: Wire Application Insights in ServiceDefaults | PR #511 opened — `UseAzureMonitor()` uncommented in ServiceDefaults, package added, redundant calls removed from Api/Web/Functions |

## Learnings

### Application Insights / Azure Monitor wiring (S8-328)
- **ServiceDefaults was the gap**: `UseAzureMonitor()` was commented out in `ServiceDefaults/Extensions.cs` AND the `Azure.Monitor.OpenTelemetry.AspNetCore` package was missing from `ServiceDefaults.csproj`.
- **Api and Web had redundant unconditional calls**: Both `Program.cs` files called `services.AddOpenTelemetry().UseAzureMonitor()` unconditionally in their `ConfigureTelemetryAndLogging` — no connection string guard. ServiceDefaults owns this now with a proper `APPLICATIONINSIGHTS_CONNECTION_STRING` guard.
- **Functions uses a different pattern**: The isolated worker model uses `UseFunctionsWorkerDefaults()` (from `Microsoft.Azure.Functions.Worker.OpenTelemetry`) for worker-specific instrumentation. `UseAzureMonitorExporter()` was removed because ServiceDefaults' `UseAzureMonitor()` now handles the exporter centrally — including for Functions.
- **host.json** already had `telemetryMode: OpenTelemetry` — no change needed there.
- **Package versions**: Api and Web both referenced `Azure.Monitor.OpenTelemetry.AspNetCore` v1.4.0; ServiceDefaults now uses the same version for consistency.

