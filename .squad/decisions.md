
# Morpheus Decisions: Orphan Detection (Issue #274)

## Date
2026-03-16

## Decisions

### 1. SQL Migration file location
Created `scripts/database/migrations/2026-03-16-scheduleditem-integrity.sql`.
The existing pattern places one-off scripts in `scripts/database/`. A `migrations/` subdirectory
was created to distinguish idempotency-sensitive one-time scripts from the base schema.

### 2. Valid ItemTableName values
Valid values enforced by the new CHECK constraint:
- `Engagements`
- `Talks`
- `SyndicationFeedSources`
- `YouTubeSources`

Bad legacy values fixed in the migration:
- `SyndicationFeed` → `SyndicationFeedSources`
- `YouTube` → `YouTubeSources`

### 3. Orphan detection SQL strategy
Used conditional NOT EXISTS per table name rather than dynamic SQL, since the set of valid
table names is fixed and small. This keeps it readable, type-safe, and fast with indexed PKs.

### 4. Return type
`GetOrphanedScheduledItemsAsync()` returns `IEnumerable<Domain.Models.ScheduledItem>` to stay
consistent with the domain layer. EF entity results are mapped via AutoMapper (same pattern as
all other methods in ScheduledItemDataStore).

### 5. Raw SQL approach
Used `FromSqlRaw` on `broadcastingContext.ScheduledItems` because the join condition is
conditional on a string column value — this cannot be expressed cleanly in LINQ without
client-side evaluation. `FromSqlRaw` is the existing EF Core pattern for this scenario.

### 6. Trinity coordination note
Trinity is adding a `ScheduledItemType` enum and renaming `ItemTableName` → `ItemType` on the
Domain model. The orphan detection method uses the EF entity (which still stores the string
`ItemTableName` in the DB) and relies on the existing AutoMapper mapping to produce
`Domain.Models.ScheduledItem`. No changes to the mapping layer are needed from our side.

# Decision: No Raw SQL in ScheduledItemDataStore

**Date:** 2026-03-16
**Source:** PR #280 review comment by @jguadagno
**Applies to:** Morpheus, Trinity (any data store work)

## Rule

Do NOT use FromSqlRaw, ExecuteSqlRaw, or any hardcoded SQL strings in ScheduledItemDataStore (or any DataStore).
Use **Entity Framework Core LINQ queries** instead. When type-based dispatch is needed (e.g. per ScheduledItemType), use the enum directly in LINQ predicates.

## Example (correct approach for orphan detection)

Use EF DbSets with .Where() and .ContainsAsync() / HashSet membership — do not write raw SQL.

# Decision: UI Dropdown Value Fix (Issue #274)

**Date:** 2025-01-16  
**Author:** Sparks (Frontend Developer)

## Decision

Updated `ItemTableName` dropdown option values in Schedule Add/Edit views and the supporting JS switch statement to match the backend's expected table name strings.

## Changes Made

| File | Change |
|------|--------|
| `Views/Schedules/Add.cshtml` | `value="SyndicationFeed"` → `"SyndicationFeedSources"`, `value="YouTube"` → `"YouTubeSources"` |
| `Views/Schedules/Edit.cshtml` | Same as above |
| `wwwroot/js/schedules.edit.js` | Updated `case` strings to match new values |

## Rationale

Display labels are user-facing and remain unchanged ("Syndication Feed", "YouTube"). Only the submitted `value` attributes were corrected to align with what Azure Functions collectors expect when looking up items in table storage.

## Outcome

Build passes (0 errors). Committed on branch `issue-274`.

# Tank: Decisions for Issue #274 Test Suite

## Context
Writing unit tests for issue #274 — ScheduledItems Referential Integrity changes.

## Decisions

### 1. No new test project needed
All tests placed in the existing `JosephGuadagno.Broadcasting.Data.Sql.Tests` project. It already had the right dependencies (xUnit v3, Moq, AutoMapper, EF InMemory) and a `ScheduledItemDataStoreTests.cs` to pattern-match against.

### 2. GetOrphanedScheduledItemsAsync tested via Moq (not EF InMemory)
The concrete implementation uses `FromSqlRaw` which is not supported by the EF Core InMemory provider. Rather than spin up a real SQL Server instance, mock-based contract tests against `IScheduledItemDataStore` are used. This verifies the interface contract and return-value propagation without requiring infrastructure.

### 3. Fixed pre-existing test breakage
`ScheduledItemDataStoreTests.cs` had a `CreateScheduledItem` helper and several inline test items using `ItemTableName = "TestTable"` / `"T"`. After issue #274 changed `BroadcastingProfile` to call `Enum.Parse<ScheduledItemType>(source.ItemTableName)`, these values caused `ArgumentException` at runtime. All occurrences were updated to use `"Engagements"` (a valid enum value). This restored 5 pre-broken tests to green.

### 4. Assertion library: xUnit Assert (not FluentAssertions)
The `Data.Sql.Tests` csproj does not reference FluentAssertions. All assertions use the standard xUnit `Assert.*` API to stay consistent with the existing test files.

### 5. Three new test files created
- `ScheduledItemTypeTests.cs` — enum value coverage (D) + domain model computed property (A)
- `ScheduledItemMappingTests.cs` — AutoMapper bidirectional mapping coverage (B)
- `ScheduledItemOrphanTests.cs` — mock-based orphan detection contract tests (C)

## Result
122/122 tests passing in `Data.Sql.Tests`. Committed to `issue-274` branch.

# Decision: Custom Exception Types for Social Managers (Issue #273)

**Date:** 2026-03-16
**Author:** Trinity (Backend Dev)
**Applies to:** Facebook Manager, LinkedIn Manager, Domain

## What was done

Introduced a typed exception hierarchy to replace generic `ApplicationException` and `HttpRequestException` throws in the social media manager classes.

### New Types

| Type | Location | Purpose |
|------|----------|---------|
| `BroadcastingException` | `Domain/Exceptions/` | Abstract base for all broadcasting-related exceptions. Carries optional `ApiErrorCode` and `ApiErrorMessage` properties. |
| `FacebookPostException` | `Managers.Facebook/Exceptions/` | Thrown by `FacebookManager` on API or deserialization failures. |
| `LinkedInPostException` | `Managers.LinkedIn/Exceptions/` | Thrown by `LinkedInManager` on API or deserialization failures. |

## Decisions Made

### 1. Base exception lives in Domain
`BroadcastingException` is placed in the `Domain` project so it can be referenced by any layer (API, Functions, Web) that needs to catch platform-specific errors without coupling to individual manager assemblies.

### 2. Domain reference added to both manager projects
`Managers.Facebook` and `Managers.LinkedIn` did not previously reference `Domain`. References were added via `dotnet add reference` to enable the inheritance chain.

### 3. `ArgumentNullException` throws left unchanged
Parameter validation guards that throw `ArgumentNullException` were intentionally left as-is — those represent programming errors (invalid call-site contract), not API failures.

### 4. All `HttpRequestException` and `ApplicationException` throws in the managers replaced
Every API-failure throw site in both managers was updated to the typed exception. This includes `ExecuteGetAsync`, `CallPostShareUrl`, `GetUploadResponse`, and `UploadImage` in `LinkedInManager`, and both methods in `FacebookManager`.

### 5. `throw;` re-throws left unchanged
Bare `throw;` statements in catch blocks remain as-is — they preserve the original stack trace and are correct.

# Decision: ScheduledItemType Enum (Issue #274)

**Author**: Trinity (Backend Dev)  
**Date**: 2025-07-11  
**Branch**: `issue-274`  
**Related todos**: `domain-enum`, `data-mapping`, `functions-enum`

## Summary

Added a `ScheduledItemType` enum to replace raw `string ItemTableName` usage in switch dispatching across all 4 Functions.

## Decisions

### 1. `ItemType` is the primary property; `ItemTableName` is computed

`Domain.Models.ScheduledItem` now has:
- `public ScheduledItemType ItemType { get; set; }` — the authoritative, type-safe property
- `public string ItemTableName => ItemType.ToString();` — computed, read-only, kept for backward-compat logging

**Rationale**: Keeps existing log statements (`scheduledItem.ItemTableName`) compiling without change, while making the switch in Functions fully type-safe. The DB column name (`ItemTableName`) is preserved in the EF entity (`Data.Sql.Models.ScheduledItem`) unchanged.

### 2. EF entity (`Data.Sql.Models.ScheduledItem`) unchanged

The SQL entity retains `public string ItemTableName { get; set; }`. The DB schema requires no migration.

### 3. AutoMapper handles string ↔ enum conversion

`BroadcastingProfile` uses `Enum.Parse<ScheduledItemType>` when mapping EF entity → Domain model, and `.ToString()` for the reverse. This is safe because the DB should only contain valid enum names; invalid values will throw at read time (fail-fast).

### 4. `WebMappingProfile` updated

`ScheduledItemViewModel.ItemTableName` (string) → `Domain.ScheduledItem.ItemType` (enum) via `Enum.Parse`, and back via `.ToString()`. The ViewModel itself is unchanged to avoid impacting Razor views.

### 5. All 4 Functions switch on `ScheduledItemType`

Twitter, Facebook, LinkedIn, and Bluesky `ProcessScheduledItemFired.cs` now switch on `scheduledItem.ItemType` using `ScheduledItemType` enum cases. `SourceSystems` constants are no longer used in the switch expressions but are not removed (they may have other usages).

### 6. Test data updated

All test files that set `ItemTableName` on `Domain.Models.ScheduledItem` (read-only after this change) were updated to set `ItemType = ScheduledItemType.SyndicationFeedSources` as a safe default where the specific type doesn't affect test logic.

# Twitter/Bluesky Exception Implementation Decisions

## Files Created
- `src/JosephGuadagno.Broadcasting.Managers.Twitter/Exceptions/TwitterPostException.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/Exceptions/BlueskyPostException.cs`

## Files Modified
- `src/JosephGuadagno.Broadcasting.Managers.Twitter/TwitterManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/BlueskyManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/JosephGuadagno.Broadcasting.Managers.Bluesky.csproj`

## Decisions

### TwitterManager
- `SendTweetAsync` now throws `TwitterPostException` instead of returning `null` on both null-tweet and exception paths
- Added a `catch (TwitterPostException) { throw; }` re-throw guard so the inner TwitterPostException created from a null tweet propagates cleanly through the outer catch

### BlueskyManager
- `Post()` now throws `BlueskyPostException` for both login failure and post failure paths, with HTTP status code and API error message captured via the `apiErrorCode`/`apiErrorMessage` constructor
- `DeletePost()` left unchanged — returns `false` on failure (boolean method, not a post operation)
- `GetEmbeddedExternalRecord()` thumbnail `HttpRequestException` catch left intentionally silent (per existing comment)
- Added `ProjectReference` to `JosephGuadagno.Broadcasting.Domain` in Bluesky csproj (was missing)

### BroadcastingException Wait
- Waited ~2 minutes for the base class to be created by the other Trinity agent before proceeding

# Decision: Enable All 5 Event Grid Topics in Local Dev

**Date:** 2026-07-11  
**Author:** Cypher (DevOps Engineer)  
**Branch:** `feature/s4-5-eventgrid-local-dev`  
**Task:** S4-5 — Enable all Event Grid topics in local dev event-grid-simulator

---

## Problem

The `event-grid-simulator-config.json` in the Functions project only had `new-youtube-item` enabled
(`"disabled": false`). The remaining 4 topics were disabled or misconfigured, blocking local
end-to-end testing of all event-driven code paths.

### Bugs found in addition to disabled topics

| Topic | Bug |
|---|---|
| `new-speaking-engagement` | `"port": true` — invalid type, should be `60102` |
| `new-random-post` | Missing `FacebookProcessNewRandomPost` and `LinkedInProcessNewRandomPost` subscribers |
| `new-speaking-engagement` | Facebook subscriber `name` label was `FacebookProcessNewSpeakingEngagementDataFired`; corrected to `FacebookProcessSpeakingEngagementDataFired` to match the function name in the endpoint |

---

## Changes Made

### `src/JosephGuadagno.Broadcasting.Functions/event-grid-simulator-config.json`

| Topic | Before | After |
|---|---|---|
| `new-random-post` (port 60101) | `disabled: true`, 2 subscribers (Bluesky, Twitter only) | `disabled: false`, all 4 subscribers |
| `new-speaking-engagement` (port 60102) | `port: true` (bug!), `disabled: true` | `port: 60102`, `disabled: false` |
| `new-syndication-feed-item` (port 60103) | `disabled: true` | `disabled: false` |
| `new-youtube-item` (port 60104) | unchanged — already correct | unchanged |
| `scheduled-item-fired` (port 60105) | `disabled: true` | `disabled: false` |

### `local.settings.json` — No changes needed

All 5 topic endpoint entries were already present with correct ports:
- `new-random-post` → `https://localhost:60101/api/events`
- `new-speaking-engagement` → `https://localhost:60102/api/events`
- `new-syndication-feed-item` → `https://localhost:60103/api/events`
- `new-youtube-item` → `https://localhost:60104/api/events`
- `scheduled-item-fired` → `https://localhost:60105/api/events`

### `AppHost.cs` — No changes needed

The Aspire AppHost uses `WithExternalHttpEndpoints()` on the Functions project, which already
covers all event-grid-simulator HTTP webhook traffic. No per-topic wiring is required at the
AppHost level.

---

## Subscriber Topology (as wired)

| Topic | Subscribers |
|---|---|
| `new-random-post` | BlueskyProcessRandomPostFired, FacebookProcessNewRandomPost, LinkedInProcessNewRandomPost, TwitterProcessRandomPostFired |
| `new-speaking-engagement` | BlueskyProcessSpeakingEngagementDataFired, FacebookProcessSpeakingEngagementDataFired, LinkedInProcessSpeakingEngagementDataFired, TwitterProcessSpeakingEngagementDataFired |
| `new-syndication-feed-item` | BlueskyProcessNewSyndicationDataFired, FacebookProcessNewSyndicationDataFired, LinkedInProcessNewSyndicationDataFired, TwitterProcessNewSyndicationDataFired |
| `new-youtube-item` | BlueskyProcessNewYouTubeDataFired, FacebookProcessNewYouTubeDataFired, LinkedInProcessNewYouTubeDataFired, TwitterProcessNewYouTubeDataFired |
| `scheduled-item-fired` | BlueskyProcessScheduledItemFired, FacebookProcessScheduledItemFired, LinkedInProcessScheduledItemFired, TwitterProcessScheduledItemFired |

All subscribers use port `59833` (Azure Functions local host) with `disableValidation: true`.

---

## Local Dev Architecture Note

Events flow: Publisher → `https://localhost:6010X/api/events` (simulator) → simulator fans out
to `http://localhost:59833/runtime/webhooks/EventGrid?functionName=<FunctionName>`.

The `AzureWebJobs.<FunctionName>.Disabled` entries in `local.settings.json` allow individual
functions to be selectively enabled during dev/test. All are disabled by default; developers
opt-in per session.

# Decision: All 5 Event Grid Topics in AppHost

**Date:** 2026-03-18
**Author:** Cypher (DevOps Engineer)

## Summary

Added all 5 Event Grid topics from `JosephGuadagno.Broadcasting.Domain.Constants.Topics` to the Aspire AppHost for Azure provisioning.

## Topics Provisioned

| Topic Name                | Constant                |
|---------------------------|-------------------------|
| `new-random-post`         | `Topics.NewRandomPost`          |
| `new-speaking-engagement` | `Topics.NewSpeakingEngagement`  |
| `new-syndication-feed-item` | `Topics.NewSyndicationFeedItem` |
| `new-youtube-item`        | `Topics.NewYouTubeItem`         |
| `scheduled-item-fired`    | `Topics.ScheduledItemFired`     |

## Decisions

### 1. `Azure.Provisioning.EventGrid` via `AddAzureInfrastructure`
There is no `Aspire.Hosting.Azure.EventGrid` package. Topics are provisioned using `builder.AddAzureInfrastructure()` with `EventGridTopic` from `Azure.Provisioning.EventGrid` 1.1.0.

### 2. Endpoints wired to Functions; keys are not
`Azure.Provisioning.EventGrid` 1.1.0 does not expose a `GetKeys()` or `listKeys` equivalent. Topic **endpoints** are output via `ProvisioningOutput` and wired to the Functions project as `EventGridTopics__TopicEndpointSettings__{index}__Endpoint` and `TopicName` env vars. **Keys must be set separately** via Azure App Service settings, Key Vault, or azd parameters.

### 3. Local dev unaffected
The `local.settings.json` already has all 5 topics configured for use with the event-grid-simulator. The AppHost additions only affect Azure provisioning via `azd`.

### 4. `infrastructure-needs.md` updated
Replaced the 2 outdated topics (`new-source-data`, `scheduled-item-fired`) with the correct 5 topics including full subscriber function tables.

# Ghost Decision: LinkedIn Token Refresh Automation (S4-1)

**Date:** 2026-03-17  
**Author:** Ghost (Security & Identity Specialist)  
**Branch:** `feature/s4-1-linkedin-token-automation`

---

## How LinkedIn Token Refresh Differs from Facebook

| Aspect | Facebook | LinkedIn |
|--------|----------|----------|
| Refresh mechanism | Call Graph API with existing long-lived token | OAuth2 `grant_type=refresh_token` using a stored refresh token |
| Token lifetime | ~60 days (long-lived) | 60 days (access), 365 days (refresh) |
| Refresh token issued | No — same token is extended | Yes — refresh token may rotate on use |
| Human interaction required | Never (fully automated) | Only on first authorization and if refresh token expires (365-day window) |
| Manager method | `IFacebookManager.RefreshToken(string token)` | `ILinkedInManager.RefreshTokenAsync(clientId, clientSecret, refreshToken, url)` |

**Key asymmetry:** Facebook's long-lived token can refresh itself. LinkedIn requires a separate refresh token stored in Key Vault, obtained during the initial OAuth2 authorization code flow in the Web UI (`LinkedInController`). The Web controller's `Callback` action was updated in this PR to persist `jjg-net-linkedin-refresh-token` alongside the access token.

---

## Key Vault Secrets Used

| Secret Name | Contents | Set By |
|-------------|----------|--------|
| `jjg-net-linkedin-access-token` | LinkedIn OAuth2 access token (60-day expiry) | `LinkedInController.Callback` (Web UI) / `LinkedIn.RefreshTokens` (Function, after refresh) |
| `jjg-net-linkedin-refresh-token` | LinkedIn OAuth2 refresh token (365-day expiry) | `LinkedInController.Callback` (Web UI) / `LinkedIn.RefreshTokens` (Function, if LinkedIn issues a new one) |

**Critical prerequisite:** `jjg-net-linkedin-refresh-token` must exist in Key Vault before the Function can run. It is populated when a user completes the OAuth2 flow in the Web UI for the first time (or re-authorizes). If the secret is missing or empty, the Function logs a `LogError` and exits gracefully — no crash.

---

## Timer Schedule Chosen

**Setting key:** `linkedin_refresh_tokens_cron_settings`  
**Production value (recommended):** `0 0 9 * * *` (daily at 09:00 UTC)  
**Local dev value:** `0 0 9 * * *`

### Rationale

- LinkedIn access tokens expire in **60 days**. The 5-day proactive buffer means refresh triggers when `expiry - 5 days < now`, i.e. from day 55 onward.  
- A **daily check at 09:00 UTC** is sufficient — no need to check every 2 minutes (unlike Facebook's development cron). Over-frequent checks risk unnecessary API calls and rate-limit exposure.  
- Facebook uses `0 */2 * * * *` only in dev for fast local iteration; production would also use a daily schedule. We set LinkedIn's dev cron directly to daily since there is no local token to test with anyway.

---

## Limitations Discovered

1. **Bootstrap requirement:** The refresh token flow cannot be bootstrapped without a human completing the OAuth2 authorization code flow at least once via the Web UI. This is inherent to LinkedIn's API — they do not support machine-only initial authorization.

2. **Refresh token rotation:** LinkedIn may issue a new refresh token on every refresh call. The Function handles this by saving the new refresh token back to Key Vault if one is returned.

3. **No `ILinkedInApplicationSettings.AccessTokenUrl` previously:** The settings model did not include the token endpoint URL. Added with default `https://www.linkedin.com/oauth/v2/accessToken`. This default can be overridden in Azure App Service settings.

4. **`TokenRefresh` tracking record name:** Uses the string `"LinkedIn"` as the token name in Table Storage, consistent with Facebook's `"LongLived"` / `"Page"` convention.

5. **Refresh token expiry not tracked in Table Storage:** The `TokenRefresh` model only tracks access token expiry. If the refresh token expires (365 days), the Function will log an error and require manual re-authorization. Consider adding a separate `TokenRefresh` record for the refresh token in a future sprint.

6. **LinkedIn's refresh token grant requires `offline_access` scope** (or equivalent — verify current LinkedIn documentation). The existing Web controller scopes (`_linkedInSettings.Scopes`) must include the permission that enables programmatic refresh. If the scope is not set correctly, the initial authorization will succeed but no refresh token will be issued.

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

# Decision: Staging Slots Confirmed Active

**Date:** 2026-03-18
**Author:** Link (Platform & DevOps Engineer)

## Context

My charter listed "No staging deployment slot or approval gate — every push to `main` goes straight to production" as a known issue. This has been resolved.

## Current State

All three Azure deployment targets have active staging slots:

| Service | App Name | Staging Slot |
|---|---|---|
| Azure Functions | `jjgnet-broadcast` | `jjgnet-broadcast-staging` |
| API App Service | `api-jjgnet-broadcast` | `api-jjgnet-broadcast-staging` |
| Web App Service | `web-jjgnet-broadcast` | `web-jjgnet-broadcast-staging` |

All three GitHub Actions workflows (`main_jjgnet-broadcast.yml`, `main_api-jjgnet-broadcast.yml`, `main_web-jjgnet-broadcast.yml`) already implement the correct 3-job pattern:

1. **build** — compiles, tests, publishes artifact
2. **deploy-to-staging** — deploys artifact to the staging slot
3. **swap-to-production** — runs under the `production` GitHub environment (approval gate), then performs an Azure slot swap

## Required GitHub Secret

All three `swap-to-production` jobs reference `${{ secrets.AZURE_RESOURCE_GROUP }}`. Confirm this secret is set in the repository.

## Known Issue Resolved

The "no staging slot" known issue in my charter is now closed. No pipeline changes are needed.

# Morpheus Decisions: ScheduledItems New Columns + MessageTemplates Table (Issue #269)

## Date
2026-03-17 (revised)

## Summary
**Revised design**: `MessageTemplate` is NOT stored as a per-row column on `ScheduledItems`.
Instead, a dedicated `MessageTemplates` lookup table holds Scriban templates keyed by
`(Platform, MessageType)`. `ScheduledItems` retains only the new `ImageUrl` nullable column.

## Column / Table Definitions

### `ScheduledItems` change (kept)

| Column    | Type             | Nullable | Purpose                                               |
|-----------|------------------|----------|-------------------------------------------------------|
| `ImageUrl` | `NVARCHAR(2048)` | YES      | URL of an image to attach/embed in the broadcast post |

### New `MessageTemplates` table

| Column        | Type             | Nullable | Purpose                                                       |
|---------------|------------------|----------|---------------------------------------------------------------|
| `Platform`    | `NVARCHAR(50)`   | NO (PK)  | Social platform name, e.g. `Twitter`, `Facebook`, etc.        |
| `MessageType` | `NVARCHAR(50)`   | NO (PK)  | Message category, e.g. `RandomPost`                           |
| `Template`    | `NVARCHAR(MAX)`  | NO       | Scriban template string used to render the broadcast message  |
| `Description` | `NVARCHAR(500)`  | YES      | Human-readable description of what the template is for        |

Primary key: composite `(Platform, MessageType)`.

## Design Choices

### 1. Composite PK `(Platform, MessageType)` — not a surrogate int
Templates are looked up by exact `(Platform, MessageType)` pair at send time. Using those two
business-key columns as the PK eliminates a redundant surrogate key, makes look-up queries
self-documenting, and enforces at the DB layer that each platform+type combination is unique.

### 2. `NVARCHAR(MAX)` for `Template`
Scriban templates can be arbitrarily long (conditional blocks, loops, variable references).
Consistent with the existing `Message` column on `ScheduledItems`.

### 3. `MessageTemplate` removed from `ScheduledItems`
A per-row template column couples the template definition to each scheduled item, causing
proliferation and inconsistency. The lookup table is the single source of truth; all scheduled
items for a given platform pick up the same template automatically.

### 4. `ImageUrl` stays on `ScheduledItems` (`NVARCHAR(2048)`, nullable)
Image choice is genuinely per-item — it makes sense as a row-level attribute.
2048 characters is the de-facto safe upper limit for a URL, matching existing URL columns in
the codebase.

### 5. Seed data
Four default rows are inserted by the migration, one per platform, for `MessageType = 'RandomPost'`:

| Platform  | Template                                              |
|-----------|-------------------------------------------------------|
| Twitter   | `{{ title }} - {{ url }}`                             |
| Facebook  | `{{ title }}\n\n{{ description }}\n\n{{ url }}`       |
| LinkedIn  | `{{ title }}\n\n{{ description }}\n\n{{ url }}`       |
| Bluesky   | `{{ title }} - {{ url }}`                             |

### 6. Migration approach
The existing migration file `2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql` was
revised in place (no new file needed — it was not yet applied to any environment). The
`ALTER TABLE … ADD MessageTemplate` statement was removed and replaced with the
`CREATE TABLE [dbo].[MessageTemplates]` DDL plus the 4 seed `INSERT` rows.

## Files Changed

| File | Change |
|------|--------|
| `scripts/database/table-create.sql` | Removed `MessageTemplate` from `ScheduledItems`; added `MessageTemplates` CREATE TABLE block |
| `scripts/database/migrations/2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql` | Replaced `ADD MessageTemplate` ALTER TABLE with `CREATE TABLE MessageTemplates` + seed INSERTs; kept `ADD ImageUrl` |

# Morpheus: DateTimeOffset Consistency (feature/datetimeoffset-consistency)

**Date:** 2026-03-18
**Branch:** `feature/datetimeoffset-consistency`
**Author:** Morpheus (Data Engineer)

## Summary

Audited all SQL datetime columns and C# model properties for timezone-aware (`DateTimeOffset`) consistency. The SQL schema was already fully `datetimeoffset`-consistent from prior migrations. Two C# model gaps were closed.

---

## SQL Schema Audit

**Result: No SQL changes needed.** Every point-in-time column in the schema already uses `DATETIMEOFFSET`. The schema was migrated to `DATETIMEOFFSET` during the initial table creation work (`2026-01-31-engagement-add-time-columns.sql`, `2026-02-04-move-from-table-storage.sql`).

### Confirmed DATETIMEOFFSET columns (nothing to change)

| Table | Column | Type | Notes |
|-------|--------|------|-------|
| `dbo.Engagements` | `StartDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Engagements` | `EndDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Engagements` | `CreatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.Engagements` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.Talks` | `StartDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Talks` | `EndDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.ScheduledItems` | `SendOnDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.ScheduledItems` | `MessageSentOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.Cache` | `ExpiresAtTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Cache` | `AbsoluteExpiration` | `datetimeoffset` | ✅ Already correct |
| `dbo.FeedChecks` | `LastCheckedFeed` | `datetimeoffset` | ✅ Already correct |
| `dbo.FeedChecks` | `LastItemAddedOrUpdated` | `datetimeoffset` | ✅ Already correct |
| `dbo.FeedChecks` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `Expires` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `LastChecked` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `LastRefreshed` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `PublicationDate` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `AddedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `ItemLastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `PublicationDate` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `AddedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `ItemLastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |

### No DATE-only columns found
No `DATE`-only columns exist in the schema — all temporal columns already carry full timestamp + offset information.

---

## EF Core & Domain Model Audit

All `Data.Sql.Models.*` and `Domain.Models.*` classes that correspond to DB columns already used `DateTimeOffset`. No changes needed there.

---

## C# Model Changes Made

### 1. `Domain.Models.LoadFeedItemsRequest`
**File:** `src/JosephGuadagno.Broadcasting.Domain/Models/LoadFeedItemsRequest.cs`

| Property | Old Type | New Type | Rationale |
|----------|----------|----------|-----------|
| `CheckFrom` | `DateTime` | `DateTimeOffset` | Represents a UTC/timezone-aware checkpoint used for feed filtering. Using `DateTime` was inconsistent with all other temporal Domain model properties. |

### 2. `SpeakingEngagementsReader.Models.Presentation`
**File:** `src/JosephGuadagno.Broadcasting.SpeakingEngagementsReader/Models/Presentation.cs`

| Property | Old Type | New Type | Rationale |
|----------|----------|----------|-----------|
| `PresentationStartDateTime` | `DateTime?` | `DateTimeOffset?` | JSON deserialization model for talk start times. These map to `Talk.StartDateTime` (`DateTimeOffset`) in the domain. Using `DateTime?` caused implicit conversion with potential loss of timezone offset when the source JSON carries ISO 8601 timestamps with offsets. |
| `PresentationEndDateTime` | `DateTime?` | `DateTimeOffset?` | Same rationale as above. |

---

## BroadcastingContext.cs

No changes. `BroadcastingContext.cs` has no explicit `HasColumnType("datetime2")` mappings — all EF Core column type inference relies on the CLR type (`DateTimeOffset`) mapping to SQL `datetimeoffset` automatically.

---

## Test Updates

**File:** `src/JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Tests/ModelsTests.cs`

Updated two `Assert.Equal` calls in `Presentation_Properties_Work` to use `new DateTimeOffset(new DateTime(...))` to match the updated `DateTimeOffset?` property type.

---

## Migration Script

`scripts/database/migrations/2026-03-18-datetimeoffset-consistency.sql` — audit/documentation script. Contains no DML/DDL since no schema changes were needed. Documents the full list of confirmed `datetimeoffset` columns for operational reference.

---

## Columns Left As-Is

All datetime columns were already `datetimeoffset`. No columns were intentionally left as `datetime`/`datetime2`.

Non-temporal columns (e.g., `Name`, `Url`, `ItemTableName`, `Platform`, `MessageType`) are string/int/bit types — no consideration needed.

## Coordination Note for Sparks

The domain models `Engagement.StartDateTime`, `Engagement.EndDateTime`, `Talk.StartDateTime`, `Talk.EndDateTime`, `ScheduledItem.SendOnDateTime`, and `ScheduledItem.MessageSentOn` are all `DateTimeOffset`. Sparks can safely apply timezone-aware display in the UI using `TimeZoneInfo.ConvertTime()` against these values with the `Engagement.TimeZoneId` IANA timezone identifier.

# Morpheus Decisions: MessageTemplate Seed Data (Issue S4-4-seed)

## Date
2026-03-18

## Branch
`feature/s4-4-seed-message-templates`

---

## Summary

Added default seed data for the `MessageTemplates` table to `scripts/database/data-create.sql`.
This ensures that when Aspire provisions a fresh database, all 4 platforms × 5 message types
(20 total rows) are pre-populated. Without this, Scriban rendering in the publish Functions
would fall through to hardcoded fallback strings on every send.

---

## Scriban Template Variables (per message type)

All 4 `ProcessScheduledItemFired` Functions populate these fields in `TryRenderTemplateAsync`:

| Variable | Source | Feed/YouTube | Engagements | Talks |
|----------|--------|:---:|:---:|:---:|
| `{{ title }}` | `Title` / `Name` | ✅ | ✅ | ✅ |
| `{{ url }}` | `ShortenedUrl ?? Url` / `Url` / `UrlForTalk` | ✅ | ✅ | ✅ |
| `{{ description }}` | `Comments` (empty for feed/YouTube) | empty string | `Comments ?? ""` | `Comments` |
| `{{ tags }}` | `Tags ?? ""` (empty for engagements/talks) | `Tags ?? ""` | empty string | empty string |
| `{{ image_url }}` | `ScheduledItem.ImageUrl` (nullable) | ✅ | ✅ | ✅ |

> **Note on `image_url`**: It is passed to the Scriban context but is NOT forwarded to any of the
> 4 platform queue payload types (Twitter/Bluesky use `string?`, Facebook uses `FacebookPostStatus`,
> LinkedIn uses `LinkedInPostLink` — none have an image field). A `LogInformation` is emitted when
> `image_url` is non-null. Image support is a future work item.

---

## Platform-Specific Constraints

| Platform | Character limit | Tone | Notes |
|----------|----------------|------|-------|
| Twitter | ~280 chars | Casual | Templates kept short: `title + url` pattern |
| Bluesky | ~300 chars | Casual | Same length constraints as Twitter |
| Facebook | ~2000 chars | Informal | Multi-line with description block |
| LinkedIn | ~3000 chars | Professional | Multi-line with description block |

---

## Message Types Seeded

| MessageType | Purpose | Currently used in code? |
|-------------|---------|:---:|
| `RandomPost` | Default template for all scheduled items | ✅ Yes (all 4 Functions query this) |
| `NewSyndicationFeedItem` | New RSS/Atom blog post announced | ❌ Reserved for future use |
| `NewYouTubeItem` | New YouTube video announced | ❌ Reserved for future use |
| `NewSpeakingEngagement` | New conference/event speaking slot | ❌ Reserved for future use |
| `ScheduledItem` | Generic scheduled broadcast | ❌ Reserved for future use |

> All 4 Functions currently load only `MessageTypes.RandomPost` (see `MessageTemplates.cs` constants).
> The other 4 types are seeded now so they are ready when the code is extended.

---

## Template Designs

### Twitter & Bluesky (short-form)

| MessageType | Template |
|-------------|----------|
| RandomPost | `{{ title }} - {{ url }}` |
| NewSyndicationFeedItem | `Blog Post: {{ title }} {{ url }}` |
| NewYouTubeItem | `New video: {{ title }} {{ url }}` |
| NewSpeakingEngagement | `I'm speaking at {{ title }}! {{ url }}` (Twitter) / `Speaking at {{ title }}! {{ url }}` (Bluesky) |
| ScheduledItem | `{{ title }} {{ url }}` |

### Facebook (multi-line, informal)

| MessageType | Template |
|-------------|----------|
| RandomPost | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |
| NewSyndicationFeedItem | `ICYMI: {{ title }}\n\n{{ description }}\n\n{{ url }}` |
| NewYouTubeItem | `New video: {{ title }}\n\n{{ description }}\n\nWatch now: {{ url }}` |
| NewSpeakingEngagement | `I'm speaking at {{ title }}!\n\n{{ description }}\n\n{{ url }}` |
| ScheduledItem | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |

### LinkedIn (multi-line, professional)

| MessageType | Template |
|-------------|----------|
| RandomPost | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |
| NewSyndicationFeedItem | `New blog post: {{ title }}\n\n{{ description }}\n\nRead more: {{ url }}` |
| NewYouTubeItem | `New video: {{ title }}\n\n{{ description }}\n\nWatch: {{ url }}` |
| NewSpeakingEngagement | `I am excited to announce I will be speaking at {{ title }}.\n\n{{ description }}\n\nLearn more: {{ url }}` |
| ScheduledItem | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |

---

## Seed Approach

### Why `data-create.sql` (not a migration)?

The Aspire AppHost (`AppHost.cs`) uses `WithCreationScript` which concatenates exactly:
1. `database-create.sql`
2. `table-create.sql`
3. `data-create.sql`

The `scripts/database/migrations/` directory is NOT loaded by Aspire — migrations are manual
one-off scripts for existing databases. Since the `MessageTemplates` table is already defined
in `table-create.sql`, the seed data must go in `data-create.sql` to be provisioned on fresh
database creation.

> **Cross-reference**: The migration file `2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql`
> seeded 4 `RandomPost` templates for existing databases. The new `data-create.sql` entries cover
> all 20 templates for fresh provisioning.

### Idempotency

Each of the 20 inserts is wrapped in an `IF NOT EXISTS` guard:
```sql
IF NOT EXISTS (SELECT 1 FROM JJGNet.dbo.MessageTemplates
               WHERE Platform = N'Twitter' AND MessageType = N'RandomPost')
    INSERT INTO JJGNet.dbo.MessageTemplates ...
```

This makes the seed block re-runnable (e.g., if someone runs `data-create.sql` against an
existing database, or if Aspire's creation script mechanism is ever changed).

### Newlines in multi-line templates

Facebook and LinkedIn templates use SQL Server `CHAR(10)` concatenation for embedded newlines,
matching the pattern established in the existing migration:
```sql
N'{{ title }}' + CHAR(10) + CHAR(10) + N'{{ description }}' + CHAR(10) + CHAR(10) + N'{{ url }}'
```

This produces `\n\n` (double newline) paragraph breaks, which render correctly in social platform
post text fields.

# Sparks: ImageUrl Field Added to ScheduledItem Views (Issue #269)

**Date:** 2025-07-11
**Branch:** `issue-269`
**Author:** Sparks (Frontend Developer)

## Summary

Added `ImageUrl` as an optional form field to both the Add and Edit views for ScheduledItems.

## Files Changed

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Web/Models/ScheduledItemViewModel.cs` | Added `public string? ImageUrl { get; set; }` with `[Url]` and `[Display(Name = "Image URL")]` annotations |
| `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Add.cshtml` | Added `ImageUrl` form field after the `Message` field |
| `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Edit.cshtml` | Added `ImageUrl` form field after the `Message` field |

## ViewModel Change

Added to `ScheduledItemViewModel`:

```csharp
[Url]
[Display(Name = "Image URL")]
public string? ImageUrl { get; set; }
```

- `[Url]` provides client-side and server-side URL format validation
- `[Display(Name = "Image URL")]` drives the label rendered by `asp-for`
- Nullable (`string?`) — field is optional, no `[Required]`

## AutoMapper — No Changes Needed

`WebMappingProfile` maps `ScheduledItemViewModel` ↔ `Domain.Models.ScheduledItem` via `CreateMap`. Both have a property named `ImageUrl`, so AutoMapper maps it by convention. No explicit `.ForMember()` call was needed.

## Form Layout

The field appears between **Message** and **Sent on Date/Time** in both views:

```html
<div class="mb-3">
    <label asp-for="ImageUrl" class="form-label"></label>
    <input asp-for="ImageUrl" type="url" class="form-control" placeholder="https://example.com/image.jpg" />
    <span asp-validation-for="ImageUrl" class="text-danger"></span>
</div>
```

- Uses `type="url"` for native browser URL validation hint
- Placeholder: `https://example.com/image.jpg`
- Label text rendered from `[Display(Name = "Image URL")]` via `asp-for`
- Validation span for unobtrusive client-side error display
- No new JS dependencies

## Build Result

`Build succeeded. 0 Error(s)` — all pre-existing warnings only (CS8618 nullable, unrelated to this change).

# Sparks: DateTimeOffset Timezone-Aware Display in Web UI

**Date:** 2026-03-18
**Branch:** `feature/datetimeoffset-consistency`
**Author:** Sparks (Frontend Developer)

## Summary

All `DateTimeOffset` values in the Web UI are now displayed in the **browser's local timezone** rather than as raw UTC strings. This work was originally delivered in PR #213 (`feat: add local time display to all DateTimeOffset views in Web project`) and is now confirmed consistent with the `feature/datetimeoffset-consistency` branch where Morpheus completed the domain/data layer audit.

---

## Approach

### 1. `LocalTimeTagHelper` (`TagHelpers/LocalTimeTagHelper.cs`)

A custom ASP.NET Core Tag Helper that renders a `<time>` element carrying:
- `datetime` attribute — ISO 8601 string (`"o"` format) for JavaScript consumption
- `data-local-time` attribute — either `"date"` or `"datetime"` (controlled by the `date-only` parameter)
- Inner text — server-side fallback using `"d"` (short date) or `"f"` (full date/time) format specifiers

```html
<!-- Razor source -->
<local-time value="@Model.SendOnDateTime" />

<!-- Rendered HTML -->
<time datetime="2026-03-18T14:30:00+00:00" data-local-time="datetime">Tuesday, March 18, 2026 2:30 PM</time>
```

### 2. Client-Side Conversion (`wwwroot/js/site.js`)

A small `DOMContentLoaded` listener queries all `time[data-local-time]` elements and replaces their text content with the browser-locale string using the built-in `Date` constructor and `toLocaleString()` / `toLocaleDateString()`. No external libraries.

### 3. `_Layout.cshtml` Integration

`site.js` is already referenced globally at the bottom of `_Layout.cshtml` via `<script src="~/js/site.js" asp-append-version="true"></script>`, so all pages automatically get timezone conversion.

---

## Views Updated

All display views use `<local-time>` — **no raw `.ToString()` calls** remain on datetime fields in any view.

| View | Fields |
|------|--------|
| `Schedules/Index.cshtml` | `SendOnDateTime` |
| `Schedules/Upcoming.cshtml` | `SendOnDateTime` |
| `Schedules/Unsent.cshtml` | `SendOnDateTime` |
| `Schedules/Calendar.cshtml` | `SendOnDateTime` |
| `Schedules/Details.cshtml` | `SendOnDateTime`, `MessageSentOn` |
| `Schedules/Delete.cshtml` | `SendOnDateTime` |
| `Engagements/Index.cshtml` | `StartDateTime`, `EndDateTime`, `LastUpdatedOn` (date-only) |
| `Engagements/Details.cshtml` | `StartDateTime`, `EndDateTime`, `CreatedOn`, `LastUpdatedOn`, nested talk times |
| `Engagements/Edit.cshtml` | Nested talk `StartDateTime`, `EndDateTime` |
| `Engagements/Delete.cshtml` | `StartDateTime`, `EndDateTime` |
| `Talks/Details.cshtml` | `StartDateTime`, `EndDateTime` |
| `Talks/Delete.cshtml` | `StartDateTime`, `EndDateTime` |

Add/Edit forms use `<input type="datetime-local">` (native browser date/time picker) — no change needed there.

---

## Decisions

### 1. Tag Helper over inline spans
Used a reusable Tag Helper (`<local-time value="...">`) rather than copy-pasting `<span class="local-time" data-utc="...">` inline in every view. This keeps views clean and the ISO 8601 serialization logic in one place.

### 2. `<time>` element with `datetime` attribute
Used the semantic HTML `<time>` element with the standard `datetime` attribute (not `data-utc`). This is both semantically correct and accessible.

### 3. Server-side fallback text
The server renders a human-readable fallback (`"f"` or `"d"` format) inside the `<time>` element. If JavaScript is disabled or slow to load, users still see a meaningful date/time string (in UTC/server timezone).

### 4. `toLocaleString()` / `toLocaleDateString()` — no `Intl.DateTimeFormat` options
Kept the JS simple with no explicit locale options. The browser uses the user's system locale for formatting. This matches the broadest range of user preferences without over-specifying.

### 5. No `datetime-local.js` — used `site.js` instead
The suggested `datetime-local.js` approach was folded into the existing `site.js` to avoid adding a redundant script reference to `_Layout.cshtml`. `site.js` is already globally included.

---

## Coordination Note

- Morpheus confirmed (on the same branch) that all SQL and domain model datetime fields are `DateTimeOffset` — no conversions or casts are needed server-side.
- The `"o"` round-trip format specifier in C# produces strings like `2026-03-18T14:30:00+00:00`, which the browser `Date` constructor parses correctly.

# Sparks: Decisions for S4-4-UI MessageTemplate Views

**Date:** 2025-07-11
**Author:** Sparks (Frontend Developer)
**Branch:** `feature/s4-4-ui-message-template-management`

## Summary

Implemented Razor views and nav entry for the MessageTemplates management UI.

## Decisions

### 1. Index view: grouped table by Platform

Templates are rendered as one Bootstrap `table-striped table-hover` per platform, with an `<h4>` heading for each group. Sorted by Platform then MessageType for predictable order. This is clearer than a flat table with a Platform column because the 4×5 matrix is small and logically organized by platform.

### 2. Template truncation with Bootstrap tooltip

The template body can be long. Index shows first 80 chars with `…` and the full template in a `title` / `data-bs-toggle="tooltip"` attribute. Bootstrap tooltips are initialized via a small vanilla JS snippet in `@section Scripts` — no new dependencies.

### 3. Edit view: two-column layout

Used Bootstrap `row g-4` / `col-lg-8` + `col-lg-4`:
- Left: the edit form (Platform, MessageType as read-only text inputs, Description, Template textarea)
- Right: Scriban variable reference card (`card border-info`)

The variable reference panel documents `title`, `url`, `description`, `tags`, `image_url` with availability notes per item type, derived from `TryRenderTemplateAsync` in the Functions project.

### 4. Template textarea: monospace, 6 rows

Used `style="font-family: monospace; font-size: 0.9em;"` inline on the `<textarea>` — consistent with the task spec and keeps it simple without adding a CSS class. Placeholder text shows example Scriban syntax.

### 5. Scriban syntax in the reference panel uses Razor escaping

Scriban `{{ variable }}` conflicts with Razor syntax. Used `{{ "{{" }} variable {{ "}}" }}` to safely render the double-braces in the HTML without Razor attempting to interpret them.

### 6. Nav link placement

Added "Message Templates" as a plain `nav-item` between the Schedules dropdown and Privacy, matching the existing nav item style. A simple link (not a dropdown) is sufficient since there is only one page under this section (Index, with Edit reachable via row button).

### 7. No new JS dependencies

All interactivity (tooltip initialization) uses Bootstrap 5's built-in JS that is already loaded by `_Layout.cshtml`. No additional scripts or LibMan entries needed.

# Switch: Calendar Widget — FullCalendar.js for Speaking Engagements

**Date:** 2026-07-14
**Author:** Switch (Frontend Engineer)
**Branch:** `feature/calendar-widget`
**Issue:** Calendar placeholder replaced per squad tasking

---

## What Was Done

Replaced the `<!-- TODO: Add real calender -->` placeholder in `Views/Schedules/Calendar.cshtml`
with a functional FullCalendar.js month-view calendar that displays speaking engagements fetched
asynchronously from a new JSON endpoint.

---

## Where the Calendar View Lives

`src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Calendar.cshtml`

Served by `SchedulesController.Calendar(int? year, int? month)` at route `/Schedules/Calendar`.
The existing navigation link in `_Layout.cshtml` (Schedules → Calendar) continues to work
unchanged.

---

## Controller Action Added for JSON Events

**File:** `src/JosephGuadagno.Broadcasting.Web/Controllers/EngagementsController.cs`

```csharp
[HttpGet]
public async Task<JsonResult> GetCalendarEvents()
```

**Route:** `GET /Engagements/GetCalendarEvents`

Returns a JSON array in FullCalendar's native event format:

```json
[
  {
    "id": "42",
    "title": "Conference Name",
    "start": "2026-05-15T09:00:00",
    "end": "2026-05-16T18:00:00",
    "url": "https://..."
  }
]
```

Data sourced from `IEngagementService.GetEngagementsAsync()` (all engagements, no date filter —
FullCalendar shows the relevant month and users can navigate freely).

**Rationale for placement in EngagementsController:** The data is engagement data; putting the
endpoint on `EngagementsController` keeps data access co-located with the domain. The Calendar
view (in Schedules) simply fetches from this endpoint.

---

## LibMan Entry Added

**File:** `src/JosephGuadagno.Broadcasting.Web/libman.json`

```json
{
  "library": "fullcalendar@6.1.15",
  "destination": "wwwroot/libs/fullcalendar",
  "files": ["index.global.min.js"]
}
```

**Notes:**
- Provider: `jsdelivr` (project default)
- Only `index.global.min.js` is needed — FullCalendar 6's global build auto-injects its own CSS
  at runtime (no separate `.css` file ships in the npm package).
- `wwwroot/libs/` is in `.gitignore`; LibMan restores at dev setup via `libman restore`.

---

## Layout Change

**File:** `src/JosephGuadagno.Broadcasting.Web/Views/Shared/_Layout.cshtml`

Added `@await RenderSectionAsync("Styles", required: false)` inside `<head>` (after `site.css`).
This enables per-page `@section Styles { }` blocks. The Calendar view uses this to set a
`max-width` on the `#calendar` container.

---

## Design Decisions

1. **All engagements, no date filter** — `GetCalendarEvents` returns all engagements. FullCalendar
   handles display by month; users navigate with prev/next. A future enhancement could add
   `start`/`end` query params to filter server-side if the dataset grows large.

2. **JS only, no Razor model rendering** — The Calendar view no longer renders server-side event
   data. The `@model List<ScheduledItemViewModel>?` declaration is kept for controller
   compatibility (the `Calendar` action still passes the model) but the view ignores it.

3. **Two calendar views** — `dayGridMonth` (default) and `listYear` are exposed via the header
   toolbar. List view is useful for scanning upcoming talks by date.

4. **Event click → new tab** — Engagement URLs open in a new browser tab, keeping the app open.

5. **No jQuery dependency** — FullCalendar 6 global build is vanilla JS; no additional framework
   needed beyond what's already on the page.

# Switch: Decisions for S4-4-UI MessageTemplate Management

**Date:** 2025-07-11
**Author:** Switch (Frontend Engineer)
**Branch:** `feature/s4-4-ui-message-template-management`

## Summary

Implemented the controller, ViewModel, service interface, and service layer for the MessageTemplates management UI.

## Decisions

### 1. Service layer over direct DataStore injection

The Web project communicates with the API via HTTP client services (same pattern as `EngagementService`, `ScheduledItemService`). `IMessageTemplateDataStore` was NOT injected directly into the Web controller because the Web project has no DB context registration — it talks to the API. Instead:

- Created `IMessageTemplateService` in `Web/Interfaces/`
- Created `MessageTemplateService : ServiceBase` in `Web/Services/`
- Registered via `services.TryAddScoped<IMessageTemplateService, MessageTemplateService>()` in `Program.cs`

### 2. Added UpdateAsync to IMessageTemplateDataStore and MessageTemplateDataStore

The existing interface only had `GetAsync` and `GetAllAsync`. Added:

```csharp
Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate);
```

Implementation uses `FirstOrDefaultAsync` (no `AsNoTracking`) on the composite PK, mutates `Template` and `Description`, then calls `SaveChangesAsync`.

### 3. New API MessageTemplatesController

Added `src/JosephGuadagno.Broadcasting.Api/Controllers/MessageTemplatesController.cs` with:
- `GET /messagetemplates` — GetAllAsync
- `GET /messagetemplates/{platform}/{messageType}` — GetAsync
- `PUT /messagetemplates/{platform}/{messageType}` — UpdateAsync

Injects `IMessageTemplateDataStore` directly (no manager layer needed for this simple entity). Uses `Domain.Scopes.MessageTemplates.All` for authorization.

### 4. Added MessageTemplates scope

Added `Scopes.MessageTemplates` class with `All = "MessageTemplates.All"` in `Domain/Scopes.cs`. Updated `AllAccessToDictionary` to include this scope so the Web's MSAL token acquisition requests it.

### 5. Web MessageTemplatesController actions

- `Index()` — GET, lists all templates (no route params)
- `Edit(string platform, string messageType)` — GET, renders edit form
- `Edit(MessageTemplateViewModel model)` — POST, saves and redirects to Index on success

On save failure, re-renders the edit form with a `ModelState` error (consistent with other controllers).

### 6. AutoMapper in WebMappingProfile

Added bidirectional mappings:
```csharp
CreateMap<Models.MessageTemplateViewModel, Domain.Models.MessageTemplate>();
CreateMap<Domain.Models.MessageTemplate, Models.MessageTemplateViewModel>();
```
All properties are 1:1 — no custom `ForMember` calls needed.

### 7. No Delete action

The task scope is Index (list) + Edit (update template body). Delete is intentionally excluded — templates are seeded configuration data, not user-created records. Adding/removing templates requires a DB seed change.

# Tank: Decisions for Issue #269 Test Suite — Scriban Template Rendering

## Date
2026-03-17

## Branch
`issue-269` — commit `f98295d`

---

## Files Created

| File | Tests |
|------|-------|
| `src/JosephGuadagno.Broadcasting.Data.Sql.Tests/MessageTemplateDataStoreTests.cs` | 7 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Scriban/ScribanTemplateRenderingTests.cs` | 10 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Twitter/ProcessScheduledItemFiredTests.cs` | 5 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Facebook/ProcessScheduledItemFiredTests.cs` | 5 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/LinkedIn/ProcessScheduledItemFiredTests.cs` | 5 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Bluesky/ProcessScheduledItemFiredTests.cs` | 5 |

**Total new tests: 37**  
**All 37 pass. Pre-existing tests unaffected (40/40 Functions.Tests, 126/126 Data.Sql.Tests).**

---

## Decisions

### 1. `MessageTemplateDataStoreTests` placed in `Data.Sql.Tests`
The `MessageTemplateDataStore` is a concrete EF-backed repository in `Data.Sql`. The `Data.Sql.Tests` project already has EF InMemory, AutoMapper with `BroadcastingProfile`, and the xUnit patterns needed. Tests use the InMemory database to verify `GetAsync` (found/not-found/wrong platform/wrong message type/multiple platforms) and `GetAllAsync`.

The `MessageTemplate` entity has a composite primary key `(Platform, MessageType)` — EF InMemory handles this correctly.

### 2. `TryRenderTemplateAsync` is private — tested indirectly via `RunAsync`
All four platform functions expose `TryRenderTemplateAsync` only as `private`. Rather than using reflection (an anti-pattern), the per-platform tests go through the public `RunAsync` API with fully mocked dependencies. This validates the full integration of the template lookup → rendering → fallback logic.

The `EventGridEvent` is constructed with `BinaryData.FromString(json)` where `json` is a serialized `ScheduledItemFiredEvent`. This avoids any real Azure service dependency.

### 3. `ScribanTemplateRenderingTests` — isolated rendering proof
A separate class directly exercises the exact `Template.Parse → ScriptObject.Import → TemplateContext → RenderAsync` pattern that all 4 functions share. This provides:
- Definitive proof that `title`, `url`, `description`, `tags`, `image_url` are all accessible in templates
- Edge-case coverage: null image_url renders as empty string, whitespace-only output returns null, trimming is applied

These tests are platform-agnostic since all 4 functions use identical rendering code.

### 4. `NullLogger<T>.Instance` used instead of `Mock<ILogger<T>>`
All 4 functions make extensive `LogDebug`/`LogInformation`/`LogWarning`/`LogError` and `LogCustomEvent` calls. Using `NullLogger<T>` is simpler and cleaner than configuring `Mock<ILogger<T>>` for extension methods. Tests don't assert on log output — only on return values and mock invocations.

### 5. `SyndicationFeedSources` used as item type in all per-platform tests
The Scriban rendering logic is symmetric across all 4 item types (Feed, YouTube, Engagement, Talk) in each function. Using `SyndicationFeedSources` for all tests keeps the fixture code concise without losing coverage of the fallback/template decision branch. The `ScribanTemplateRenderingTests` covers field-level rendering independently of item type.

### 6. `Functions.Tests` csproj has no `ImplicitUsings`
Unlike `Data.Sql.Tests`, the `Functions.Tests` project does not enable implicit usings. All new test files include explicit `using System;`, `using System.Threading.Tasks;` etc. to match the project convention seen in `LoadNewPostsTests.cs`.

---

## Test Coverage Summary

| Coverage area | Tests | Notes |
|---|---|---|
| `MessageTemplateDataStore.GetAsync` (found) | 2 | Exact match + multi-platform selection |
| `MessageTemplateDataStore.GetAsync` (not found) | 3 | Empty DB, wrong platform, wrong type |
| `MessageTemplateDataStore.GetAllAsync` | 2 | Multiple + empty |
| Scriban field rendering (title, url, description, tags, image_url) | 10 | Isolated; all 5 fields tested individually and together |
| Template found → rendered text used (per platform) | 4 | Twitter, Facebook, LinkedIn, Bluesky |
| Template null → fallback (per platform) | 4 | Twitter/Bluesky → auto-generated; LinkedIn → scheduledItem.Message |
| `image_url` in context when set (per platform) | 4 | Verified in rendered output |
| `image_url` empty when null (per platform) | 4 | Scriban renders null as "" |
| Facebook: `LinkUri` always from item, not template | 1 | Template overrides StatusText only |
| LinkedIn: credentials always from settings | 1 | AuthorId + AccessToken unaffected by template |
| Empty template string → fallback (Twitter, Bluesky) | 2 | Whitespace template → null → fallback |

---

## Gaps / Future Testing Notes

- **`YouTubeSources`, `Engagements`, `Talks` item types** not exercised in per-platform `RunAsync` tests. The Scriban rendering path is the same for all types, but the item-manager mock setup differs. Future tests could add coverage for those branches.
- **`MessageTemplateDataStore.GetAllAsync` sorting/filtering** — no filtering tests since the method returns all rows. If filtering is added later, tests will need updating.
- **Scriban template errors** — the `catch → return null` guard in `TryRenderTemplateAsync` is covered indirectly by the isolated `ScribanTemplateRenderingTests` edge cases, but is not explicitly tested through `RunAsync` (would require mocking template content that causes Scriban to throw).
- **Integration tests** — full end-to-end (Functions.IntegrationTests) would require Aspire AppHost and real DB. Not attempted here.

# Trinity Decisions: MessageTemplate Domain Model (Issue #269) — REVISED

## Date
2026-03-17 (revised — supersedes prior note)

## Summary
**Revised per Morpheus schema change**: `MessageTemplate` column was removed from `ScheduledItems`.
`ImageUrl` stays on `ScheduledItems`. A new dedicated `MessageTemplates` lookup table (composite PK)
holds Scriban templates keyed by `(Platform, MessageType)`.

This note documents the revised C# changes made in commit `e662c56` on branch `issue-269`.

## Files Changed

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Domain/Models/ScheduledItem.cs` | Removed `MessageTemplate` property; `ImageUrl` kept |
| `src/JosephGuadagno.Broadcasting.Data.Sql/Models/ScheduledItem.cs` | Removed `MessageTemplate` property; `ImageUrl` kept |
| `src/JosephGuadagno.Broadcasting.Domain/Models/MessageTemplate.cs` | **New** — Domain model with `Platform`, `MessageType`, `Template`, `Description` |
| `src/JosephGuadagno.Broadcasting.Domain/Interfaces/IMessageTemplateDataStore.cs` | **New** — Interface: `GetAsync(platform, messageType)` + `GetAllAsync()` |
| `src/JosephGuadagno.Broadcasting.Data.Sql/Models/MessageTemplate.cs` | **New** — EF entity (`#nullable disable`, matches DB schema) |
| `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` | Added `DbSet<MessageTemplate> MessageTemplates`; configured composite PK `(Platform, MessageType)`, `Template` (no max length = NVARCHAR(MAX)), `Description` (max 500) |
| `src/JosephGuadagno.Broadcasting.Data.Sql/MappingProfiles/BroadcastingProfile.cs` | Added `CreateMap<Models.MessageTemplate, Domain.Models.MessageTemplate>().ReverseMap()` |
| `src/JosephGuadagno.Broadcasting.Data.Sql/MessageTemplateDataStore.cs` | **New** — Implements `IMessageTemplateDataStore` with `BroadcastingContext` + `IMapper` primary constructor pattern |
| `src/JosephGuadagno.Broadcasting.Api/Program.cs` | Added DI registration (see below) |

## DI Registration Added

**File:** `src/JosephGuadagno.Broadcasting.Api/Program.cs`

```csharp
// MessageTemplate
services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();
```

Placed after the `ScheduledItem` block (~line 165). Only API registered — Functions and Web are
out of scope for this task.

## Design Choices

### 1. `IMessageTemplateDataStore` does NOT inherit `IDataStore<T>`
Standard `IDataStore<T>` uses `int primaryKey`. `MessageTemplates` has a composite PK
`(Platform, MessageType)`. A custom interface with `GetAsync(string, string)` and `GetAllAsync()`
matches the actual look-up pattern (read-only lookup by platform+type at send time).

### 2. `AsNoTracking()` in data store
`MessageTemplates` is a read-only lookup at runtime. `AsNoTracking()` avoids unnecessary EF
change-tracking overhead on every send.

### 3. AutoMapper — `.ReverseMap()` sufficient
Both the EF entity and domain model have identical property names and types. No custom `ForMember`
mappings are needed.

### 4. `Template` property — no `.HasMaxLength()` in EF config
`NVARCHAR(MAX)` is the SQL type (per Morpheus decision). EF Core maps an unconstrained `string`
to `NVARCHAR(MAX)` by default; adding a max-length would cause a schema mismatch.

### 5. Build result
`dotnet build` — **Build succeeded, 0 errors**. All warnings are pre-existing nullable reference /
XML doc warnings unrelated to this change.

# Trinity Decisions: Scriban Template Rendering in Publish Functions (Issue #269)

## Date
2026-03-17

## Branch
`issue-269` — commit `f924641`

---

## Files Modified

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Functions/JosephGuadagno.Broadcasting.Functions.csproj` | Added `Scriban 6.5.8` NuGet package |
| `src/JosephGuadagno.Broadcasting.Functions/Program.cs` | Registered `IMessageTemplateDataStore` → `MessageTemplateDataStore` as scoped in `ConfigureFunction` |
| `src/JosephGuadagno.Broadcasting.Functions/Twitter/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |
| `src/JosephGuadagno.Broadcasting.Functions/Facebook/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |
| `src/JosephGuadagno.Broadcasting.Functions/LinkedIn/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |
| `src/JosephGuadagno.Broadcasting.Functions/Bluesky/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |

---

## Scriban Model Field Names

The Scriban template context exposes these fields (populated from the referenced item):

| Field | Source |
|-------|--------|
| `title` | `SyndicationFeedSource.Title` / `YouTubeSource.Title` / `Engagement.Name` / `Talk.Name` |
| `url` | `ShortenedUrl ?? Url` for feed/YouTube; `Engagement.Url`; `Talk.UrlForTalk` |
| `description` | Empty string for feed/YouTube; `Engagement.Comments ?? ""`; `Talk.Comments` |
| `tags` | `feed.Tags ?? ""` / `yt.Tags ?? ""`; empty string for engagement/talk |
| `image_url` | `ScheduledItem.ImageUrl` (nullable) |

Example seed templates (from `scripts/database/migrations/2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql`):
- Twitter/Bluesky: `{{ title }} - {{ url }}`
- Facebook/LinkedIn: `{{ title }}\n\n{{ description }}\n\n{{ url }}`

---

## Fallback Logic (Per Platform)

### Twitter and Bluesky (return `string?`)

```
1. Load template: messageTemplateDataStore.GetAsync("Twitter"/"Bluesky", "RandomPost")
2. If template.Template is not null/whitespace → call TryRenderTemplateAsync
3. If render succeeds (non-null, non-whitespace) → use rendered string as post text
4. If render returns null (no template / error / empty) → existing switch/case fallback runs
   (GetPostForSyndicationSource / GetPostForYouTubeSource / GetPostForEngagement / GetPostForTalk)
```

The existing `GetPost*` helpers are **completely unchanged** and still present as the fallback.

### Facebook (return `FacebookPostStatus?`)

```
1. Always run existing switch → populates facebookPostStatus.StatusText AND .LinkUri
2. Load template: messageTemplateDataStore.GetAsync("Facebook", "RandomPost")
3. If template exists → call TryRenderTemplateAsync
4. If render succeeds → override facebookPostStatus.StatusText with rendered text
5. LinkUri is always from the item (never overridden)
```

Rationale: Facebook requires both a text body AND a link URL. The switch is always needed for LinkUri; the template only replaces the text portion.

### LinkedIn (return `LinkedInPostLink?`)

```
1. Always run existing switch → populates linkedInPost.Title AND .LinkUrl
2. Load template: messageTemplateDataStore.GetAsync("LinkedIn", "RandomPost")
3. If template exists → call TryRenderTemplateAsync → store as renderedText
4. linkedInPost.Text = renderedText ?? scheduledItem.Message
5. AuthorId and AccessToken set from linkedInApplicationSettings as before
```

Fallback is `scheduledItem.Message` (the pre-stored message on the scheduled item), matching the original behavior.

---

## TryRenderTemplateAsync (shared pattern in all 4 functions)

Each function has a private `TryRenderTemplateAsync(ScheduledItem scheduledItem, string templateContent)` method that:

1. Loads the referenced item via the appropriate manager based on `scheduledItem.ItemType`
2. Maps item properties to `title`, `url`, `description`, `tags`
3. Parses and renders via Scriban: `Template.Parse` → `ScriptObject.Import` → `TemplateContext` → `RenderAsync`
4. Returns the trimmed rendered string, or `null` if rendering fails or produces whitespace
5. Any exception is caught, logged as `LogWarning`, and returns `null` (never throws — fallback always available)

---

## ImageUrl Handling Per Platform

`ScheduledItem.ImageUrl` is passed as `image_url` in the Scriban model so templates can include it via `{{ image_url }}`.

For the queue payload (what is placed on the Azure Storage Queue), none of the 4 platform queue message models support an image URL field:

| Platform | Queue message type | ImageUrl support |
|----------|--------------------|-----------------|
| Twitter | `string?` (plain text) | ❌ Not supported in plain string queue message |
| Facebook | `FacebookPostStatus` (StatusText + LinkUri) | ❌ No image field on `FacebookPostStatus` |
| LinkedIn | `LinkedInPostLink` (Text + Title + LinkUrl + AuthorId + AccessToken) | ❌ No image field on `LinkedInPostLink` |
| Bluesky | `string?` (plain text) | ❌ Not supported in plain string queue message |

In all 4 cases, if `scheduledItem.ImageUrl` is not null/empty, a `LogInformation` message is emitted:
> `"ImageUrl '{ImageUrl}' is available for scheduled item {Id} but is not supported in the {Platform} queue payload"`

No exception is thrown and the broadcast proceeds normally. A future issue can add image support when the queue message schemas are extended.

---

## DI Registration

Added to `ConfigureFunction` in `Program.cs`:

```csharp
services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();
```

Placed after the existing `TokenRefresh` registrations. Uses `TryAddScoped` consistent with all other data store registrations in the Functions project.

---

## Build Result

`dotnet build` — **Build succeeded, 0 errors**. All warnings are pre-existing nullable reference / XML doc warnings unrelated to this change.

# Decision: Engagement Duplicate Detection (feature/engagement-dupe-detection)

**Date:** 2026-07-11
**Author:** Trinity (Backend Dev)
**Branch:** `feature/engagement-dupe-detection`

## Context

`LoadNewSpeakingEngagements` is a timer-triggered Azure Function that pulls engagements from an
external reader and saves them to the database. Running it repeatedly (e.g. on redeploy or manual
trigger) would re-insert the same engagements, causing duplicate rows.

## Natural Key Chosen

| Field | Rationale |
|-------|-----------|
| `Name` | Title of the speaking engagement |
| `Url` | Canonical event URL — unique per event |
| `StartDateTime.Year` | Scopes collisions to the same calendar year |

Combined: **Name + Url + Year** — this mirrors the existing `GetByNameAndUrlAndYearAsync` already
present on `IEngagementDataStore` and `EngagementManager` from a previous sprint. No new query
method was needed.

## Detection Approach

"Check then skip" in the Function, before the save pipeline:

```csharp
var existingEngagement = await engagementManager.GetByNameAndUrlAndYearAsync(
    item.Name, item.Url, item.StartDateTime.Year);
if (existingEngagement != null)
{
    logger.LogDebug("Skipping duplicate speaking engagement '{Name}' ({Url}, {Year})", ...);
    continue;
}
```

- Duplicates are **skipped** (not upserted) — re-running the collector is now idempotent.
- Logged at **Debug** level (low-noise, appropriate for an expected skip path).
- Pattern matches `LoadNewPosts` (SyndicationFeed) and `LoadNewVideos` (YouTube) collectors.

## Files Changed

| File | Change |
|------|--------|
| `Domain/Interfaces/IEngagementManager.cs` | Added `GetByNameAndUrlAndYearAsync` to interface (was implemented but not exposed) |
| `Functions/Collectors/SpeakingEngagement/LoadNewSpeakingEngagements.cs` | Added duplicate check + skip before `SavePipeline.ExecuteAsync`; removed TODO comment |
| `Functions.Tests/Collectors/LoadNewSpeakingEngagementsTests.cs` | New — 3 tests covering duplicate-skip, new-save, and no-items paths |

## Why Not Upsert?

`EngagementManager.SaveAsync` already does an implicit "find by natural key and update" when
`entity.Id == 0`. The collector does not need to update existing engagements — if the reader
returns a known engagement, the correct behavior is to skip it so that any manual edits made via
the Web UI are preserved.

## Test Count

3 new unit tests in `JosephGuadagno.Broadcasting.Functions.Tests.Collectors.LoadNewSpeakingEngagementsTests`.

# Trinity Decision Note: ImageUrl Support in Queue Payloads (S4-3)

## Date
2025-01-27

## Context
Issue #269 added `ImageUrl` to `ScheduledItem` (domain + DB column) and exposed it in Scriban templates. However, the queue message models for all 4 platforms did not carry the field, and each platform's sender function logged "ImageUrl not supported" instead of using it. This work closes that gap.

---

## What Was Implemented Per Platform

### Twitter

**Queue model**: Created new `TwitterTweetMessage` (in `Domain.Models.Messages`) with `Text` and `ImageUrl` properties, replacing the plain `string` queue payload.

**Sender functions updated** to return `TwitterTweetMessage?`:
- `Twitter/ProcessScheduledItemFired.cs` — sets `ImageUrl = scheduledItem.ImageUrl`
- `Twitter/ProcessNewSyndicationDataFired.cs` — wraps text in `TwitterTweetMessage { Text = ... }`
- `Twitter/ProcessNewYouTubeData.cs` — same
- `Twitter/ProcessNewRandomPost.cs` — same (no ImageUrl source in these flows)

**Receiver** (`Twitter/SendTweet.cs`): Now accepts `TwitterTweetMessage` instead of `string`. When `ImageUrl` is set, logs a warning that Twitter media API upload is not yet implemented and posts the tweet text without an image attachment.

**Deferred**: Actual image attachment via the Twitter v1.1 media API (`POST media/upload`) is not implemented. The current `ITwitterManager`/`TwitterManager` (LinqToTwitter) only calls `SendTweetAsync(string text)`. Full attachment would require: download image bytes → POST to `media/upload` → get `media_id` → pass `media_ids` in tweet POST.

---

### Facebook

**Queue model**: Added `ImageUrl?` to `FacebookPostStatus` (in `Domain.Models.Messages`).

**Sender function** (`Facebook/ProcessScheduledItemFired.cs`): Sets `facebookPostStatus.ImageUrl = scheduledItem.ImageUrl`. Non-scheduled senders leave `ImageUrl = null`.

**Manager**: Added `PostMessageLinkAndPictureToPage(message, link, picture)` to `IFacebookManager` and `FacebookManager`. This appends `&picture={encoded_url}` to the Graph API `/feed` POST. Facebook uses this parameter as the link-preview thumbnail override.

**Receiver** (`Facebook/PostPageStatus.cs`): When `ImageUrl` is set, calls `PostMessageLinkAndPictureToPage`; otherwise calls `PostMessageAndLinkToPage` (unchanged).

**Note**: The Graph API `picture` parameter overrides the link thumbnail (OG image) in the feed post preview. It does not create a separate "photo post" — that would require `/{page_id}/photos`. The current approach is the simplest integration that attaches an image to a link post without breaking the existing flow.

---

### LinkedIn

**Queue model**: Added `ImageUrl?` to `LinkedInPostLink` (in `Domain.Models.Messages`).

**Sender function** (`LinkedIn/ProcessScheduledItemFired.cs`): Sets `linkedInPost.ImageUrl = scheduledItem.ImageUrl`. Non-scheduled senders leave `ImageUrl = null`.

**Receiver** (`LinkedIn/PostLink.cs`):
- Added `HttpClient httpClient` to constructor (consistent with existing `PostImage.cs`).
- When `ImageUrl` is set: downloads image bytes via `HttpClient`, calls `PostShareTextAndImage` (existing `ILinkedInManager` method) — this is a full image post.
- On image download failure: logs error and falls back to `PostShareTextAndLink`.
- When `ImageUrl` is null: calls `PostShareTextAndLink` (unchanged behavior).

**No manager changes required** — `ILinkedInManager.PostShareTextAndImage` was already present.

---

### Bluesky

**Queue model**: Added `ImageUrl?` to `BlueskyPostMessage` (in `Managers.Bluesky.Models`).

**Sender function** (`Bluesky/ProcessScheduledItemFired.cs`):
- **Breaking fix**: Changed return type from `string?` to `BlueskyPostMessage?`. The original code sent a plain `string` to the queue but `SendPost.cs` expected `BlueskyPostMessage` — a pre-existing type mismatch that would cause runtime deserialization failures.
- Now returns `BlueskyPostMessage { Text = ..., Url = sourceUrl, ImageUrl = scheduledItem.ImageUrl }`.
- Added `GetSourceUrlAsync()` helper to fetch the canonical URL from the source item (used by the embed path).

**Manager**: Added `GetEmbeddedExternalRecordWithThumbnail(externalUrl, thumbnailImageUrl)` to `IBlueskyManager` and `BlueskyManager`. Behaves like `GetEmbeddedExternalRecord` but skips the og:image fetch from the page and instead downloads `thumbnailImageUrl` directly to upload as the card blob thumbnail.

**Receiver** (`Bluesky/SendPost.cs`):
- When `ShortenedUrl` + `Url` are set AND `ImageUrl` is set: uses `GetEmbeddedExternalRecordWithThumbnail(Url, ImageUrl)` to build the link card with the explicit thumbnail.
- When `ShortenedUrl` + `Url` are set, no `ImageUrl`: uses `GetEmbeddedExternalRecord(Url)` (original behavior).
- When `Url` + `ImageUrl` are set (no `ShortenedUrl`): uses `GetEmbeddedExternalRecordWithThumbnail(Url, ImageUrl)` — this covers the scheduled-item path.

**Deferred**: Standalone image embedding (Bluesky `app.bsky.embed.images` record type) — posting an image without a link card — would require a new `IBlueskyManager.UploadImageAndEmbed(imageUrl)` method that uploads the blob and builds an `EmbedImages` record for the `PostBuilder`. Not implemented as the current use case always has a source URL.

---

## Manager Capability Gaps Discovered

| Platform  | Gap                                                                                                           | Effort to close                                                                |
|-----------|---------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------|
| Twitter   | `ITwitterManager.SendTweetAsync` only accepts text; no media upload                                           | Extend with `SendTweetWithImageAsync(text, imageUrl)` using LinqToTwitter media API |
| Facebook  | `PostMessageLinkAndPictureToPage` uses the legacy `picture` param; cannot create a true "photo post" on page | Add `PostPhotoToPage(message, imageUrl)` calling `/{page_id}/photos`           |
| LinkedIn  | ✅ Full image posting already supported via `PostShareTextAndImage`                                            | None                                                                           |
| Bluesky   | No standalone image embed (without a link card)                                                               | Add `UploadImageAndEmbed` to `IBlueskyManager` using `app.bsky.embed.images`  |

## Test Fixes

- `Twitter/ProcessScheduledItemFiredTests.cs`: Updated 5 assertions from `result` (was `string`) to `result?.Text` / `result!.Text` following the `TwitterTweetMessage` return-type change.
- `Bluesky/ProcessScheduledItemFiredTests.cs`: Same pattern for `BlueskyPostMessage`.

---

# Sprint 7 & 8 Planning Decisions

**Date:** 2026-03-20  
**Author:** Neo (Lead)  
**Context:** Planning Sprint 7 and Sprint 8 after Sprint 6 completion

## Sprint 7: Message Templating & Testing Foundations

**Theme:** Implement the message templating engine using Scriban (already added to repo) and establish testing infrastructure for critical collectors.

**Issues assigned:** 6 issues (#474, #475, #476, #477, #478, #302)

### Rationale

1. **Templating is ready to implement** — Scriban was added in a recent commit, and issue #474 explicitly states "Now that we have Scriban in the repository, we can create custom templated messages." This is the natural next step.

2. **Parallel platform work** — The 4 platform-specific templating issues (#475-478) can be worked independently by different team members, making this sprint highly parallelizable.

3. **Testing foundation** — Issue #302 (create JsonFeedReader.Tests project) addresses a gap where an entire project has no test coverage. This is a low-hanging fruit that establishes good habits before tackling larger test efforts.

4. **No blockers** — None of these issues depend on the remaining Sprint 6 PR (#500, HTTP security headers for Web).

## Sprint 8: API Improvements, Security Hardening, & Infrastructure

**Theme:** Prepare the API for external integrations by adding DTOs, pagination, and REST compliance, while hardening security across the stack.

**Issues assigned:** 7 issues (#315, #316, #317, #303, #336, #328, #335)

### Rationale

1. **API readiness cluster** — Issues #315 (DTOs), #316 (pagination), and #317 (REST conventions) form a coherent "make the API production-ready" theme. These are prerequisites for external consumers and should be tackled together.

2. **Security hardening continues Sprint 6 work** — Sprint 6 delivered HTTP security headers for the Web (#412, #417). Sprint 8 extends this to the API (#303) and adds cookie security (#336), completing the security header story.

3. **Observability enablement** — Issue #328 (Application Insights) is critical for production monitoring. It's currently stubbed out but not wired up; Sprint 8 activates it.

4. **CI hygiene** — Issue #335 (vulnerable NuGet package scanning) complements the security work and should be automated sooner rather than later.

5. **Balanced sprint** — 7 issues is within the 5-7 target range and mixes API work (3 issues), security (2 issues), and infrastructure (2 issues).

## Sequencing Notes

- **Sprint 7 first** — Templating work is user-facing value (better social media messages) and tests improve confidence. No dependencies on Sprint 6 completion.
  
- **Sprint 8 second** — API and security hardening are foundational work that will benefit all future features. The API improvements (#315-317) should be done before adding more endpoints.

## Issues Deliberately Deferred

The following high-value issues were reviewed but not planned into Sprint 7 or 8:

| # | Title | Reason for deferral |
|---|-------|---------------------|
| 300 | test: add unit tests for all Azure Function collectors | Larger effort; plan after #302 establishes the pattern |
| 301 | test: add unit tests for Facebook, LinkedIn, Bluesky publisher Functions | Same as #300 — defer until testing patterns are proven |
| 304 | feat(api): add rate limiting to the API | Important but should come after API DTOs/pagination (#315-316) |
| 306 | fix(web): validation script path bug | Already fixed in Sprint 6 (#415) — this may be a duplicate |
| 307 | feat(web): implement real calendar widget | Lower priority than API/security work |
| 308 | feat(web): add TempData feedback on all forms | Already done in Sprint 6 (#417) — this may be a duplicate |
| 309 | refactor: adopt IOptions<T> pattern | Good refactor but not blocking any features |
| 310 | refactor: EventPublisher failure semantics | Architectural improvement; defer until more event usage patterns emerge |
| 311 | feat: add CancellationToken propagation | Async hygiene; important but not urgent |
| 312 | feat: introduce Result<T> pattern in Managers | Architectural change; should be its own focused sprint |
| 313 | feat: add health checks | Important for production; plan after App Insights is wired (#328) |
| 314 | refactor: deduplicate Serilog config | Tech debt; low urgency |
| 318 | feat(api): wire up granular OAuth2 scopes | Depends on API DTOs (#315) being in place first |
| 319 | feat(functions): add retry policies and DLQ | Infrastructure hardening; plan after core features are stable |
| 321 | fix(bluesky): cache auth session | Performance optimization; defer until Bluesky usage increases |
| 322-325 | Database improvements (NVARCHAR lengths, Tags normalization, pagination, 50MB cap) | Cluster these into a "Database Sprint" later |
| 326 | feat(ci): CodeQL scanning | Good CI hygiene but lower priority than #335 (vulnerable packages) |
| 327 | feat(aspire): add Event Grid topics to AppHost | Decisions.md shows this was already done by Cypher (2026-03-18) — check if issue can be closed |
| 329 | feat(ci): staging deployment slots | DevOps maturity; defer until deployment pipeline is more established |
| 330-331 | More unit tests | Plan after Sprint 7's #302 and Sprint 8's foundation work |
| 332-334 | Web UI improvements (accessibility, loading states, pagination) | User experience polish; defer until API work is done |

## Milestone Links

- **Sprint 7:** https://github.com/jguadagno/jjgnet-broadcast/milestone/2
- **Sprint 8:** https://github.com/jguadagno/jjgnet-broadcast/milestone/3

## Notes

- Sprint 6 has 1 remaining open PR (#500) which is the HTTP security headers for Web. This should be merged before starting Sprint 7.
  
- Issues #306 and #308 appear to duplicate Sprint 6 work (#415 and #417). Recommend reviewing these for closure.

- Issue #327 (Event Grid topics in AppHost) appears completed per decisions.md (Cypher, 2026-03-18). Recommend verifying and closing.

---

# Oracle Decision Record: HTTP Security Headers Middleware (S6-6, Issue #303)

## Date
2026-03-19

## Author
Oracle (Security Engineer)

## Status
Pending Ghost review for CSP allowlist

---

## Context

Both the API and Web applications were missing standard HTTP security response headers, leaving
responses vulnerable to clickjacking, MIME sniffing, and cross-site scripting. Issue #303 requires
adding the full recommended header set to every response in both projects.

---

## Decisions

### 1. Implementation approach — inline `app.Use` middleware

Used `app.Use(async (context, next) => { ... })` in each `Program.cs` rather than a third-party
package (`NWebsec`, `NetEscapades.AspNetCore.SecurityHeaders`). Rationale: zero new dependencies,
the header set is small and stable, and the policy strings are clearly readable in one place. If
the policy grows significantly, migrating to `NetEscapades.AspNetCore.SecurityHeaders` is a low-cost
future refactor.

Middleware is placed **after** `UseHttpsRedirection()` so headers are only emitted on HTTPS
responses and are not duplicated on redirect responses.

### 2. Headers applied — API (`JosephGuadagno.Broadcasting.Api`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `DENY` | API has no legitimate iframe use; strictest setting |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy browser XSS auditor (superseded by CSP) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage on cross-origin navigation |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | API serves JSON only; no scripts/styles/frames needed. `frame-ancestors 'none'` reinforces DENY framing |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | Disable browser features not required by a REST API |

### 3. Headers applied — Web (`JosephGuadagno.Broadcasting.Web`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `SAMEORIGIN` | MVC app may legitimately frame its own pages (e.g. OAuth popups) |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy XSS auditor |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage |
| `Content-Security-Policy` | See §4 below | |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | No browser hardware features used |

### 4. Web Content-Security-Policy rationale

**Policy:**
```
default-src 'self';
script-src 'self' cdn.jsdelivr.net;
style-src 'self' cdn.jsdelivr.net;
img-src 'self' data: https:;
font-src 'self' cdn.jsdelivr.net data:;
connect-src 'self';
frame-ancestors 'self';
object-src 'none';
base-uri 'self';
form-action 'self'
```

**Directive-by-directive rationale:**

- **`default-src 'self'`** — safe fallback; anything not explicitly listed must come from the
  same origin.
- **`script-src 'self' cdn.jsdelivr.net`** — `'self'` covers all local JS bundles (jQuery,
  Bootstrap, site.js, schedules.edit.js, theme-support.js, the two new externalized scripts).
  `cdn.jsdelivr.net` is required in production for jQuery, Bootstrap bundle, FontAwesome JS,
  jquery-validation, and FullCalendar. No `'unsafe-inline'` — inline scripts were externalized
  (see §5).
- **`style-src 'self' cdn.jsdelivr.net`** — `cdn.jsdelivr.net` required in production for
  Bootstrap CSS, Bootstrap Icons CSS, and FontAwesome CSS. No `'unsafe-inline'` — the one inline
  `<style>` block in Calendar.cshtml was moved to `site.css`.
- **`img-src 'self' data: https:`** — `'self'` covers `/favicon.ico` and local images.
  `data:` is required for Bootstrap Icons (inline SVG data-URIs in the CSS). `https:` covers
  `@Settings.StaticContentRootUrl` favicon images whose exact hostname is a runtime setting
  (see open question §6).
- **`font-src 'self' cdn.jsdelivr.net data:`** — `cdn.jsdelivr.net` for FontAwesome woff2/woff
  files. `data:` covers any base64-encoded font fallbacks in vendor CSS.
- **`connect-src 'self'`** — all XHR/fetch calls go to the same origin (Engagements calendar
  events endpoint, API calls proxied by the Web app).
- **`frame-ancestors 'self'`** — paired with `X-Frame-Options: SAMEORIGIN`; allows same-origin
  framing, denies cross-origin.
- **`object-src 'none'`** — no Flash/plugin content.
- **`base-uri 'self'`** — prevents base tag injection attacks.
- **`form-action 'self'`** — all form POSTs must target the same origin.

### 5. Inline script/style externalization

Two inline `<script>` blocks were moved to dedicated JS files to avoid needing `'unsafe-inline'`
in `script-src`:

- `Views/MessageTemplates/Index.cshtml` → `wwwroot/js/message-templates-index.js`
  (Bootstrap tooltip initializer)
- `Views/Schedules/Calendar.cshtml` → `wwwroot/js/schedules-calendar.js`
  (FullCalendar initializer; no server-side data injection — uses an AJAX endpoint)

One inline `<style>` block from `Views/Schedules/Calendar.cshtml` (`#calendar` sizing) was moved
to `wwwroot/css/site.css`.

### 6. Open Questions for Ghost Review

1. **`img-src https:`** — This broad allowance was chosen because `Settings.StaticContentRootUrl`
   (used for favicons) is a runtime configuration value with an unknown hostname at code-time.
   Ghost should evaluate whether this should be tightened to the known static asset host
   (e.g., `https://static.josephguadagno.net`) and potentially read from config at startup.

2. **`cdn.jsdelivr.net` scope** — All CDN assets are pinned with SRI `integrity=` hashes in
   the Production `<environment>` blocks. The CSP host allowance is a belt-and-suspenders
   measure. Ghost should confirm no other CDN hostnames are referenced in any partial views
   not covered by this review.

3. **Nonce-based CSP** — A future improvement would replace the `cdn.jsdelivr.net` allowance
   with per-request nonces, eliminating CDN host trust entirely. Out of scope for S6-6.

---

## Files Changed

- `src/JosephGuadagno.Broadcasting.Api/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/message-templates-index.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/schedules-calendar.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/css/site.css` — calendar style appended
- `src/JosephGuadagno.Broadcasting.Web/Views/MessageTemplates/Index.cshtml` — inline script removed
- `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Calendar.cshtml` — inline script and style removed

---

## 2026-03-20: Branch + PR required for all work
All team work must use a feature branch and PR. Direct commits to main are not allowed.
Applies to: all agents, all work types (code, SQL migrations, config changes).


## 2026-03-20: Sprint 9 Planning — Test Coverage Expansion
# Neo Decision: Sprint 9 Plan

**Date:** 2026-03-20  
**Decision by:** Neo (Lead)  
**Context:** Sprint planning for Sprint 9, following Sprint 7 (Message Templating) and Sprint 8 (API/Security)

## Decision

**Sprint 9 Theme:** Test Coverage Expansion — comprehensive unit tests for Azure Functions (collectors & publishers), manager business logic, and removal of external test dependencies.

**Milestone:** [Sprint 9](https://github.com/jguadagno/jjgnet-broadcast/milestone/4)

**Issues assigned:**

| # | Title | Labels | Why |
|---|-------|--------|-----|
| #300 | test: add unit tests for all Azure Function collectors | azure-functions, testing, priority: high | Collectors are untested; need mocked infrastructure for RSS/YouTube feeds |
| #301 | test: add unit tests for Facebook, LinkedIn, and Bluesky publisher Functions | azure-functions, testing, priority: high | Publishers are untested; need mocked social API clients |
| #330 | test: add real logic tests for EngagementManager (timezone correction, deduplication) | .NET, testing, priority: high | EngagementManager has complex business logic that needs coverage |
| #331 | test: add local unit tests to SyndicationFeedReader.Tests — remove network dependency | .NET, testing, priority: high | Current tests hit external URLs; need local mocked tests for CI stability |
| #319 | feat(functions): add retry policies and dead-letter queue handling to host.json | azure-functions, priority: medium | Functions reliability improvement; complements testing work |

## Rationale

1. **Follows Sprint 7/8 progression:** Sprint 7 establishes the first test project (#302), Sprint 8 hardens API/security, Sprint 9 expands test coverage across the board.

2. **Cohesive theme:** All 5 issues focus on Azure Functions reliability — 4 are direct testing improvements, and #319 adds production-ready error handling (retries + DLQ) to the Functions host configuration.

3. **High priority cluster:** Testing Cluster was identified in Sprint 7/8 planning as a deferred high-priority cluster. All 4 testing issues are marked "priority: high" and are ready to execute.

4. **Reduces flaky tests:** #331 specifically addresses the network-dependent tests that fail in CI/sandboxed environments (noted in the repository's custom instructions).

5. **Balanced scope:** 5 issues is appropriate for a sprint focused on testing — no external dependencies, all work is internal to the test suite and Functions configuration.

## Deferred to Later Sprints

- **Database Improvements Cluster** (#322-325): Deferred to Sprint 10 or 11 — larger architectural change requiring schema migrations and data migrations.
- **Architectural Refactors** (#309-312, #314): Deferred to dedicated refactor sprint — significant code changes across multiple layers (Managers, Data, logging).

## Next Steps

1. Sprint 9 milestone created and issues assigned
2. Sprint 7 and 8 remain open for execution
3. Database and refactor work remains in backlog for future sprint planning



## 2026-03-20: JsonFeedReader Implementation Pattern
# Decision: JsonFeedReader Implementation Pattern

**Date:** 2026-03-20  
**Author:** Tank (Tester)  
**Context:** Issue #302 - Create JsonFeedReader.Tests project  
**PR:** #501

## Problem

Issue #302 requested creation of JsonFeedReader.Tests, but the JsonFeedReader implementation project didn't exist — only an empty directory with build artifacts. Blocker documented in issue comment.

## Decision

Created minimal JsonFeedReader implementation using TDD approach to unblock test creation, following established SyndicationFeedReader pattern.

## Implementation Choices

### 1. JSON Parsing Library
**Chosen:** System.Text.Json (built-in)  
**Rejected:** JsonFeed.NET (namespace/compatibility issues with .NET 10)

**Rationale:** System.Text.Json provides sufficient functionality for JSON feed parsing without external dependencies. Private model classes (JsonFeedModel, JsonFeedItem, JsonFeedAuthor) handle deserialization. This keeps the implementation simple and maintainable.

### 2. Project Structure
Mirrored SyndicationFeedReader exactly:
- `Interfaces/` - IJsonFeedReader, IJsonFeedReaderSettings
- `Models/` - JsonFeedReaderSettings
- `JsonFeedReader.cs` - Main implementation

**Rationale:** Consistency across reader implementations. New developers can pattern-match against SyndicationFeedReader.

### 3. Domain Model
Created `JsonFeedSource` in `Domain/Models/` mirroring `SyndicationFeedSource` structure.

**Properties:**
- Id, FeedIdentifier, Author, Title, Url, Tags
- PublicationDate, AddedOn, LastUpdatedOn, ItemLastUpdatedOn
- All match SyndicationFeedSource for consistency

### 4. Test Coverage Strategy
**Unit Tests (JsonFeedReader.Tests):**
- Constructor validation (4 tests)
- NO network calls

**Integration Tests:**
- Deferred to future JsonFeedReader.IntegrationTests project
- Follows SyndicationFeedReader.IntegrationTests pattern

**Rationale:** Unit tests should be fast, reliable, and not dependent on external services. Integration tests belong in separate project.

### 5. Constructor Validation
Strict validation matches SyndicationFeedReader:
- Null settings → ArgumentNullException
- Null/Empty FeedUrl → ArgumentNullException

**Rationale:** Fail fast on misconfiguration. Clear error messages guide developers.

## Test Results

All 4 tests passing:
- ✅ Constructor_WithValidParameters_ShouldNotThrowException
- ✅ Constructor_WithNullFeedSettings_ShouldThrowArgumentNullException  
- ✅ Constructor_WithFeedSettingsUrlNull_ShouldThrowArgumentNullException
- ✅ Constructor_WithFeedSettingsUrlEmpty_ShouldThrowArgumentNullException

## Future Work

1. **Integration Tests:** Create JsonFeedReader.IntegrationTests with real feed URLs (test against josephguadagno.net/feed.json if available)
2. **Error Handling Tests:** Malformed JSON, empty feed, missing required fields
3. **Function Collector:** Create LoadJsonFeedItems Azure Function (infrastructure-needs.md references collectors_feed_load_json_feed_items)

## Applies To

- JsonFeedReader implementation
- JsonFeedReader.Tests
- Future JSON feed-related features



---

# Decision: DTO Mapping Pattern for API Controllers

**Author:** Trinity  
**Date:** 2026-03-21  
**Related PR:** #512 (`feature/s8-315-api-dtos`)

## Decision

DTO mapping in API controllers uses private static helper methods (`ToResponse` / `ToModel`) co-located in the controller class — no AutoMapper or external mapping library.

## Pattern

```csharp
// In controller class:
private static EngagementResponse ToResponse(Engagement e) => new() { ... };
private static Engagement ToModel(EngagementRequest r, int id = 0) => new() { ... };
```

For **update** endpoints, the route `id` is injected at the `ToModel` call site:
```csharp
var engagement = ToModel(request, engagementId);  // id from route, not from DTO
```

This eliminates the "route id must match body id" validation check — the DTO simply doesn't carry an `Id` field.

## Rationale

1. **Zero new dependencies** — consistent with how `MessageTemplatesController` was already implemented.
2. **Co-location is readable** — helpers are at the bottom of the controller, easy to find.
3. **Route id as ground truth** — the route parameter is authoritative; no need to repeat it in the request body.

## Scope

Applies to: `EngagementsController`, `SchedulesController`, `MessageTemplatesController` (already done).  
Future controllers should follow the same pattern unless a compelling reason exists to introduce a mapping library.


---

# Decision: Request DTOs Must NOT Include Route Parameters

**Author:** Neo  
**Date:** 2026-03-21  
**Related PR:** #512 review (`feature/s8-315-api-dtos`)

## Decision

Request DTOs must **never** include properties that are provided via route parameters. Route parameters are the single source of truth for entity identifiers and other URL-based values.

## Violation Found

In PR #512, `TalkRequest.cs` includes:
```csharp
[Required]
public int EngagementId { get; set; }
```

But the controller route is:
```csharp
[HttpPost("{engagementId:int}/talks")]
public async Task<ActionResult<TalkResponse>> CreateTalkAsync(int engagementId, TalkRequest request)
```

The `engagementId` comes from the route, not the request body. The DTO property is:
- **Misleading**: API consumers might think they need to provide it in the JSON body
- **Redundant**: The controller correctly ignores the DTO property and uses `ToModel(request, engagementId)` (route parameter)
- **Violates ground truth principle**: Route parameter is authoritative, not the DTO

## Rationale

1. **Single source of truth**: Route parameters are part of the URL (RESTful resource identifier) and must not be duplicated in the request body
2. **Clear API contract**: DTOs should only include data that comes from the request body, not from URL components
3. **Prevents confusion**: Having the same value in two places (URL and body) creates ambiguity and requires validation logic to ensure they match
4. **Consistency**: This aligns with the broader DTO pattern decision where route IDs eliminated the need for "route id must match body id" checks

## Correct Pattern

### ✅ Good (current `EngagementRequest`)
```csharp
public class EngagementRequest
{
    [Required] public string Name { get; set; }
    [Required] public DateTimeOffset StartDateTime { get; set; }
    // No Id property — comes from route in PUT /engagements/{id}
}
```

### ❌ Bad (PR #512's `TalkRequest`)
```csharp
public class TalkRequest
{
    [Required] public string Name { get; set; }
    [Required] public int EngagementId { get; set; }  // ← WRONG: route provides this
}
```

### ✅ Good (corrected `TalkRequest`)
```csharp
public class TalkRequest
{
    [Required] public string Name { get; set; }
    // EngagementId removed — route provides it in POST /engagements/{engagementId}/talks
}
```

## Scope

Applies to all Request DTOs in the API layer. Response DTOs **may** include IDs since they represent the full resource state being returned to the client.

## Review Checklist for Future PRs

When reviewing DTO PRs, verify:
- [ ] Request DTOs do not include route parameters as properties
- [ ] Controller `ToModel` mapping uses route parameters, not DTO properties, for IDs
- [ ] No "route id must match body id" validation checks exist


---

### 2026-03-19T21-16-29Z: User directive
**By:** Joseph Guadagno (via Copilot)
**What:** When a PR is merged, delete the local branch in addition to the remote branch. Agents must run `git branch -d {branch-name}` after every `gh pr merge --delete-branch`. Also set `git config fetch.prune true` so remote tracking refs are pruned on fetch.
**Why:** User request — keep local workspace clean after merges

---

# Ralph's Triage and Audit Report
**Date:** 2026-03-19  
**Reporter:** Ralph (Work Monitor)  
**Requested by:** Joseph Guadagno

---

## Summary

**Part 1 - Untriaged Backlog:** No untriaged squad issues found. All issues with the `squad` label already had `squad:{member}` labels.

**Part 2 - Issues Below #201 Audit:** Reviewed 5 open issues numbered below 201. Closed 1 resolved issue, triaged 4 still-relevant issues.

---

## Part 1: Triage of Untriaged Backlog Issues

### Finding
There were **zero untriaged issues** with the `squad` label but no `squad:{member}` label. All squad-tracked work has been properly routed.

### Action Taken
Created the complete set of squad labels for future use:
- `squad` - General squad tracking label (triage required)
- `squad:neo` - Lead (Architecture, decisions, review)
- `squad:trinity` - Backend Dev (API, Azure Functions, business logic)
- `squad:morpheus` - Data Engineer (SQL Server, Table Storage, EF Core)
- `squad:tank` - Tester (xUnit, Moq, FluentAssertions)
- `squad:switch` - Frontend Engineer (MVC controllers, ViewModels)
- `squad:sparks` - Frontend Developer (Razor views, LibMan, Bootstrap, CSS/JS)
- `squad:ghost` - Security & Identity (OAuth2/OIDC, auth middleware, MSAL)
- `squad:oracle` - Security Engineer (Azure AD, Key Vault, secrets)
- `squad:cypher` - DevOps Engineer (.NET Aspire, Bicep, local dev)
- `squad:link` - Platform & DevOps (GitHub Actions, Event Grid, Azure deployment)

---

## Part 2: Audit of Issues Numbered Below #201

### Issues Closed (1)

#### #200 - LoadAllSpeakingEngagements and LoadNewSpeakingEngagements are not populating talks
**Status:** ✅ CLOSED  
**Reason:** Already resolved  
**Evidence:** Code review of `SpeakingEngagementsReader.cs` (lines 76-93) shows that talks are now being populated from the Presentations collection. The logic iterates through `speakingEngagement.Presentations` and adds each talk to `engagement.Talks` with proper field mapping (Name, Url, StartDateTime, EndDateTime, Room, Comments).

---

### Issues Triaged (4)

#### #198 - Create an event in EventGrid for when a SpeakingEngagement is added
**Status:** 🔄 STILL RELEVANT  
**Assigned to:** Trinity (squad:trinity)  
**Reasoning:** This is about Azure Functions and EventGrid integration. The infrastructure is partially in place:
- ✅ EventGrid topic `new-speaking-engagement` is defined in Topics.cs
- ✅ EventPublisher has `PublishNewSpeakingEngagementEventsAsync` method
- ✅ EventGrid simulator config defines subscribers for all 4 platforms
- ❌ The actual subscriber functions (BlueskyProcessSpeakingEngagementDataFired, FacebookProcessSpeakingEngagementDataFired, LinkedInProcessSpeakingEngagementDataFired, TwitterProcessSpeakingEngagementDataFired) **do not exist**

The issue requests publishers for Bluesky, Facebook, LinkedIn, and Twitter/X. Trinity owns Azure Functions and should implement these EventGrid subscriber functions.

---

#### #191 - Update the site privacy page
**Status:** 🔄 STILL RELEVANT  
**Assigned to:** Sparks (squad:sparks)  
**Reasoning:** This is a Razor view update. The file `Views/Home/Privacy.cshtml` currently contains placeholder text: "Use this page to detail your site's privacy policy." Sparks owns Razor views, static assets, and frontend content. This is a straightforward content update requiring real privacy policy text.

---

#### #170 - Add back in the fine-grained permissions to the API endpoints
**Status:** 🔄 STILL RELEVANT  
**Assigned to:** Ghost (squad:ghost)  
**Reasoning:** This issue is about OAuth2/OIDC scopes and API authorization. The description states: "The scopes should not be *.All, the *Modify, *.List, etc." This is about implementing fine-grained authorization with specific permission scopes rather than broad wildcard permissions. Ghost owns OAuth2/OIDC flows, token lifecycle, auth middleware, and MSAL integration. Recent commits show some scope-related work (PR #487 "Update identity scopes and refine MessageTemplates view"), but without a detailed review of all API endpoints, it's unclear if this is fully resolved. Ghost should audit API authorization attributes and implement granular scopes.

---

#### #167 - For an engagement, we should add the BlueSky handle
**Status:** 🔄 STILL RELEVANT  
**Assigned to:** Morpheus (squad:morpheus)  
**Reasoning:** This is a database schema and domain model change. The request is to add a BlueSky handle field to the Engagement/ScheduledItem models. Code search found no evidence of a `BlueSkyHandle` field in the domain models. Morpheus owns SQL Server, Table Storage, and EF Core — this requires:
1. Adding the field to the Engagement domain model
2. Creating a database migration (or SQL script, since this project uses raw SQL)
3. Updating the EF Core DbContext and entity configuration
4. Potentially updating the API DTOs and Web ViewModels

This is a data layer task that falls squarely in Morpheus's domain.

---

## Evidence Review

### Commits Analyzed
Reviewed last 50 commits (git log --oneline -50) for evidence of issue resolution. Key commits:
- `fbc62df` - feat: add duplicate detection to LoadNewSpeakingEngagements collector
- `361e7e9` - feat(aspire): enable all 5 Event Grid topics in local dev event-grid-simulator
- `1eac700` - feat(web): update identity scopes and improve MessageTemplates view
- Multiple Scriban template and scheduled item commits

### Merged PRs Analyzed
Reviewed last 50 merged PRs. Notable PRs:
- #514 - feat(api): add pagination to all list API endpoints
- #512 - feat(api): introduce DTOs to decouple API contract from domain models
- #511 - fix: uncomment and wire up Application Insights/Azure Monitor
- #487 - Update identity scopes and refine MessageTemplates view
- #482 - feat(aspire): enable all 5 Event Grid topics in local dev event-grid-simulator

### Code Files Examined
- `src/JosephGuadagno.Broadcasting.SpeakingEngagementsReader/SpeakingEngagementsReader.cs` - Verified talks population logic
- `src/JosephGuadagno.Broadcasting.Data/EventPublisher.cs` - Confirmed NewSpeakingEngagement topic publishing method exists
- `src/JosephGuadagno.Broadcasting.Functions/event-grid-simulator-config.json` - Verified EventGrid subscriber configuration
- `src/JosephGuadagno.Broadcasting.Web/Views/Home/Privacy.cshtml` - Confirmed placeholder text still present
- `src/JosephGuadagno.Broadcasting.Domain/Constants/Topics.cs` - Verified topic definitions

---

## Recommendations

1. **Trinity** should create the 4 EventGrid subscriber functions for new-speaking-engagement events (#198)
2. **Sparks** should replace the privacy page placeholder with real privacy policy text (#191)
3. **Ghost** should audit API endpoint authorization and implement fine-grained scopes (#170)
4. **Morpheus** should add the BlueSkyHandle field to Engagement model and database schema (#167)

All issues are now properly labeled and ready for squad members to pick up.

---

## Label System Status

✅ Squad label system fully operational  
✅ All 11 squad labels created with descriptions and color coding  
✅ Labels ready for future triage and routing  

The label system follows the routing matrix defined in `.squad/routing.md` and maps to team members in `.squad/team.md`.

---

# Ghost — Cookie Security Hardening (Issue #336)

**Date:** 2026-03-19
**Sprint:** Sprint 8
**PR:** #510

## What Was Done

Three separate cookie surfaces were hardened in `src/JosephGuadagno.Broadcasting.Web/Program.cs`:

### 1. Auth Cookie (`CookieAuthenticationOptions`)
Previously only set `Events`. Now also sets:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Lax`

*Lax is appropriate for the auth cookie — it must survive top-level cross-site navigations (e.g., OIDC redirect back from Azure AD).*

### 2. Session Cookie (`AddSession`)
Previously used `AddSession()` with no options. Now:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Lax`
- `IsEssential = true` — prevents session cookie from being blocked by GDPR middleware before consent

### 3. Antiforgery Cookie (`AddAntiforgery`)
Not previously configured at all. Added explicit:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Strict`

*Strict is correct for the antiforgery token — it never needs to be sent on cross-site requests. This provides the strongest CSRF protection.*

## Findings / Learnings

- `ImplicitUsings=enable` on the Web project means `Microsoft.AspNetCore.Http` types (`CookieSecurePolicy`, `SameSiteMode`) are available without explicit `using` statements.
- `AddAntiforgery` is called before `AddControllersWithViews` so our explicit configuration wins over the default registered by MVC.
- The `Configure<CookieAuthenticationOptions>` post-configuration pattern used by MSAL (`RejectSessionCookieWhenAccountNotInCacheEvents`) still works fine when security options are added to the same lambda.
- SameSite=Lax (not Strict) is required for the auth cookie because the OIDC `redirect_uri` is a cross-site POST from Azure AD — Strict would break login.

## Decision

> Cookie security flags must be explicitly set on all cookie surfaces (auth, session, antiforgery) rather than relying on framework defaults. This is now the pattern for this project.

---

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

---

# Decision: PR #511 CI Fix — Merge main instead of rebase

**Date:** 2025-07-14  
**Author:** Link (Platform & DevOps Engineer)  
**PR:** #511 `feature/s8-328-wire-application-insights`

## Decision

Used `git merge origin/main --no-edit` (not rebase) to bring PR #511 up to date with main after PR #513 landed.

## Rationale

- PR #511's changes are entirely in `ServiceDefaults/` and `Program.cs` files — no overlap with the controller/test renames from PR #513.
- Merge produced a clean auto-merge with no conflicts.
- Rebase was unnecessary complexity for a non-overlapping change set; merge preserves the original commit history and is less risky in a shared branch.

## Workflow conflict policy (secondary decision)

When popping stashes onto branches that have received `origin/main` updates, workflow file conflicts in `.github/workflows/*.yml` should always resolve to the `origin/main` version. The vuln-scan policy (Critical-only gate, with High/Medium/Low logged but non-blocking) was deliberately established in PR #509 and must not be regressed.

---

### 2026-03-20: Pagination parameter validation pattern
**By:** Morpheus
**What:** Paginated endpoints clamp page to min 1, pageSize to range 1-100. Applied as inline guards at the top of each list action method.
**Why:** Neo review blocked on division-by-zero (pageSize=0) and negative Skip (page=0).

---

# Neo PR #512 Re-Review Verdict

**Date:** 2026-03-21
**PR:** #512 `feature/s8-315-api-dtos`
**Original Review:** 2026-03-21 (CHANGES REQUESTED)
**Fix Author:** Morpheus
**Re-Review Author:** Neo

## Verdict: APPROVED ✅

Both blocking issues from initial review have been resolved.

## Issues Resolved

### 1. ✅ BOM Character Removed
**Original issue:** MessageTemplatesController.cs line 1 had UTF-8 BOM (U+FEFF) before first `using` statement.

**Fix verified:** Commit `9f02d429` changed line 1 from `\uFEFFusing` to clean `using`. File now clean UTF-8.

### 2. ✅ Route-as-Ground-Truth Pattern Fixed
**Original issue:** `TalkRequest.EngagementId` property violated route-as-ground-truth pattern. The route `POST /engagements/{engagementId}/talks` provides `engagementId`, so it should not be in the request body DTO.

**Fix verified:** 
- Commit `9f02d429` removed these lines from TalkRequest.cs:
  ```csharp
  [Required]
  public int EngagementId { get; set; }
  ```
- Controller ToModel calls correctly use route parameter:
  - CREATE: `var talk = ToModel(request, engagementId);`
  - UPDATE: `var talk = ToModel(request, engagementId, talkId);`
- ToModel signature: `private static Talk ToModel(TalkRequest r, int engagementId, int id = 0)`

## Pattern Compliance Verified

All 3 controllers (EngagementsController, SchedulesController, MessageTemplatesController) follow the approved DTO pattern:

1. ✅ Private static `ToResponse(DomainModel)` helpers
2. ✅ Private static `ToModel(RequestDTO, routeParams...)` helpers
3. ✅ No AutoMapper or external mapping library
4. ✅ Route parameters passed to ToModel as arguments, not from DTO
5. ✅ Request DTOs for input, Response DTOs for output
6. ✅ Proper null handling with `?.` operator (e.g., `e.Talks?.Select(ToResponse).ToList()`)
7. ✅ No "route id must match body id" validation checks

## CI Status

- ✅ GitGuardian Security Checks passed

## New Issues

None identified.

## Recommendation

**Ready to merge.** PR #512 successfully implements DTO layer pattern and closes issue #315.

## GitHub Limitation Note

Cannot formally approve PR via GitHub API because reviewer (jguadagno) is same as PR author. Posted approval verdict as comment: https://github.com/jguadagno/jjgnet-broadcast/pull/512#issuecomment-4095334205

---

### 2026-03-19T20:47:12: PR #514 pagination review verdict
**By:** Neo
**Verdict:** CHANGES REQUESTED
**Blocking issues:**
1. Division by zero in PagedResponse.TotalPages when pageSize=0
2. Negative Skip() calculation when page=0

**Why:**
The core pagination pattern is correctly implemented (PagedResponse<T> wrapper, consistent defaults, full coverage of all list endpoints, proper DTO usage). However, two edge cases will cause runtime failures:
- pageSize=0 throws DivideByZeroException in TotalPages calculation
- page=0 produces negative Skip() value, leading to misleading client behavior

These are defensive validation gaps that must be fixed before production use. Pattern compliance is otherwise excellent — no BOM issues, consistent across all 9 list endpoints.

**Remediation:** Per team protocol, Trinity (PR author) cannot fix their own rejected PR. Coordinator must assign a different agent.

---

# Neo Re-Review Verdict: PR #514 — Pagination Implementation (APPROVED)

**Date:** 2026-03-21  
**Reviewer:** Neo  
**PR:** #514 `feature/s8-316-pagination`  
**Previous Review:** 2026-03-19T20:47:12 (CHANGES REQUESTED)  
**Fixes By:** Morpheus  

## Verdict: APPROVED ✅

Both blocking edge cases from the initial review have been resolved with proper input validation guards.

## Issues Resolved

### 1. ✅ Division by Zero — FIXED
**Original Issue:** PagedResponse.TotalPages calculation (`TotalCount / PageSize`) threw DivideByZeroException when `pageSize=0`.

**Fix Applied:** All 8 paginated endpoints now validate and clamp pageSize:
```csharp
if (pageSize < 1) pageSize = 1;
if (pageSize > 100) pageSize = 100;
```

**Result:** TotalPages calculation is always safe because PageSize is guaranteed to be ≥ 1.

### 2. ✅ Negative Skip — FIXED
**Original Issue:** `Skip((page - 1) * pageSize)` produced negative values when `page=0`, causing undefined behavior.

**Fix Applied:** All 8 paginated endpoints now validate and clamp page:
```csharp
if (page < 1) page = 1;
```

**Result:** Skip calculation always receives valid positive or zero values.

## Validation Coverage (8/8 Endpoints)

All paginated list endpoints have consistent validation guards:

1. **EngagementsController.GetEngagementsAsync** — ✅ page/pageSize guards present
2. **EngagementsController.GetTalksForEngagementAsync** — ✅ page/pageSize guards present
3. **MessageTemplatesController.GetAllAsync** — ✅ page/pageSize guards present
4. **SchedulesController.GetScheduledItemsAsync** — ✅ page/pageSize guards present
5. **SchedulesController.GetUnsentScheduledItemsAsync** — ✅ page/pageSize guards present
6. **SchedulesController.GetScheduledItemsToSendAsync** — ✅ page/pageSize guards present
7. **SchedulesController.GetUpcomingScheduledItemsForCalendarMonthAsync** — ✅ page/pageSize guards present
8. **SchedulesController.GetOrphanedScheduledItemsAsync** — ✅ page/pageSize guards present

## Pattern Compliance

✅ **Consistent validation logic** across all endpoints (page: min 1, pageSize: 1-100)  
✅ **PagedResponse\<T\> wrapper** correctly used with Items, Page, PageSize, TotalCount, TotalPages  
✅ **Response DTOs** properly wrapped in PagedResponse  
✅ **No route-as-ground-truth violations** detected  
✅ **No BOM characters** in modified files  
✅ **CI passing** (GitGuardian checks successful)  

## New Issues Found

**None.** The validation fix is clean and introduces no new problems.

## Recommendation

**READY TO MERGE.** All blocking issues resolved, pattern compliance verified, CI passing.

## Next Steps

1. Merge PR #514
2. Close issue #316
3. Consider documenting the pagination pattern (min/max limits, validation approach) for future API endpoint development

---

*Note: Could not formally approve PR via `gh pr review --approve` because PR author (jguadagno) cannot approve their own PR per GitHub policy. Added approval comment to PR thread instead.*

---

# Oracle Decision Record: HTTP Security Headers Middleware (S6-6, Issue #303)

## Date
2026-03-19

## Author
Oracle (Security Engineer)

## Status
Pending Ghost review for CSP allowlist

---

## Context

Both the API and Web applications were missing standard HTTP security response headers, leaving
responses vulnerable to clickjacking, MIME sniffing, and cross-site scripting. Issue #303 requires
adding the full recommended header set to every response in both projects.

---

## Decisions

### 1. Implementation approach — inline `app.Use` middleware

Used `app.Use(async (context, next) => { ... })` in each `Program.cs` rather than a third-party
package (`NWebsec`, `NetEscapades.AspNetCore.SecurityHeaders`). Rationale: zero new dependencies,
the header set is small and stable, and the policy strings are clearly readable in one place. If
the policy grows significantly, migrating to `NetEscapades.AspNetCore.SecurityHeaders` is a low-cost
future refactor.

Middleware is placed **after** `UseHttpsRedirection()` so headers are only emitted on HTTPS
responses and are not duplicated on redirect responses.

### 2. Headers applied — API (`JosephGuadagno.Broadcasting.Api`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `DENY` | API has no legitimate iframe use; strictest setting |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy browser XSS auditor (superseded by CSP) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage on cross-origin navigation |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | API serves JSON only; no scripts/styles/frames needed. `frame-ancestors 'none'` reinforces DENY framing |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | Disable browser features not required by a REST API |

### 3. Headers applied — Web (`JosephGuadagno.Broadcasting.Web`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `SAMEORIGIN` | MVC app may legitimately frame its own pages (e.g. OAuth popups) |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy XSS auditor |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage |
| `Content-Security-Policy` | See §4 below | |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | No browser hardware features used |

### 4. Web Content-Security-Policy rationale

**Policy:**
```
default-src 'self';
script-src 'self' cdn.jsdelivr.net;
style-src 'self' cdn.jsdelivr.net;
img-src 'self' data: https:;
font-src 'self' cdn.jsdelivr.net data:;
connect-src 'self';
frame-ancestors 'self';
object-src 'none';
base-uri 'self';
form-action 'self'
```

**Directive-by-directive rationale:**

- **`default-src 'self'`** — safe fallback; anything not explicitly listed must come from the
  same origin.
- **`script-src 'self' cdn.jsdelivr.net`** — `'self'` covers all local JS bundles (jQuery,
  Bootstrap, site.js, schedules.edit.js, theme-support.js, the two new externalized scripts).
  `cdn.jsdelivr.net` is required in production for jQuery, Bootstrap bundle, FontAwesome JS,
  jquery-validation, and FullCalendar. No `'unsafe-inline'` — inline scripts were externalized
  (see §5).
- **`style-src 'self' cdn.jsdelivr.net`** — `cdn.jsdelivr.net` required in production for
  Bootstrap CSS, Bootstrap Icons CSS, and FontAwesome CSS. No `'unsafe-inline'` — the one inline
  `<style>` block in Calendar.cshtml was moved to `site.css`.
- **`img-src 'self' data: https:`** — `'self'` covers `/favicon.ico` and local images.
  `data:` is required for Bootstrap Icons (inline SVG data-URIs in the CSS). `https:` covers
  `@Settings.StaticContentRootUrl` favicon images whose exact hostname is a runtime setting
  (see open question §6).
- **`font-src 'self' cdn.jsdelivr.net data:`** — `cdn.jsdelivr.net` for FontAwesome woff2/woff
  files. `data:` covers any base64-encoded font fallbacks in vendor CSS.
- **`connect-src 'self'`** — all XHR/fetch calls go to the same origin (Engagements calendar
  events endpoint, API calls proxied by the Web app).
- **`frame-ancestors 'self'`** — paired with `X-Frame-Options: SAMEORIGIN`; allows same-origin
  framing, denies cross-origin.
- **`object-src 'none'`** — no Flash/plugin content.
- **`base-uri 'self'`** — prevents base tag injection attacks.
- **`form-action 'self'`** — all form POSTs must target the same origin.

### 5. Inline script/style externalization

Two inline `<script>` blocks were moved to dedicated JS files to avoid needing `'unsafe-inline'`
in `script-src`:

- `Views/MessageTemplates/Index.cshtml` → `wwwroot/js/message-templates-index.js`
  (Bootstrap tooltip initializer)
- `Views/Schedules/Calendar.cshtml` → `wwwroot/js/schedules-calendar.js`
  (FullCalendar initializer; no server-side data injection — uses an AJAX endpoint)

One inline `<style>` block from `Views/Schedules/Calendar.cshtml` (`#calendar` sizing) was moved
to `wwwroot/css/site.css`.

### 6. Open Questions for Ghost Review

1. **`img-src https:`** — This broad allowance was chosen because `Settings.StaticContentRootUrl`
   (used for favicons) is a runtime configuration value with an unknown hostname at code-time.
   Ghost should evaluate whether this should be tightened to the known static asset host
   (e.g., `https://static.josephguadagno.net`) and potentially read from config at startup.

2. **`cdn.jsdelivr.net` scope** — All CDN assets are pinned with SRI `integrity=` hashes in
   the Production `<environment>` blocks. The CSP host allowance is a belt-and-suspenders
   measure. Ghost should confirm no other CDN hostnames are referenced in any partial views
   not covered by this review.

3. **Nonce-based CSP** — A future improvement would replace the `cdn.jsdelivr.net` allowance
   with per-request nonces, eliminating CDN host trust entirely. Out of scope for S6-6.

---

## Files Changed

- `src/JosephGuadagno.Broadcasting.Api/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/message-templates-index.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/schedules-calendar.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/css/site.css` — calendar style appended
- `src/JosephGuadagno.Broadcasting.Web/Views/MessageTemplates/Index.cshtml` — inline script removed
- `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Calendar.cshtml` — inline script and style removed

---

# Decision: API Pagination Pattern

**Author:** Trinity  
**Date:** 2026-03-20  
**Context:** Issue #316 - Add pagination to all list API endpoints

## Decision

All list endpoints in API controllers use **query parameter-based pagination** with `PagedResponse<T>` wrapper.

## Pattern

```csharp
// Add using statement
using JosephGuadagno.Broadcasting.Api.Models;

// Endpoint signature
public async Task<ActionResult<PagedResponse<TResponse>>> GetItemsAsync(
    int page = 1, 
    int pageSize = 25)
{
    var allItems = await _manager.GetAllAsync();
    var totalCount = allItems.Count;
    var items = allItems
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(ToResponse)
        .ToList();
    
    return new PagedResponse<TResponse>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

## PagedResponse Model

Located at `src/JosephGuadagno.Broadcasting.Api/Models/PagedResponse.cs`:

```csharp
public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

## Defaults

- **page**: 1 (first page)
- **pageSize**: 25 (items per page)

## Rationale

1. **Query parameters** - RESTful convention for pagination, allows optional parameters
2. **Default values** - Backward compatible - omitting params gives sensible defaults
3. **Client-side pagination** - Current managers return full collections; pagination happens in controller (acceptable for current data volumes)
4. **Consistent wrapper** - `PagedResponse<T>` provides uniform structure across all list endpoints
5. **TotalPages calculation** - Derived property eliminates need for clients to calculate page count

## Endpoints Updated (Issue #316)

### EngagementsController
- `GET /engagements?page={page}&pageSize={pageSize}`
- `GET /engagements/{id}/talks?page={page}&pageSize={pageSize}`

### SchedulesController
- `GET /schedules?page={page}&pageSize={pageSize}`
- `GET /schedules/unsent?page={page}&pageSize={pageSize}`
- `GET /schedules/upcoming?page={page}&pageSize={pageSize}`
- `GET /schedules/calendar/{year}/{month}?page={page}&pageSize={pageSize}`
- `GET /schedules/orphaned?page={page}&pageSize={pageSize}`

### MessageTemplatesController
- `GET /messagetemplates?page={page}&pageSize={pageSize}`

## Special Cases: 404 Endpoints

Endpoints that return `404 NotFound` when no items exist (e.g., unsent, orphaned) check count **before** pagination to maintain existing behavior:

```csharp
var allItems = await _manager.GetUnsentScheduledItemsAsync();
if (allItems.Count == 0)
{
    return NotFound();
}
// ... then paginate
```

## Future Considerations

- If data volumes grow significantly, consider adding server-side pagination to managers/data stores
- Could add sorting parameters (e.g., `?sortBy=createdOn&sortDirection=desc`)
- Could add filtering parameters (e.g., `?status=unsent`)

## References

- PR #514: https://github.com/jguadagno/jjgnet-broadcast/pull/514
- Issue #316: https://github.com/jguadagno/jjgnet-broadcast/issues/316

---

# Decision: Scriban Template Seeding Strategy (Sprint 7)

**Date:** 2026-03-20  
**Decider:** Trinity (Backend Dev)  
**Epic:** #474 - Templatize all of the messages  
**Issues:** #475 (Bluesky), #476 (Facebook), #477 (LinkedIn), #478 (Twitter)

## Context

The Scriban template infrastructure was implemented in PR #491, adding:
- `MessageTemplate` domain model (Platform, MessageType, Template, Description)
- `IMessageTemplateDataStore` interface with SQL implementation
- Template lookup in all 4 `ProcessScheduledItemFired` functions with fallback to hard-coded messages
- Constants for platforms (Twitter, Facebook, LinkedIn, Bluesky) and message types (RandomPost, NewSyndicationFeedItem, NewYouTubeItem, NewSpeakingEngagement, ScheduledItem)

However, NO templates were seeded in the database, so the system always fell back to the hard-coded message construction.

## Decision

**Seed default Scriban templates via SQL migration script** instead of embedded resource files.

Created `scripts/database/migrations/2026-03-20-seed-message-templates.sql` with 20 templates (5 per platform).

## Options Considered

### Option 1: Database-backed templates (SQL migration) ✅ CHOSEN
**Pros:**
- Can be updated via Web UI (`MessageTemplatesController` already exists)
- No code deployment required to change templates
- Centralized storage in SQL Server (already used for all other configuration)
- Consistent with existing `IMessageTemplateDataStore` implementation

**Cons:**
- Requires database migration execution
- Not version-controlled alongside code (but migrations are)

### Option 2: Embedded resource files (.liquid or .scriban in Functions project)
**Pros:**
- Version-controlled with code
- No database dependency
- Faster lookup (no DB round-trip)

**Cons:**
- Requires code redeployment to update templates
- Would need new loader implementation (file reader)
- Inconsistent with existing `IMessageTemplateDataStore` interface

### Option 3: Azure App Configuration or Key Vault
**Pros:**
- Centralized cloud configuration
- Can be updated without deployment

**Cons:**
- Adds external dependency
- Higher latency than local DB
- More complex than necessary for this use case

## Template Design

### Field Model (Exposed to all templates)
Each platform's `TryRenderTemplateAsync` provides:
- `title`: Post/engagement/talk title
- `url`: Full or shortened URL
- `description`: Comments/engagement details
- `tags`: Space-separated hashtags
- `image_url`: Optional thumbnail URL

### Platform-Specific Templates

#### Bluesky (300 char limit)
- **NewSyndicationFeedItem**: `Blog Post: {{ title }} {{ url }} {{ tags }}`
- **NewYouTubeItem**: `Video: {{ title }} {{ url }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }}) {{ description }}`
- **ScheduledItem**: `My talk: {{ title }} ({{ url }}) {{ description }} Come see it!`
- **RandomPost**: `{{ title }} {{ url }} {{ tags }}`

#### Facebook (2000 char limit, link preview handles URL)
- **NewSyndicationFeedItem**: `ICYMI: Blog Post: {{ title }} {{ tags }}`
- **NewYouTubeItem**: `ICYMI: Video: {{ title }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }})\n\n{{ description }}`
- **ScheduledItem**: `Talk: {{ title }} ({{ url }})\n\n{{ description }}`
- **RandomPost**: `{{ title }}\n\n{{ description }}`

#### LinkedIn (Professional tone)
- **NewSyndicationFeedItem**: `New blog post: {{ title }}\n\n{{ description }}\n\n{{ tags }}`
- **NewYouTubeItem**: `New video: {{ title }}\n\n{{ description }}\n\n{{ tags }}`
- **NewSpeakingEngagement**: `Excited to announce I'll be speaking at {{ title }}!\n\n{{ description }}\n\nLearn more: {{ url }}`
- **ScheduledItem**: `My talk: {{ title }}\n\n{{ description }}\n\nJoin me: {{ url }}`
- **RandomPost**: `{{ title }}\n\n{{ description }}\n\n{{ tags }}`

#### Twitter/X (280 char limit)
- **NewSyndicationFeedItem**: `Blog Post: {{ title }} {{ url }} {{ tags }}`
- **NewYouTubeItem**: `Video: {{ title }} {{ url }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }}) {{ description }}`
- **ScheduledItem**: `My talk: {{ title }} ({{ url }}) {{ description }} Come see it!`
- **RandomPost**: `{{ title }} {{ url }} {{ tags }}`

## Rationale

1. **Database-backed wins for flexibility**: The Web UI already has a MessageTemplates controller. Admins can tweak templates without code changes.
2. **Simple templates first**: Initial templates mirror the existing hard-coded logic. Future iterations can add Scriban conditionals (`if`/`else`), filters, etc.
3. **Platform limits enforced by code**: Functions already have fallback truncation logic. Templates don't need to handle character limits—they just provide the structure.
4. **Single migration for all platforms**: All 4 platforms share the same infrastructure, so a single SQL file seeds all 20 templates.

## Consequences

### Positive
- Templates are now customizable without redeployment
- Hard-coded fallback logic remains as safety net
- Web UI can manage templates (list, edit, update)
- Future templates can use Scriban's full feature set (conditionals, loops, filters)

### Negative
- Database must be migrated before templates take effect
- Templates are not co-located with code (but migrations are version-controlled)
- No compile-time validation of template syntax (errors logged at runtime)

## Implementation

**Commit:** `6c32c01` (pushed directly to `main`)  
**File:** `scripts/database/migrations/2026-03-20-seed-message-templates.sql`  
**Testing:** Build succeeds (Debug configuration). No unit tests needed for seed data.  
**Deployment:** Run migration script against production SQL Server to activate templates.

## Related

- **Epic:** #474 - Templatize all of the messages
- **Issues:** #475 (Bluesky), #476 (Facebook), #477 (LinkedIn), #478 (Twitter)
- **PR:** #491 - Original template infrastructure implementation
- **Domain Model:** `JosephGuadagno.Broadcasting.Domain.Models.MessageTemplate`
- **Data Store:** `JosephGuadagno.Broadcasting.Data.Sql.MessageTemplateDataStore`
- **Functions:** `ProcessScheduledItemFired` in Twitter, Facebook, LinkedIn, Bluesky folders

## Future Enhancements

1. **Conditional formatting**: Use Scriban `if`/`else` to vary messages based on field values (e.g., "Updated Blog Post" vs "New Blog Post" based on `item_last_updated_on`)
2. **Character limit enforcement in templates**: Add Scriban custom functions to truncate strings at specific lengths
3. **A/B testing**: Store multiple templates per (Platform, MessageType) and randomly select
4. **Localization**: Add a `Language` field to support multi-language templates
5. **Template validation**: Add UI preview/test functionality in the Web app




# Decision: ToResponse(null) NullReferenceException is a Known Production Bug

**By:** Neo (Lead)
**Date:** 2026-03-20
**Context:** PR #518 review — Api.Tests DTO fix

## Decision

The ToResponse(null) calls in EngagementsController and SchedulesController throw NullReferenceException when the manager returns 
ull (resource not found). Controllers should return NotFound() instead. This is a **tracked production bug** introduced by PR #512.

## Current Behavior (Bug)

`csharp
// Controller calls ToResponse(null) → NullReferenceException
var engagement = await _manager.GetAsync(id);  // returns null
return Ok(EngagementResponse.ToResponse(engagement!));  // throws
`

## Expected Behavior (Fix Required)

`csharp
var engagement = await _manager.GetAsync(id);
if (engagement == null) return NotFound();
return Ok(EngagementResponse.ToResponse(engagement));
`

## Impact

- Affects GetEngagementAsync, GetTalkAsync, GetScheduledItemAsync
- Returns 500 instead of 404 when resource doesn't exist
- Documented in test TODO comments pending fix

## Action Required

A follow-up issue/PR should fix null checks in all three controllers. This is **not** a test issue — it's a production code bug. Assign to Trinity (owns controllers/API).


# Decision: Sparks PR Batch Review — Forms UX & Accessibility

**By:** Neo (Lead)
**Date:** 2026-03-20
**Context:** Review of PRs #520, #522, #524 (Sparks' work)

---

## Decision: PR #520 — APPROVED (confirmed merged)

**PR:** feat(web): add loading/submitting state to forms (Closes #333)
**Branch:** squad/333-form-loading-state
**Status:** Squash-merged to main, branch deleted, issue #333 closed ✅

All criteria met:
1. JS uses existing jQuery — no new dependencies
2. Button re-enabled on `invalid-form.validate` (no permanent lock)
3. Calendar and theme toggle unaffected
4. Bootstrap 5 spinner markup correct
5. Change in `wwwroot/js/site.js` only

---

## Decision: PR #522 — HELD (code correct, CI red — not Sparks' fault)

**PR:** feat(web): add form accessibility (Closes #332)
**Branch:** squad/332-form-accessibility
**Status:** Open, awaiting fix of pre-existing AutoMapper issue

### Code Review: PASS (all 5 criteria met)
1. Every `<span asp-validation-for="X">` has `id="val-X"` ✅
2. Every input has `aria-describedby="val-{FieldName}"` ✅
3. `autocomplete` values correct (url for URLs, off for others) ✅
4. No structural changes — purely additive attributes ✅
5. WCAG 2.1 AA intent preserved ✅

### CI: FAIL (pre-existing issue from PR #523)

The `MappingTests.MappingProfile_IsValid` test fails because PR #523 (BlueSkyHandle
schema work) added `BlueSkyHandle` to `Domain.Models.Engagement` and
`Domain.Models.Talk` but did NOT add it to `Web.Models.EngagementViewModel` or
`Web.Models.TalkViewModel`. AutoMapper's `AssertConfigurationIsValid()` catches this.

PR #523 was merged at 15:21:46. PR #522's CI started at 15:21:52 (6 seconds later).
GitHub CI runs against the merged state with main, so #522 inherited the broken mapping.

### Required Fix (NOT Sparks' work)
1. Add `BlueSkyHandle` string? property to `EngagementViewModel`
2. Add `BlueSkyHandle` string? property to `TalkViewModel`
3. Ensure AutoMapper maps it (likely automatic via convention, or add `.Ignore()` if not
   yet exposed in the form)

Once fixed on main, Sparks should rebase #522 and re-run CI.

---

## Decision: PR #524 — APPROVED (confirmed merged)

**PR:** feat(web): add privacy page content (Closes #191)
**Branch:** squad/191-privacy-page
**Status:** Squash-merged to main, issue #191 closed ✅

All criteria met:
1. Placeholder replaced with real content — no TODO or lorem ipsum ✅
2. Appropriate for a personal broadcasting tool ✅
3. No broken HTML or Razor syntax ✅
4. Layout consistent with other content pages (Bootstrap table, standard headings) ✅

---

## Cross-PR Interference Pattern

When multiple feature branches are simultaneously open and one merges while another's CI
is queued/running, the second PR's CI will test against the merged state of main. This
means a branch with perfectly correct code can show red CI due to incomplete follow-on
work from a different PR.

**Protocol going forward:**
- When a schema/model PR (like BlueSkyHandle) merges, all open PRs against the same area
  should have their CI re-run after the follow-on ViewModel/mapping work is also merged
- Do not attribute a CI failure to the PR author without tracing the root cause


# Schema Decision: BlueSkyHandle on Engagements and Talks

**Date:** 2026-03-21
**Author:** Morpheus (Data Engineer)
**Issues:** #167 (Engagement BlueSkyHandle), #166 (Scheduled Talk BlueSkyHandle)
**PR:** #523

## Decision

Added `BlueSkyHandle NVARCHAR(255) NULL` to both the `dbo.Engagements` and `dbo.Talks` tables.

## Column Spec

| Table        | Column        | Type            | Nullable | Max Length |
|--------------|---------------|-----------------|----------|------------|
| Engagements  | BlueSkyHandle | NVARCHAR(255)   | YES      | 255        |
| Talks        | BlueSkyHandle | NVARCHAR(255)   | YES      | 255        |

## Rationale

- **Nullable:** No existing rows have a BlueSky handle. Making it nullable is the only backward-compatible choice.
- **NVARCHAR(255):** BlueSky handles follow the format `@user.bsky.social` (max ~253 chars). 255 is consistent with other handle/name columns in this schema.
- **Both tables:** An engagement (conference/event) may have its own BlueSky account. A talk's speaker may have a different BlueSky handle than the event itself.

## Files Changed

- `scripts/database/table-create.sql` — base schema updated
- `scripts/database/migrations/2026-03-21-add-bluesky-handle.sql` — ALTER TABLE for existing databases
- `src/JosephGuadagno.Broadcasting.Domain/Models/Engagement.cs` — `public string? BlueSkyHandle { get; set; }`
- `src/JosephGuadagno.Broadcasting.Domain/Models/Talk.cs` — `public string? BlueSkyHandle { get; set; }`
- `src/JosephGuadagno.Broadcasting.Data.Sql/Models/Engagement.cs` — EF entity property added
- `src/JosephGuadagno.Broadcasting.Data.Sql/Models/Talk.cs` — EF entity property added
- `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` — `HasMaxLength(255)` configured for both

## Follow-on Work

- **Trinity:** Update DTOs (`EngagementResponse`, `TalkRequest`/`TalkResponse`) to expose the field
- **Sparks:** Add BlueSkyHandle input fields to Engagement and Talk Add/Edit forms


# Decision: PR #516 and PR #517 Merge Review

## Date
2026-03-21

## Reviewer
Neo (Lead)

## PRs Merged

### PR #516 — feat(functions): add retry policies and dead-letter queue handling
- **Branch:** squad/319-functions-retry-policies
- **Issue closed:** #319
- **Verdict:** APPROVED & MERGED (squash)

### PR #517 — fix(sql): address 50MB database size cap and surface capacity errors
- **Branch:** squad/324-sql-size-cap
- **Issue closed:** #324
- **Verdict:** APPROVED & MERGED (squash)

---

## PR #516 — host.json Retry Policies

### Schema Verification
Azure Functions v4 `host.json` retry and queue extension config is valid:

```json
"retry": {
  "strategy": "exponentialBackoff",
  "maxRetryCount": 3,
  "minimumInterval": "00:00:05",
  "maximumInterval": "00:00:30"
},
"extensions": {
  "queues": {
    "maxPollingInterval": "00:00:02",
    "visibilityTimeout": "00:00:30",
    "batchSize": 16,
    "maxDequeueCount": 3,
    "newBatchThreshold": 8
  }
}
```

### Findings
1. `exponentialBackoff` strategy with `minimumInterval`/`maximumInterval` — correct v4 schema (TimeSpan `hh:mm:ss` format)
2. `maxRetryCount: 3` (function-level) = `maxDequeueCount: 3` (queue-level) — consistent; function gets 3 retries, then poison-queue routing
3. `visibilityTimeout: 30s` ≥ `maximumInterval: 30s` — no race where message re-appears on queue before retry backoff completes
4. All `extensions.queues` properties are valid Azure Storage Queue trigger settings
5. Poison queues auto-created by Azure Storage SDK — no provisioning work needed

### Pattern Established
- For Azure Functions queue retry config: `maxRetryCount` (function retries) should equal `maxDequeueCount` (queue DLQ threshold) to ensure consistent failure behavior
- `visibilityTimeout` must be ≥ `maximumInterval` to prevent retry/visibility race conditions

---

## PR #517 — SQL Size Cap Fix

### Verification: SQL Error 1105
SQL Server error **1105** = "Could not allocate space for object in database because the filegroup is full" — correct error for capacity-exceeded INSERT failures. ✅

### Migration Script Safety
`ALTER DATABASE JJGNet MODIFY FILE` is:
- Non-destructive DDL (does not recreate or truncate)
- Safe to run on live databases
- Idempotent (setting UNLIMITED on already-UNLIMITED files is a no-op)
- Includes verification SELECT for confirmation

### Code Quality: SaveChangesAsync Override
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    try { return await base.SaveChangesAsync(cancellationToken); }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
    {
        if (sqlEx.Number == 1105)
            throw new InvalidOperationException("Database capacity exceeded...", ex);
        throw;
    }
}
```
- `when` clause is efficient — no overhead on non-SqlException paths
- Overriding `CancellationToken` variant covers both overloads (no-arg `SaveChangesAsync()` delegates to it in EF Core's DbContext base)
- Original exception preserved as `innerException` — stack trace intact
- Non-1105 SqlExceptions are re-thrown unchanged — no swallowing

### Pattern Established
**Two-layer defense for database infrastructure constraints:**
1. **Preventive:** Remove arbitrary limits in provisioning scripts (`MAXSIZE = UNLIMITED`)
2. **Defensive:** Override `SaveChangesAsync` in DbContext to catch and surface specific SQL error codes

**SQL error handling in EF Core DbContext:**
- Catch `DbUpdateException` → check `InnerException is SqlException`
- Check `SqlException.Number` for specific codes (1105 = capacity, 2627 = unique constraint, etc.)
- Throw domain-appropriate exceptions with clear messages, preserving original as inner

---

## Impact
- Azure Functions: All 6 queue-triggered functions now have exponential backoff retry (3x, 5s→30s) and DLQ routing
- SQL: New databases provisioned without size caps; existing databases can be migrated; capacity failures now throw clear exceptions
- No breaking changes in either PR

## Related
- Sprint 9 milestone (#4)
- Issues #319, #324 (auto-closed)


# Neo Decision: PR #521 Merge — Null Guard / 404 Fix

**Date:** 2026-03-20
**PR:** #521 `squad/519-fix-null-ref-404`
**Issue:** #519 (auto-closed)
**Merged by:** jguadagno (already merged before review)

## Decision

**APPROVED.** PR #521 is correct, complete, and safe to merge. All changes verified.

## What Changed

### Production Code
- `EngagementsController.GetEngagementAsync`: null guard + `return Ok(ToResponse(engagement))`
- `EngagementsController.GetTalkAsync`: return type fixed (`Task<TalkResponse>` → `Task<ActionResult<TalkResponse>>`), null guard added
- `SchedulesController.GetScheduledItemAsync`: null guard + `return Ok(ToResponse(item))`
- Bonus: scope acceptance updated to include granular scopes (`.List`/`.View`/`.Modify`) alongside `.All` in EngagementsController

### Tests
- 3 not-found tests: `ThrowsNullReferenceException` → `ReturnsNotFound` (`result.Result.Should().BeOfType<NotFoundResult>()`)
- 3 success tests: `result.Value` → `result.Result.Should().BeOfType<OkObjectResult>().Subject` (correct for explicit `return Ok()`)

## Patterns Established

1. **Null guard before ToResponse**: `if (x is null) return NotFound(); return Ok(ToResponse(x));`
2. **OkObjectResult test pattern**: When testing explicit `return Ok(value)` endpoints, access value via `((OkObjectResult)result.Result).Value`, not `result.Value`
3. **ActionResult return type required**: Methods returning `NotFound()` must have return type `ActionResult<T>` — bare `T` return type cannot carry non-200 responses

## Minor Gap (Not Blocking)

`GetTalkAsync` scope still only accepts `Talks.All` (`.View` remains commented). This is pre-existing from before this PR. Recommend a follow-up scope cleanup pass across all Talk endpoints.


# Decision: Fine-Grained API Permission Scopes (Issue #170)

**Date:** 2026-03-20
**Author:** Ghost (Security & Identity Specialist)
**Applies to:** API controllers, Web services, Domain/Scopes.cs
**PR:** #526

---

## Context

The API used `*.All` scopes on every endpoint. Issue #170 requires breaking these into specific least-privilege scopes so callers only need the permission for what they're actually doing.

---

## Decisions

### 1. Scope naming convention — `{Resource}.{Action}`

| HTTP verb | Scope action |
|-----------|-------------|
| GET (collection) | `List` |
| GET (by ID) | `View` |
| POST / PUT | `Modify` |
| DELETE | `Delete` |

Special read-only Schedules sub-endpoints retain their existing scope constants:
- `Schedules.UnsentScheduled` → GET /schedules/unsent
- `Schedules.ScheduledToSend` → GET /schedules/upcoming
- `Schedules.UpcomingScheduled` → GET /schedules/calendar/{year}/{month}

These special scopes also accept `Schedules.List` or `Schedules.All` as fallback (three-argument `VerifyUserHasAnyAcceptedScope`).

### 2. Backward compatibility — dual-scope acceptance on API side

**Decision:** Controllers accept `(specificScope, *.All)` via `VerifyUserHasAnyAcceptedScope`.

**Rationale:** Existing Azure AD app registrations and client credentials using `*.All` must continue working without forced reconfiguration. Least-privilege enforcement is opt-in via new token issuance.

**When to remove the *.All fallback:** After all callers have been updated to request only fine-grained scopes and verified in production, the `*.All` fallback can be stripped from controller checks. Track this as a follow-up.

### 3. Web services request fine-grained scopes

**Decision:** `SetRequestHeader(scope)` in all Web services now uses the specific scope, not `*.All`.

**Rationale:** This is the correct least-privilege behavior at the MSAL token level. The Web app's MSAL client (`EnableTokenAcquisitionToCallDownstreamApi`) can still acquire the broader `*.All` scopes if needed; the per-request scope narrows what the token carries.

### 4. `Web/Program.cs` MSAL scope config unchanged

`AllAccessToDictionary` is still used for `EnableTokenAcquisitionToCallDownstreamApi` because it defines the universe of scopes the Web app's OIDC client is allowed to request. No change needed here — the per-request `SetRequestHeader(specificScope)` handles narrowing.

### 5. Swagger advertises all fine-grained scopes

`XmlDocumentTransformer` changed from `AllAccessToDictionary` → `ToDictionary` so Swagger UI shows every available scope for interactive testing. This helps API consumers discover and test with least-privilege tokens.

### 6. MessageTemplates scopes added

`MessageTemplates` only had `All` defined. Added `List`, `View`, and `Modify` to match the other resources. No `Delete` scope defined because the API has no delete endpoint for message templates.

### 7. Bug fix: EngagementService.DeleteEngagementTalkAsync

Was requesting `Engagements.All` (and comment incorrectly said `Engagements.Delete`). Corrected to `Talks.Delete` since the operation deletes a talk, not an engagement.

---

## What still needs Azure AD configuration

The fine-grained scopes (`Engagements.List`, `Engagements.View`, etc.) must be registered as **delegated permissions** on the API App Registration in Azure AD before production tokens can use them. This is an infrastructure step — see `infrastructure-needs.md`.

Until then, clients must use `*.All` tokens, which the API continues to accept.


---

## Decisions from Sprint — Inbox Merged 2026-03-20T19-35-21Z


# Ghost Decision: MSAL Token Cache Eviction Handling (Issue #528)

## Date
2026-03-20

## Decision

### 1. Use `[AuthorizeForScopes]` at controller class level — no params needed

`[AuthorizeForScopes]` from `Microsoft.Identity.Web` is applied as a class-level attribute on all four controllers that call the downstream API (`EngagementsController`, `TalksController`, `SchedulesController`, `MessageTemplatesController`).

No `Scopes` or `ScopeKeySection` attribute parameters are set. This is intentional: when `GetAccessTokenForUserAsync` fails, Microsoft.Identity.Web wraps the exception as `MicrosoftIdentityWebChallengeUserException` and populates `ex.Scopes` with the exact scope that was requested. The attribute reads those scopes directly from the exception and issues the correct challenge.

### 2. Two distinct "not in cache" scenarios — handled separately

| Scenario | Handler | Behavior |
|----------|---------|----------|
| Account object missing from cache entirely (`user_null`) | `RejectSessionCookieWhenAccountNotInCacheEvents` | Rejects the session cookie → user is signed out |
| Account in cache, but specific API token missing | `[AuthorizeForScopes]` on each controller | Issues OIDC challenge → user re-authenticates and gets new tokens |

These are complementary. Do not collapse them into one handler.

### 3. Token cache is SQL-backed — confirmed, no change needed

`AddDistributedSqlServerCache` + `AddDistributedTokenCaches()` in `Web/Program.cs`. The SQL `dbo.Cache` table is the token store. No in-memory fallback. If the cache table is cleared externally, both the account-missing and token-missing paths will trigger.

### 4. Scope URL is not required in the attribute

The `ApiScopeUrl` configuration key (`Settings:ApiScopeUrl`) holds only the base URI. Do NOT use it as a `ScopeKeySection` value on `[AuthorizeForScopes]` — it is not a valid scope on its own. The exception-embedded scopes approach is the correct pattern for this codebase.

### 5. Issues #83 and #85 are separate

- **#83**: `MsalClientException` with "cache contains multiple tokens" — different code path, not addressed by `[AuthorizeForScopes]`.
- **#85**: `OpenIdConnectProtocolException` AADSTS650052 — tenant/subscription configuration issue, separate from token cache handling.


# Decision: Fine-Grained API Permission Scopes (Issue #170)

**Date:** 2026-03-20
**Author:** Ghost (Security & Identity Specialist)
**Applies to:** API controllers, Web services, Domain/Scopes.cs
**PR:** #526

---

## Context

The API used `*.All` scopes on every endpoint. Issue #170 requires breaking these into specific least-privilege scopes so callers only need the permission for what they're actually doing.

---

## Decisions

### 1. Scope naming convention — `{Resource}.{Action}`

| HTTP verb | Scope action |
|-----------|-------------|
| GET (collection) | `List` |
| GET (by ID) | `View` |
| POST / PUT | `Modify` |
| DELETE | `Delete` |

Special read-only Schedules sub-endpoints retain their existing scope constants:
- `Schedules.UnsentScheduled` → GET /schedules/unsent
- `Schedules.ScheduledToSend` → GET /schedules/upcoming
- `Schedules.UpcomingScheduled` → GET /schedules/calendar/{year}/{month}

These special scopes also accept `Schedules.List` or `Schedules.All` as fallback (three-argument `VerifyUserHasAnyAcceptedScope`).

### 2. Backward compatibility — dual-scope acceptance on API side

**Decision:** Controllers accept `(specificScope, *.All)` via `VerifyUserHasAnyAcceptedScope`.

**Rationale:** Existing Azure AD app registrations and client credentials using `*.All` must continue working without forced reconfiguration. Least-privilege enforcement is opt-in via new token issuance.

**When to remove the *.All fallback:** After all callers have been updated to request only fine-grained scopes and verified in production, the `*.All` fallback can be stripped from controller checks. Track this as a follow-up.

### 3. Web services request fine-grained scopes

**Decision:** `SetRequestHeader(scope)` in all Web services now uses the specific scope, not `*.All`.

**Rationale:** This is the correct least-privilege behavior at the MSAL token level. The Web app's MSAL client (`EnableTokenAcquisitionToCallDownstreamApi`) can still acquire the broader `*.All` scopes if needed; the per-request scope narrows what the token carries.

### 4. `Web/Program.cs` MSAL scope config unchanged

`AllAccessToDictionary` is still used for `EnableTokenAcquisitionToCallDownstreamApi` because it defines the universe of scopes the Web app's OIDC client is allowed to request. No change needed here — the per-request `SetRequestHeader(specificScope)` handles narrowing.

### 5. Swagger advertises all fine-grained scopes

`XmlDocumentTransformer` changed from `AllAccessToDictionary` → `ToDictionary` so Swagger UI shows every available scope for interactive testing. This helps API consumers discover and test with least-privilege tokens.

### 6. MessageTemplates scopes added

`MessageTemplates` only had `All` defined. Added `List`, `View`, and `Modify` to match the other resources. No `Delete` scope defined because the API has no delete endpoint for message templates.

### 7. Bug fix: EngagementService.DeleteEngagementTalkAsync

Was requesting `Engagements.All` (and comment incorrectly said `Engagements.Delete`). Corrected to `Talks.Delete` since the operation deletes a talk, not an engagement.

---

## What still needs Azure AD configuration

The fine-grained scopes (`Engagements.List`, `Engagements.View`, etc.) must be registered as **delegated permissions** on the API App Registration in Azure AD before production tokens can use them. This is an infrastructure step — see `infrastructure-needs.md`.

Until then, clients must use `*.All` tokens, which the API continues to accept.


# Ghost — Cookie Security Hardening (Issue #336)

**Date:** 2026-03-19
**Sprint:** Sprint 8
**PR:** #510

## What Was Done

Three separate cookie surfaces were hardened in `src/JosephGuadagno.Broadcasting.Web/Program.cs`:

### 1. Auth Cookie (`CookieAuthenticationOptions`)
Previously only set `Events`. Now also sets:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Lax`

*Lax is appropriate for the auth cookie — it must survive top-level cross-site navigations (e.g., OIDC redirect back from Azure AD).*

### 2. Session Cookie (`AddSession`)
Previously used `AddSession()` with no options. Now:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Lax`
- `IsEssential = true` — prevents session cookie from being blocked by GDPR middleware before consent

### 3. Antiforgery Cookie (`AddAntiforgery`)
Not previously configured at all. Added explicit:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Strict`

*Strict is correct for the antiforgery token — it never needs to be sent on cross-site requests. This provides the strongest CSRF protection.*

## Findings / Learnings

- `ImplicitUsings=enable` on the Web project means `Microsoft.AspNetCore.Http` types (`CookieSecurePolicy`, `SameSiteMode`) are available without explicit `using` statements.
- `AddAntiforgery` is called before `AddControllersWithViews` so our explicit configuration wins over the default registered by MVC.
- The `Configure<CookieAuthenticationOptions>` post-configuration pattern used by MSAL (`RejectSessionCookieWhenAccountNotInCacheEvents`) still works fine when security options are added to the same lambda.
- SameSite=Lax (not Strict) is required for the auth cookie because the OIDC `redirect_uri` is a cross-site POST from Azure AD — Strict would break login.

## Decision

> Cookie security flags must be explicitly set on all cookie surfaces (auth, session, antiforgery) rather than relying on framework defaults. This is now the pattern for this project.


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


# Decision: PR #511 CI Fix — Merge main instead of rebase

**Date:** 2025-07-14  
**Author:** Link (Platform & DevOps Engineer)  
**PR:** #511 `feature/s8-328-wire-application-insights`

## Decision

Used `git merge origin/main --no-edit` (not rebase) to bring PR #511 up to date with main after PR #513 landed.

## Rationale

- PR #511's changes are entirely in `ServiceDefaults/` and `Program.cs` files — no overlap with the controller/test renames from PR #513.
- Merge produced a clean auto-merge with no conflicts.
- Rebase was unnecessary complexity for a non-overlapping change set; merge preserves the original commit history and is less risky in a shared branch.

## Workflow conflict policy (secondary decision)

When popping stashes onto branches that have received `origin/main` updates, workflow file conflicts in `.github/workflows/*.yml` should always resolve to the `origin/main` version. The vuln-scan policy (Critical-only gate, with High/Medium/Low logged but non-blocking) was deliberately established in PR #509 and must not be regressed.


# Decision: ConferenceHashtag and ConferenceTwitterHandle naming (Issue #105)

**Author:** Morpheus  
**Date:** 2026-03-21  
**Issue:** #105  
**PR:** #529

## Decision

Fields added to `dbo.Engagements` are named `ConferenceHashtag` and `ConferenceTwitterHandle` — both `NVARCHAR(255) NULL`.

## Rationale

- **Nullable:** Not every engagement has a hashtag or Twitter handle. Nullable is the right default for additive optional fields.
- **NVARCHAR(255):** Follows the team convention of bounded lengths (no MAX) on all columns. 255 is sufficient for any social handle or hashtag string.
- **`ConferenceTwitterHandle` not `TwitterHandle`:** Scoped to conference/event identity to distinguish it from a speaker handle. Parallel to the existing `BlueSkyHandle` field.
- **`ConferenceHashtag` not `HashTag` or `ConferenceHashTag`:** Pascal-case consistent with C# conventions; "Hashtag" as a single word follows current naming in the domain.

## Downstream impact

- **Trinity (API):** Add `ConferenceHashtag` and `ConferenceTwitterHandle` to `EngagementRequest` and `EngagementResponse` DTOs.
- **Switch (Web):** Surface both fields in `EngagementViewModel` and the Add/Edit/Details Razor views.


# Schema Decision: BlueSkyHandle on Engagements and Talks

**Date:** 2026-03-21
**Author:** Morpheus (Data Engineer)
**Issues:** #167 (Engagement BlueSkyHandle), #166 (Scheduled Talk BlueSkyHandle)
**PR:** #523

## Decision

Added `BlueSkyHandle NVARCHAR(255) NULL` to both the `dbo.Engagements` and `dbo.Talks` tables.

## Column Spec

| Table        | Column        | Type            | Nullable | Max Length |
|--------------|---------------|-----------------|----------|------------|
| Engagements  | BlueSkyHandle | NVARCHAR(255)   | YES      | 255        |
| Talks        | BlueSkyHandle | NVARCHAR(255)   | YES      | 255        |

## Rationale

- **Nullable:** No existing rows have a BlueSky handle. Making it nullable is the only backward-compatible choice.
- **NVARCHAR(255):** BlueSky handles follow the format `@user.bsky.social` (max ~253 chars). 255 is consistent with other handle/name columns in this schema.
- **Both tables:** An engagement (conference/event) may have its own BlueSky account. A talk's speaker may have a different BlueSky handle than the event itself.

## Files Changed

- `scripts/database/table-create.sql` — base schema updated
- `scripts/database/migrations/2026-03-21-add-bluesky-handle.sql` — ALTER TABLE for existing databases
- `src/JosephGuadagno.Broadcasting.Domain/Models/Engagement.cs` — `public string? BlueSkyHandle { get; set; }`
- `src/JosephGuadagno.Broadcasting.Domain/Models/Talk.cs` — `public string? BlueSkyHandle { get; set; }`
- `src/JosephGuadagno.Broadcasting.Data.Sql/Models/Engagement.cs` — EF entity property added
- `src/JosephGuadagno.Broadcasting.Data.Sql/Models/Talk.cs` — EF entity property added
- `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` — `HasMaxLength(255)` configured for both

## Follow-on Work

- **Trinity:** Update DTOs (`EngagementResponse`, `TalkRequest`/`TalkResponse`) to expose the field
- **Sparks:** Add BlueSkyHandle input fields to Engagement and Talk Add/Edit forms


### 2026-03-20: Pagination parameter validation pattern
**By:** Morpheus
**What:** Paginated endpoints clamp page to min 1, pageSize to range 1-100. Applied as inline guards at the top of each list action method.
**Why:** Neo review blocked on division-by-zero (pageSize=0) and negative Skip (page=0).


# Decision: SQL Server Size Cap Removal and Error Surfacing

## Date
2026-03-21

## Issue
#324 — SQL Server 50MB database size cap causes silent INSERT failures

## Context
The database-create.sql script provisioned SQL Server with a hard 50MB cap on the data file (`MAXSIZE = 50`) and 25MB cap on the log file (`MAXSIZE = 25MB`). When these limits were hit, INSERT operations would silently fail without surfacing any error to the application layer, making debugging extremely difficult.

## Root Cause
1. **Provisioning constraint:** The database creation script had arbitrary size limits (likely remnants of LocalDB or Azure SQL free-tier constraints)
2. **Silent failure:** EF Core's SaveChangesAsync would not surface SQL error 1105 (insufficient space) as a meaningful exception, leaving the application unaware of capacity issues

## Decision

### 1. Remove Size Caps (Preventive)
Changed `scripts/database/database-create.sql`:
- Data file: `MAXSIZE = 50` → `MAXSIZE = UNLIMITED`
- Log file: `MAXSIZE = 25MB` → `MAXSIZE = UNLIMITED`

**Rationale:** The 50MB cap was arbitrary and inappropriate for a production-grade application. Modern SQL Server containers and Azure SQL tiers support much larger databases. UNLIMITED allows the database to grow as needed (subject to disk space and SQL Server edition limits).

### 2. Surface Capacity Errors (Defensive)
Added `SaveChangesAsync` override in `BroadcastingContext` to catch `DbUpdateException` with inner `SqlException` and check for error number 1105 (insufficient space):

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    try
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
    {
        if (sqlEx.Number == 1105)
        {
            throw new InvalidOperationException(
                "Database capacity exceeded. The database has reached its maximum size limit. " +
                "Contact the administrator to increase the database capacity or archive old data.",
                ex);
        }
        throw;
    }
}
```

**Rationale:** Even with UNLIMITED, capacity issues can still occur (disk full, quota limits). This ensures the application fails fast with a clear error message rather than silently swallowing INSERT failures.

### 3. Migration for Existing Databases
Created `scripts/database/migrations/2026-03-21-increase-database-size-limits.sql` using `ALTER DATABASE MODIFY FILE`, which updates existing databases without requiring recreation or data loss.

**Rationale:** Allows zero-downtime migration of existing databases. `MODIFY FILE` is non-destructive and can be run on live databases.

## Pattern Established
**Two-layer defense for database capacity issues:**
1. **Preventive:** Remove arbitrary limits in provisioning scripts unless there's a specific business or infrastructure constraint
2. **Defensive:** Override SaveChangesAsync in DbContext to catch and surface SQL errors that would otherwise fail silently

**SQL Error Handling in EF Core:**
- Wrap `DbUpdateException` and check `InnerException` for `SqlException`
- Check `SqlException.Number` for specific error codes (e.g., 1105 = insufficient space, 2627 = unique constraint violation)
- Throw domain-appropriate exceptions (e.g., `InvalidOperationException`, `ArgumentException`) with clear messages

## Alternatives Considered
1. **Increase cap to 500MB instead of UNLIMITED:** Rejected because it just delays the problem and adds complexity
2. **Add monitoring/alerting instead of error handling:** Rejected as insufficient — alerting is good but doesn't prevent silent failures
3. **Use EF Core interceptors instead of SaveChangesAsync override:** Considered but SaveChangesAsync override is simpler and sufficient for this use case

## Impact
- New databases provisioned via Aspire AppHost will have no size caps
- Existing databases can be migrated using the provided script
- INSERT failures due to capacity will throw clear exceptions visible in logs and monitoring
- No breaking changes to existing code

## Related
- PR #517
- Sprint 9 milestone


# Neo PR #512 Re-Review Verdict

**Date:** 2026-03-21
**PR:** #512 `feature/s8-315-api-dtos`
**Original Review:** 2026-03-21 (CHANGES REQUESTED)
**Fix Author:** Morpheus
**Re-Review Author:** Neo

## Verdict: APPROVED ✅

Both blocking issues from initial review have been resolved.

## Issues Resolved

### 1. ✅ BOM Character Removed
**Original issue:** MessageTemplatesController.cs line 1 had UTF-8 BOM (U+FEFF) before first `using` statement.

**Fix verified:** Commit `9f02d429` changed line 1 from `\uFEFFusing` to clean `using`. File now clean UTF-8.

### 2. ✅ Route-as-Ground-Truth Pattern Fixed
**Original issue:** `TalkRequest.EngagementId` property violated route-as-ground-truth pattern. The route `POST /engagements/{engagementId}/talks` provides `engagementId`, so it should not be in the request body DTO.

**Fix verified:** 
- Commit `9f02d429` removed these lines from TalkRequest.cs:
  ```csharp
  [Required]
  public int EngagementId { get; set; }
  ```
- Controller ToModel calls correctly use route parameter:
  - CREATE: `var talk = ToModel(request, engagementId);`
  - UPDATE: `var talk = ToModel(request, engagementId, talkId);`
- ToModel signature: `private static Talk ToModel(TalkRequest r, int engagementId, int id = 0)`

## Pattern Compliance Verified

All 3 controllers (EngagementsController, SchedulesController, MessageTemplatesController) follow the approved DTO pattern:

1. ✅ Private static `ToResponse(DomainModel)` helpers
2. ✅ Private static `ToModel(RequestDTO, routeParams...)` helpers
3. ✅ No AutoMapper or external mapping library
4. ✅ Route parameters passed to ToModel as arguments, not from DTO
5. ✅ Request DTOs for input, Response DTOs for output
6. ✅ Proper null handling with `?.` operator (e.g., `e.Talks?.Select(ToResponse).ToList()`)
7. ✅ No "route id must match body id" validation checks

## CI Status

- ✅ GitGuardian Security Checks passed

## New Issues

None identified.

## Recommendation

**Ready to merge.** PR #512 successfully implements DTO layer pattern and closes issue #315.

## GitHub Limitation Note

Cannot formally approve PR via GitHub API because reviewer (jguadagno) is same as PR author. Posted approval verdict as comment: https://github.com/jguadagno/jjgnet-broadcast/pull/512#issuecomment-4095334205


### 2026-03-19T20:47:12: PR #514 pagination review verdict
**By:** Neo
**Verdict:** CHANGES REQUESTED
**Blocking issues:**
1. Division by zero in PagedResponse.TotalPages when pageSize=0
2. Negative Skip() calculation when page=0

**Why:**
The core pagination pattern is correctly implemented (PagedResponse<T> wrapper, consistent defaults, full coverage of all list endpoints, proper DTO usage). However, two edge cases will cause runtime failures:
- pageSize=0 throws DivideByZeroException in TotalPages calculation
- page=0 produces negative Skip() value, leading to misleading client behavior

These are defensive validation gaps that must be fixed before production use. Pattern compliance is otherwise excellent — no BOM issues, consistent across all 9 list endpoints.

**Remediation:** Per team protocol, Trinity (PR author) cannot fix their own rejected PR. Coordinator must assign a different agent.


# Neo Re-Review Verdict: PR #514 — Pagination Implementation (APPROVED)

**Date:** 2026-03-21  
**Reviewer:** Neo  
**PR:** #514 `feature/s8-316-pagination`  
**Previous Review:** 2026-03-19T20:47:12 (CHANGES REQUESTED)  
**Fixes By:** Morpheus  

## Verdict: APPROVED ✅

Both blocking edge cases from the initial review have been resolved with proper input validation guards.

## Issues Resolved

### 1. ✅ Division by Zero — FIXED
**Original Issue:** PagedResponse.TotalPages calculation (`TotalCount / PageSize`) threw DivideByZeroException when `pageSize=0`.

**Fix Applied:** All 8 paginated endpoints now validate and clamp pageSize:
```csharp
if (pageSize < 1) pageSize = 1;
if (pageSize > 100) pageSize = 100;
```

**Result:** TotalPages calculation is always safe because PageSize is guaranteed to be ≥ 1.

### 2. ✅ Negative Skip — FIXED
**Original Issue:** `Skip((page - 1) * pageSize)` produced negative values when `page=0`, causing undefined behavior.

**Fix Applied:** All 8 paginated endpoints now validate and clamp page:
```csharp
if (page < 1) page = 1;
```

**Result:** Skip calculation always receives valid positive or zero values.

## Validation Coverage (8/8 Endpoints)

All paginated list endpoints have consistent validation guards:

1. **EngagementsController.GetEngagementsAsync** — ✅ page/pageSize guards present
2. **EngagementsController.GetTalksForEngagementAsync** — ✅ page/pageSize guards present
3. **MessageTemplatesController.GetAllAsync** — ✅ page/pageSize guards present
4. **SchedulesController.GetScheduledItemsAsync** — ✅ page/pageSize guards present
5. **SchedulesController.GetUnsentScheduledItemsAsync** — ✅ page/pageSize guards present
6. **SchedulesController.GetScheduledItemsToSendAsync** — ✅ page/pageSize guards present
7. **SchedulesController.GetUpcomingScheduledItemsForCalendarMonthAsync** — ✅ page/pageSize guards present
8. **SchedulesController.GetOrphanedScheduledItemsAsync** — ✅ page/pageSize guards present

## Pattern Compliance

✅ **Consistent validation logic** across all endpoints (page: min 1, pageSize: 1-100)  
✅ **PagedResponse\<T\> wrapper** correctly used with Items, Page, PageSize, TotalCount, TotalPages  
✅ **Response DTOs** properly wrapped in PagedResponse  
✅ **No route-as-ground-truth violations** detected  
✅ **No BOM characters** in modified files  
✅ **CI passing** (GitGuardian checks successful)  

## New Issues Found

**None.** The validation fix is clean and introduces no new problems.

## Recommendation

**READY TO MERGE.** All blocking issues resolved, pattern compliance verified, CI passing.

## Next Steps

1. Merge PR #514
2. Close issue #316
3. Consider documenting the pagination pattern (min/max limits, validation approach) for future API endpoint development

---

*Note: Could not formally approve PR via `gh pr review --approve` because PR author (jguadagno) cannot approve their own PR per GitHub policy. Added approval comment to PR thread instead.*


# Review Decision: PR #529 — feat(data): add HashTag and ConferenceHandle fields to Engagement

**Date:** 2026-03-21  
**Author:** Neo (Lead)  
**PR:** #529  
**Branch:** squad/105-conference-hashtag-handle  
**Decision:** REQUEST CHANGES (not merged)

## Verdict

Changes requested. Two issues must be fixed before merge:

### Blocker: CI Failure — Web.Tests.MappingTests.MappingProfile_IsValid
AutoMapper `EngagementViewModel → Engagement` doesn't map the two new domain properties (`ConferenceHashtag`, `ConferenceTwitterHandle`). Fails at `AssertConfigurationIsValid()`. Fix: add the properties to `EngagementViewModel` OR add `.Ignore()` in the mapping profile. Identical pattern to PR #523 (BlueSkyHandle).

### Minor: EF Entity Nullability Mismatch
`Data.Sql/Models/Engagement.cs` declares the new columns as `string` (non-nullable) while domain model has `string?`. Should be `string?` to match domain and the `BlueSkyHandle` pattern on the same file.

## What Passed
- Migration idempotent (IF NOT EXISTS guard) ✅
- NVARCHAR(255) bounded ✅  
- Domain model nullable ✅  
- EF HasMaxLength(255) configured ✅  
- PR body notes downstream work (Trinity: DTOs, Switch: views) ✅

## Downstream Work Queue (after merge)
1. **Trinity** — `EngagementRequest` / `EngagementResponse` DTOs need the two new fields
2. **Switch** — `EngagementViewModel` + Add/Edit/Details Razor views need the fields surfaced


# Decision: Sparks PR Batch Review — Forms UX & Accessibility

**By:** Neo (Lead)
**Date:** 2026-03-20
**Context:** Review of PRs #520, #522, #524 (Sparks' work)

---

## Decision: PR #520 — APPROVED (confirmed merged)

**PR:** feat(web): add loading/submitting state to forms (Closes #333)
**Branch:** squad/333-form-loading-state
**Status:** Squash-merged to main, branch deleted, issue #333 closed ✅

All criteria met:
1. JS uses existing jQuery — no new dependencies
2. Button re-enabled on `invalid-form.validate` (no permanent lock)
3. Calendar and theme toggle unaffected
4. Bootstrap 5 spinner markup correct
5. Change in `wwwroot/js/site.js` only

---

## Decision: PR #522 — HELD (code correct, CI red — not Sparks' fault)

**PR:** feat(web): add form accessibility (Closes #332)
**Branch:** squad/332-form-accessibility
**Status:** Open, awaiting fix of pre-existing AutoMapper issue

### Code Review: PASS (all 5 criteria met)
1. Every `<span asp-validation-for="X">` has `id="val-X"` ✅
2. Every input has `aria-describedby="val-{FieldName}"` ✅
3. `autocomplete` values correct (url for URLs, off for others) ✅
4. No structural changes — purely additive attributes ✅
5. WCAG 2.1 AA intent preserved ✅

### CI: FAIL (pre-existing issue from PR #523)

The `MappingTests.MappingProfile_IsValid` test fails because PR #523 (BlueSkyHandle
schema work) added `BlueSkyHandle` to `Domain.Models.Engagement` and
`Domain.Models.Talk` but did NOT add it to `Web.Models.EngagementViewModel` or
`Web.Models.TalkViewModel`. AutoMapper's `AssertConfigurationIsValid()` catches this.

PR #523 was merged at 15:21:46. PR #522's CI started at 15:21:52 (6 seconds later).
GitHub CI runs against the merged state with main, so #522 inherited the broken mapping.

### Required Fix (NOT Sparks' work)
1. Add `BlueSkyHandle` string? property to `EngagementViewModel`
2. Add `BlueSkyHandle` string? property to `TalkViewModel`
3. Ensure AutoMapper maps it (likely automatic via convention, or add `.Ignore()` if not
   yet exposed in the form)

Once fixed on main, Sparks should rebase #522 and re-run CI.

---

## Decision: PR #524 — APPROVED (confirmed merged)

**PR:** feat(web): add privacy page content (Closes #191)
**Branch:** squad/191-privacy-page
**Status:** Squash-merged to main, issue #191 closed ✅

All criteria met:
1. Placeholder replaced with real content — no TODO or lorem ipsum ✅
2. Appropriate for a personal broadcasting tool ✅
3. No broken HTML or Razor syntax ✅
4. Layout consistent with other content pages (Bootstrap table, standard headings) ✅

---

## Cross-PR Interference Pattern

When multiple feature branches are simultaneously open and one merges while another's CI
is queued/running, the second PR's CI will test against the merged state of main. This
means a branch with perfectly correct code can show red CI due to incomplete follow-on
work from a different PR.

**Protocol going forward:**
- When a schema/model PR (like BlueSkyHandle) merges, all open PRs against the same area
  should have their CI re-run after the follow-on ViewModel/mapping work is also merged
- Do not attribute a CI failure to the PR author without tracing the root cause


# Neo Triage Decision: Issues #527 and #528

**Date:** 2026-03-21
**Author:** Neo (Lead)

## Issue #527 — `fix(api): GetTalkAsync only accepts Talks.All scope — add fine-grained Talks.View support`

**Routed to:** Trinity (`squad:trinity`)
**Priority:** High

**Rationale:**
API scope gap — `EngagementsController.GetTalkAsync` was missed during the PR #526 multi-policy scope pass. All other GET endpoints in the controller accept both a fine-grained scope and `*.All`. This issue is surgical: add the `Talks.View` policy acceptance alongside `Talks.All` in `GetTalkAsync`. Breaks least-privilege callers immediately; backward compat must hold for existing `Talks.All` tokens.

**Acceptance criteria:**
- `GetTalkAsync` accepts `Talks.View` and `Talks.All`
- Existing `Talks.All` tokens unaffected
- Unit test covers fine-grained scope path

---

## Issue #528 — `(fix): Authentication: Managing incremental consent and conditional access`

**Routed to:** Ghost (`squad:ghost`)
**Priority:** High

**Rationale:**
Classic MSAL `MsalUiRequiredException` scenario in the Web project. In-memory token cache clears on restart or eviction; session cookie still marks user as signed-in; subsequent protected API calls throw instead of silently re-acquiring a token. The fix requires applying `[AuthorizeForScopes]` attribute (or equivalent middleware challenge handling) to MVC controllers that call downstream APIs, per the microsoft-identity-web wiki pattern.

**Acceptance criteria:**
- All Web MVC controllers calling downstream APIs handle `MsalUiRequiredException` via re-challenge, not 500
- `[AuthorizeForScopes]` applied where missing
- Auth failures produce a transparent re-auth flow, not an error page

---

## Summary

Both issues are High priority. #527 goes to Trinity (API scope fix, tight scope). #528 goes to Ghost (MSAL/token lifecycle, broader auth middleware review across Web controllers).


# Oracle Decision Record: HTTP Security Headers Middleware (S6-6, Issue #303)

## Date
2026-03-19

## Author
Oracle (Security Engineer)

## Status
Pending Ghost review for CSP allowlist

---

## Context

Both the API and Web applications were missing standard HTTP security response headers, leaving
responses vulnerable to clickjacking, MIME sniffing, and cross-site scripting. Issue #303 requires
adding the full recommended header set to every response in both projects.

---

## Decisions

### 1. Implementation approach — inline `app.Use` middleware

Used `app.Use(async (context, next) => { ... })` in each `Program.cs` rather than a third-party
package (`NWebsec`, `NetEscapades.AspNetCore.SecurityHeaders`). Rationale: zero new dependencies,
the header set is small and stable, and the policy strings are clearly readable in one place. If
the policy grows significantly, migrating to `NetEscapades.AspNetCore.SecurityHeaders` is a low-cost
future refactor.

Middleware is placed **after** `UseHttpsRedirection()` so headers are only emitted on HTTPS
responses and are not duplicated on redirect responses.

### 2. Headers applied — API (`JosephGuadagno.Broadcasting.Api`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `DENY` | API has no legitimate iframe use; strictest setting |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy browser XSS auditor (superseded by CSP) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage on cross-origin navigation |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | API serves JSON only; no scripts/styles/frames needed. `frame-ancestors 'none'` reinforces DENY framing |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | Disable browser features not required by a REST API |

### 3. Headers applied — Web (`JosephGuadagno.Broadcasting.Web`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `SAMEORIGIN` | MVC app may legitimately frame its own pages (e.g. OAuth popups) |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy XSS auditor |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage |
| `Content-Security-Policy` | See §4 below | |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | No browser hardware features used |

### 4. Web Content-Security-Policy rationale

**Policy:**
```
default-src 'self';
script-src 'self' cdn.jsdelivr.net;
style-src 'self' cdn.jsdelivr.net;
img-src 'self' data: https:;
font-src 'self' cdn.jsdelivr.net data:;
connect-src 'self';
frame-ancestors 'self';
object-src 'none';
base-uri 'self';
form-action 'self'
```

**Directive-by-directive rationale:**

- **`default-src 'self'`** — safe fallback; anything not explicitly listed must come from the
  same origin.
- **`script-src 'self' cdn.jsdelivr.net`** — `'self'` covers all local JS bundles (jQuery,
  Bootstrap, site.js, schedules.edit.js, theme-support.js, the two new externalized scripts).
  `cdn.jsdelivr.net` is required in production for jQuery, Bootstrap bundle, FontAwesome JS,
  jquery-validation, and FullCalendar. No `'unsafe-inline'` — inline scripts were externalized
  (see §5).
- **`style-src 'self' cdn.jsdelivr.net`** — `cdn.jsdelivr.net` required in production for
  Bootstrap CSS, Bootstrap Icons CSS, and FontAwesome CSS. No `'unsafe-inline'` — the one inline
  `<style>` block in Calendar.cshtml was moved to `site.css`.
- **`img-src 'self' data: https:`** — `'self'` covers `/favicon.ico` and local images.
  `data:` is required for Bootstrap Icons (inline SVG data-URIs in the CSS). `https:` covers
  `@Settings.StaticContentRootUrl` favicon images whose exact hostname is a runtime setting
  (see open question §6).
- **`font-src 'self' cdn.jsdelivr.net data:`** — `cdn.jsdelivr.net` for FontAwesome woff2/woff
  files. `data:` covers any base64-encoded font fallbacks in vendor CSS.
- **`connect-src 'self'`** — all XHR/fetch calls go to the same origin (Engagements calendar
  events endpoint, API calls proxied by the Web app).
- **`frame-ancestors 'self'`** — paired with `X-Frame-Options: SAMEORIGIN`; allows same-origin
  framing, denies cross-origin.
- **`object-src 'none'`** — no Flash/plugin content.
- **`base-uri 'self'`** — prevents base tag injection attacks.
- **`form-action 'self'`** — all form POSTs must target the same origin.

### 5. Inline script/style externalization

Two inline `<script>` blocks were moved to dedicated JS files to avoid needing `'unsafe-inline'`
in `script-src`:

- `Views/MessageTemplates/Index.cshtml` → `wwwroot/js/message-templates-index.js`
  (Bootstrap tooltip initializer)
- `Views/Schedules/Calendar.cshtml` → `wwwroot/js/schedules-calendar.js`
  (FullCalendar initializer; no server-side data injection — uses an AJAX endpoint)

One inline `<style>` block from `Views/Schedules/Calendar.cshtml` (`#calendar` sizing) was moved
to `wwwroot/css/site.css`.

### 6. Open Questions for Ghost Review

1. **`img-src https:`** — This broad allowance was chosen because `Settings.StaticContentRootUrl`
   (used for favicons) is a runtime configuration value with an unknown hostname at code-time.
   Ghost should evaluate whether this should be tightened to the known static asset host
   (e.g., `https://static.josephguadagno.net`) and potentially read from config at startup.

2. **`cdn.jsdelivr.net` scope** — All CDN assets are pinned with SRI `integrity=` hashes in
   the Production `<environment>` blocks. The CSP host allowance is a belt-and-suspenders
   measure. Ghost should confirm no other CDN hostnames are referenced in any partial views
   not covered by this review.

3. **Nonce-based CSP** — A future improvement would replace the `cdn.jsdelivr.net` allowance
   with per-request nonces, eliminating CDN host trust entirely. Out of scope for S6-6.

---

## Files Changed

- `src/JosephGuadagno.Broadcasting.Api/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/message-templates-index.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/schedules-calendar.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/css/site.css` — calendar style appended
- `src/JosephGuadagno.Broadcasting.Web/Views/MessageTemplates/Index.cshtml` — inline script removed
- `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Calendar.cshtml` — inline script and style removed


# Scope Audit — Issue #527 Follow-up

**Date:** 2026-03-20  
**Author:** Trinity  
**Related Issue:** #527

## Finding: All Controllers Clean

After auditing all three API controllers for fine-grained scope gaps:

| Controller | Endpoint | Scope Check | Status |
|---|---|---|---|
| EngagementsController | GetEngagementsAsync | Engagements.List, Engagements.All | ✅ |
| EngagementsController | GetEngagementAsync | Engagements.View, Engagements.All | ✅ |
| EngagementsController | CreateEngagementAsync | Engagements.Modify, Engagements.All | ✅ |
| EngagementsController | UpdateEngagementAsync | Engagements.Modify, Engagements.All | ✅ |
| EngagementsController | DeleteEngagementAsync | Engagements.Delete, Engagements.All | ✅ |
| EngagementsController | GetTalksForEngagementAsync | Talks.List, Talks.All | ✅ |
| EngagementsController | GetTalkAsync | Talks.View, Talks.All | ✅ (fixed in PR #526) |
| EngagementsController | CreateTalkAsync | Talks.Modify, Talks.All | ✅ |
| EngagementsController | UpdateTalkAsync | Talks.Modify, Talks.All | ✅ |
| EngagementsController | DeleteTalkAsync | Talks.Delete, Talks.All | ✅ |
| SchedulesController | GetScheduledItemsAsync | Schedules.List, Schedules.All | ✅ |
| SchedulesController | GetScheduledItemAsync | Schedules.View, Schedules.All | ✅ |
| SchedulesController | CreateScheduledItemAsync | Schedules.Modify, Schedules.All | ✅ |
| SchedulesController | UpdateScheduledItemAsync | Schedules.Modify, Schedules.All | ✅ |
| SchedulesController | DeleteScheduledItemAsync | Schedules.Delete, Schedules.All | ✅ |
| SchedulesController | GetUnsentScheduledItemsAsync | Schedules.UnsentScheduled, Schedules.List, Schedules.All | ✅ |
| SchedulesController | GetScheduledItemsToSendAsync | Schedules.ScheduledToSend, Schedules.List, Schedules.All | ✅ |
| SchedulesController | GetUpcomingScheduledItemsForCalendarMonthAsync | Schedules.UpcomingScheduled, Schedules.List, Schedules.All | ✅ |
| SchedulesController | GetOrphanedScheduledItemsAsync | Schedules.List, Schedules.All | ✅ |
| MessageTemplatesController | GetAllAsync | MessageTemplates.List, MessageTemplates.All | ✅ |
| MessageTemplatesController | GetAsync | MessageTemplates.View, MessageTemplates.All | ✅ |
| MessageTemplatesController | UpdateAsync | MessageTemplates.Modify, MessageTemplates.All | ✅ |

**Conclusion:** No additional scope gaps found. The fine-grained scope rollout from PR #526 is complete.


# Decision: API Pagination Pattern

**Author:** Trinity  
**Date:** 2026-03-20  
**Context:** Issue #316 - Add pagination to all list API endpoints

## Decision

All list endpoints in API controllers use **query parameter-based pagination** with `PagedResponse<T>` wrapper.

## Pattern

```csharp
// Add using statement
using JosephGuadagno.Broadcasting.Api.Models;

// Endpoint signature
public async Task<ActionResult<PagedResponse<TResponse>>> GetItemsAsync(
    int page = 1, 
    int pageSize = 25)
{
    var allItems = await _manager.GetAllAsync();
    var totalCount = allItems.Count;
    var items = allItems
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(ToResponse)
        .ToList();
    
    return new PagedResponse<TResponse>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

## PagedResponse Model

Located at `src/JosephGuadagno.Broadcasting.Api/Models/PagedResponse.cs`:

```csharp
public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

## Defaults

- **page**: 1 (first page)
- **pageSize**: 25 (items per page)

## Rationale

1. **Query parameters** - RESTful convention for pagination, allows optional parameters
2. **Default values** - Backward compatible - omitting params gives sensible defaults
3. **Client-side pagination** - Current managers return full collections; pagination happens in controller (acceptable for current data volumes)
4. **Consistent wrapper** - `PagedResponse<T>` provides uniform structure across all list endpoints
5. **TotalPages calculation** - Derived property eliminates need for clients to calculate page count

## Endpoints Updated (Issue #316)

### EngagementsController
- `GET /engagements?page={page}&pageSize={pageSize}`
- `GET /engagements/{id}/talks?page={page}&pageSize={pageSize}`

### SchedulesController
- `GET /schedules?page={page}&pageSize={pageSize}`
- `GET /schedules/unsent?page={page}&pageSize={pageSize}`
- `GET /schedules/upcoming?page={page}&pageSize={pageSize}`
- `GET /schedules/calendar/{year}/{month}?page={page}&pageSize={pageSize}`
- `GET /schedules/orphaned?page={page}&pageSize={pageSize}`

### MessageTemplatesController
- `GET /messagetemplates?page={page}&pageSize={pageSize}`

## Special Cases: 404 Endpoints

Endpoints that return `404 NotFound` when no items exist (e.g., unsent, orphaned) check count **before** pagination to maintain existing behavior:

```csharp
var allItems = await _manager.GetUnsentScheduledItemsAsync();
if (allItems.Count == 0)
{
    return NotFound();
}
// ... then paginate
```

## Future Considerations

- If data volumes grow significantly, consider adding server-side pagination to managers/data stores
- Could add sorting parameters (e.g., `?sortBy=createdOn&sortDirection=desc`)
- Could add filtering parameters (e.g., `?status=unsent`)

## References

- PR #514: https://github.com/jguadagno/jjgnet-broadcast/pull/514
- Issue #316: https://github.com/jguadagno/jjgnet-broadcast/issues/316


# Decision: Scriban Template Seeding Strategy (Sprint 7)

**Date:** 2026-03-20  
**Decider:** Trinity (Backend Dev)  
**Epic:** #474 - Templatize all of the messages  
**Issues:** #475 (Bluesky), #476 (Facebook), #477 (LinkedIn), #478 (Twitter)

## Context

The Scriban template infrastructure was implemented in PR #491, adding:
- `MessageTemplate` domain model (Platform, MessageType, Template, Description)
- `IMessageTemplateDataStore` interface with SQL implementation
- Template lookup in all 4 `ProcessScheduledItemFired` functions with fallback to hard-coded messages
- Constants for platforms (Twitter, Facebook, LinkedIn, Bluesky) and message types (RandomPost, NewSyndicationFeedItem, NewYouTubeItem, NewSpeakingEngagement, ScheduledItem)

However, NO templates were seeded in the database, so the system always fell back to the hard-coded message construction.

## Decision

**Seed default Scriban templates via SQL migration script** instead of embedded resource files.

Created `scripts/database/migrations/2026-03-20-seed-message-templates.sql` with 20 templates (5 per platform).

## Options Considered

### Option 1: Database-backed templates (SQL migration) ✅ CHOSEN
**Pros:**
- Can be updated via Web UI (`MessageTemplatesController` already exists)
- No code deployment required to change templates
- Centralized storage in SQL Server (already used for all other configuration)
- Consistent with existing `IMessageTemplateDataStore` implementation

**Cons:**
- Requires database migration execution
- Not version-controlled alongside code (but migrations are)

### Option 2: Embedded resource files (.liquid or .scriban in Functions project)
**Pros:**
- Version-controlled with code
- No database dependency
- Faster lookup (no DB round-trip)

**Cons:**
- Requires code redeployment to update templates
- Would need new loader implementation (file reader)
- Inconsistent with existing `IMessageTemplateDataStore` interface

### Option 3: Azure App Configuration or Key Vault
**Pros:**
- Centralized cloud configuration
- Can be updated without deployment

**Cons:**
- Adds external dependency
- Higher latency than local DB
- More complex than necessary for this use case

## Template Design

### Field Model (Exposed to all templates)
Each platform's `TryRenderTemplateAsync` provides:
- `title`: Post/engagement/talk title
- `url`: Full or shortened URL
- `description`: Comments/engagement details
- `tags`: Space-separated hashtags
- `image_url`: Optional thumbnail URL

### Platform-Specific Templates

#### Bluesky (300 char limit)
- **NewSyndicationFeedItem**: `Blog Post: {{ title }} {{ url }} {{ tags }}`
- **NewYouTubeItem**: `Video: {{ title }} {{ url }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }}) {{ description }}`
- **ScheduledItem**: `My talk: {{ title }} ({{ url }}) {{ description }} Come see it!`
- **RandomPost**: `{{ title }} {{ url }} {{ tags }}`

#### Facebook (2000 char limit, link preview handles URL)
- **NewSyndicationFeedItem**: `ICYMI: Blog Post: {{ title }} {{ tags }}`
- **NewYouTubeItem**: `ICYMI: Video: {{ title }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }})\n\n{{ description }}`
- **ScheduledItem**: `Talk: {{ title }} ({{ url }})\n\n{{ description }}`
- **RandomPost**: `{{ title }}\n\n{{ description }}`

#### LinkedIn (Professional tone)
- **NewSyndicationFeedItem**: `New blog post: {{ title }}\n\n{{ description }}\n\n{{ tags }}`
- **NewYouTubeItem**: `New video: {{ title }}\n\n{{ description }}\n\n{{ tags }}`
- **NewSpeakingEngagement**: `Excited to announce I'll be speaking at {{ title }}!\n\n{{ description }}\n\nLearn more: {{ url }}`
- **ScheduledItem**: `My talk: {{ title }}\n\n{{ description }}\n\nJoin me: {{ url }}`
- **RandomPost**: `{{ title }}\n\n{{ description }}\n\n{{ tags }}`

#### Twitter/X (280 char limit)
- **NewSyndicationFeedItem**: `Blog Post: {{ title }} {{ url }} {{ tags }}`
- **NewYouTubeItem**: `Video: {{ title }} {{ url }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }}) {{ description }}`
- **ScheduledItem**: `My talk: {{ title }} ({{ url }}) {{ description }} Come see it!`
- **RandomPost**: `{{ title }} {{ url }} {{ tags }}`

## Rationale

1. **Database-backed wins for flexibility**: The Web UI already has a MessageTemplates controller. Admins can tweak templates without code changes.
2. **Simple templates first**: Initial templates mirror the existing hard-coded logic. Future iterations can add Scriban conditionals (`if`/`else`), filters, etc.
3. **Platform limits enforced by code**: Functions already have fallback truncation logic. Templates don't need to handle character limits—they just provide the structure.
4. **Single migration for all platforms**: All 4 platforms share the same infrastructure, so a single SQL file seeds all 20 templates.

## Consequences

### Positive
- Templates are now customizable without redeployment
- Hard-coded fallback logic remains as safety net
- Web UI can manage templates (list, edit, update)
- Future templates can use Scriban's full feature set (conditionals, loops, filters)

### Negative
- Database must be migrated before templates take effect
- Templates are not co-located with code (but migrations are version-controlled)
- No compile-time validation of template syntax (errors logged at runtime)

## Implementation

**Commit:** `6c32c01` (pushed directly to `main`)  
**File:** `scripts/database/migrations/2026-03-20-seed-message-templates.sql`  
**Testing:** Build succeeds (Debug configuration). No unit tests needed for seed data.  
**Deployment:** Run migration script against production SQL Server to activate templates.

## Related

- **Epic:** #474 - Templatize all of the messages
- **Issues:** #475 (Bluesky), #476 (Facebook), #477 (LinkedIn), #478 (Twitter)
- **PR:** #491 - Original template infrastructure implementation
- **Domain Model:** `JosephGuadagno.Broadcasting.Domain.Models.MessageTemplate`
- **Data Store:** `JosephGuadagno.Broadcasting.Data.Sql.MessageTemplateDataStore`
- **Functions:** `ProcessScheduledItemFired` in Twitter, Facebook, LinkedIn, Bluesky folders

## Future Enhancements

1. **Conditional formatting**: Use Scriban `if`/`else` to vary messages based on field values (e.g., "Updated Blog Post" vs "New Blog Post" based on `item_last_updated_on`)
2. **Character limit enforcement in templates**: Add Scriban custom functions to truncate strings at specific lengths
3. **A/B testing**: Store multiple templates per (Platform, MessageType) and randomly select
4. **Localization**: Add a `Language` field to support multi-language templates
5. **Template validation**: Add UI preview/test functionality in the Web app



