
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
