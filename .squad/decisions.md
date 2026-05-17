# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.

---

## Decision: Azure Functions Stable Port via .WithHttpEndpoint() in AppHost

**Date:** 2026-05-16  
**Author:** Cypher (DevOps Engineer)  
**Requested by:** Joseph Guadagno  
**Status:** Implemented

### Context

The Azure Functions project (JosephGuadagno.Broadcasting.Functions) had unstable port assignment
in local Aspire environments. Aspire assigned a random proxy port to the Functions resource on every
run, making it impossible to predict the local endpoint without inspecting the Aspire dashboard.

**Initial Approach Attempted:** Add Properties/launchSettings.json with a fixed pplicationUrl.
**Outcome:** Failed. launchSettings.json does NOT work for Azure Functions isolated worker model
in Aspire. The launch settings are ignored by the Functions host.

### Decision (Final)

**Delete** src/JosephGuadagno.Broadcasting.Functions/Properties/launchSettings.json
and any Properties/ directory if empty.

**Add** .WithHttpEndpoint(port: 7071, isProxied: false) to the Functions resource in AppHost.cs.

Port 7071 is the Azure Functions conventional default and does not conflict with any other
project in the solution:

| Project    | HTTP port | HTTPS port |
|------------|-----------|------------|
| API        | 5272      | 7272       |
| Web        | 5224      | 7224       |
| AppHost    | 15061     | 17282      |
| **Functions** | **7071** | *(none)*  |

### Rationale

launchSettings.json is a Visual Studio-specific launch configuration and is ignored by the
Azure Functions isolated worker model when running under Aspire orchestration. The correct
pattern for Aspire is to use the resource builder API in AppHost.cs.

The .WithHttpEndpoint(port: 7071, isProxied: false) configuration:
- Sets the Functions resource to bind directly to port 7071 (not through Aspire's reverse proxy)
- isProxied: false is the correct pattern for Azure Functions — the Functions host manages
  its own HTTP binding, bypassing Aspire's proxy layer
- Ensures stable port across all local runs

### Files Changed

- **Deleted:** src/JosephGuadagno.Broadcasting.Functions/Properties/launchSettings.json
- **Modified:** src/JosephGuadagno.Broadcasting.AppHost/AppHost.cs
  - Added .WithHttpEndpoint(port: 7071, isProxied: false) to Functions resource registration

### Verification

dotnet build .\src\ --no-restore --configuration Release — Build succeeded, 0 errors.
Functions resource now consistently binds to http://localhost:7071 across all local Aspire runs.

