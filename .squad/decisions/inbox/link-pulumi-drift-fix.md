# Link: Pulumi Infrastructure Drift Fix (S4-2)

**Date:** 2025-07-11  
**Author:** Link (Platform & DevOps Engineer)  
**Branch:** `feature/s4-2-pulumi-drift-fix`  
**File changed:** `eng/infra/JjgnetStack.cs`

---

## Summary

Full audit of `JjgnetStack.cs` against the live project configuration revealed four drift issues. All four were corrected in-place.

---

## Changes Made

### 1. `FUNCTIONS_EXTENSION_VERSION`: `~3` → `~4`

**Why:** The Functions `.csproj` declares `<AzureFunctionsVersion>v4</AzureFunctionsVersion>` and `host.json` has `"version": "2.0"` (the runtime schema version for v4). The Pulumi stack was pointing at the v3 extension host, which would cause a version mismatch on `pulumi up`.

### 2. `FUNCTIONS_WORKER_RUNTIME`: `dotnet` → `dotnet-isolated`

**Why:** The project uses the **isolated worker model** — confirmed by `<OutputType>Exe</OutputType>` in the `.csproj` and the use of `Microsoft.Azure.Functions.Worker` (not `Microsoft.Azure.WebJobs.*`) packages. The value `dotnet` targets the in-process model (Functions v3/legacy). Deploying with `dotnet` would cause the host to fail to load the isolated worker process.

### 3. `runtime`: `dotnet` → `dotnet-isolated`

**Why:** The `runtime` app setting is a legacy companion to `FUNCTIONS_WORKER_RUNTIME`. It was set to `dotnet`, inconsistent with the actual runtime model. Updated to match.

### 4. Missing storage queues: LinkedIn (×3) and Bluesky (×1)

**Why:** The stack declared only `twitter-tweets-to-send` and `facebook-post-status-to-page`. Cross-referencing `Domain/Constants/Queues.cs` and the `QueueTrigger` attributes in the Functions revealed four additional queues that must exist in the storage account:
- `linkedin-post-link` (triggers `LinkedIn/PostLink.cs`)
- `linkedin-post-text` (triggers `LinkedIn/PostText.cs`)
- `linkedin-post-image` (triggers `LinkedIn/PostImage.cs`)
- `bluesky-post-to-send` (triggers `Bluesky/SendPost.cs`)

Without these queues being provisioned by Pulumi, any `pulumi up` on a fresh environment would result in the LinkedIn and Bluesky functions failing to bind on startup.

---

## Cross-Reference Sources

| Source | Key fact |
|--------|----------|
| `Functions.csproj` | `<AzureFunctionsVersion>v4</AzureFunctionsVersion>`, `<OutputType>Exe</OutputType>` |
| `host.json` | `"version": "2.0"` (Functions v4 runtime schema) |
| `Domain/Constants/Queues.cs` | Canonical list of all 6 queue names |
| `AppHost.cs` | Uses `AddAzureFunctionsProject<>` (Aspire v4-aware API) — no changes needed |

---

## Build Verification

`dotnet build --no-restore` completed with **0 errors** after changes. All warnings are pre-existing (CS8618 nullable ViewModels, CS1574 XML doc refs) and unrelated to this change.

---

## No Changes to AppHost.cs

`src/JosephGuadagno.Broadcasting.AppHost/AppHost.cs` uses `AddAzureFunctionsProject<>` with Aspire's abstraction layer — queue and storage resources are wired through Aspire references, not hardcoded app settings. No drift found there.
