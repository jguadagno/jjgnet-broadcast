# Archived Decisions

Archive of team decisions from before 2026-05-18.  
See decisions.md for current decisions.

---
## # Phase 6 Complete — #980 Publisher Architecture Refactor

**Date:** 2026-05-15  
**Author:** Neo  
**Branch:** `issue-980-publisher-architecture-refactor`  
**Status:** COMPLETE
---

---
## Phase 6: Delete Obsolete Platform Queue DTOs

### Files Deleted

**Domain model DTOs (zero type references in production code):**

| File | Reason |
|------|--------|
| `Domain/Models/Messages/FacebookPostStatus.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/LinkedInPostLink.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/TwitterTweetMessage.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/LinkedInPostImage.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/LinkedInPostText.cs` | Replaced by `SocialMediaPublishRequest`; no callers |

**Orphaned LinkedIn functions (queues no longer fed by any Process* function):**

| File | Reason |
|------|--------|
| `Functions/LinkedIn/PostImage.cs` | Listened on `linkedin-post-image`; no Process* function enqueued to it; functionality fully covered by `PostLink.cs` + `LinkedInManager.PublishAsync()` which handles `ImageUrl` |
| `Functions/LinkedIn/PostText.cs` | Listened on `linkedin-post-text`; no Process* function enqueued to it; functionality covered by `PostLink.cs` |
| `Functions.Tests/LinkedIn/PostImageTests.cs` | Tests for deleted function |
| `Functions.Tests/LinkedIn/PostTextTests.cs` | Tests for deleted function |

**Domain/Models/Messages/ now contains only `Email.cs` (as intended).**

### Constants Cleaned Up

Removed from `Domain/Constants/Queues.cs`:
- `LinkedInPostText = "linkedin-post-text"`
- `LinkedInPostImage = "linkedin-post-image"`

Removed from `Domain/Constants/ConfigurationFunctionNames.cs`:
- `LinkedInPostText`
- `LinkedInPostImage`

Removed from `Domain/Constants/Metrics.cs`:
- `LinkedInPostText`
- `LinkedInPostImage`

All remaining constants (`LinkedInPostLink`, `FacebookPostStatusToPage`, etc.) are still active — they name the queue channels used by the new `SocialMediaPublishRequest` pipeline.

### Remaining References (All Valid)

References like `Queues.LinkedInPostLink`, `Queues.FacebookPostStatusToPage`, `ConfigurationFunctionNames.LinkedInPostLink`, and `Metrics.LinkedInPostLink` remain — they are **queue channel names**, not DTO type references. The queue names did not change; only the message payload type changed (from old DTOs to `SocialMediaPublishRequest`).

### Build and Test Results

- **Build:** 0 errors, 0 warnings
- **Tests:** 1,222 passed, 0 failed (41 skipped — integration tests requiring live services)
- **Commit:** `af705129` — `refactor(#980): Phase 6 — delete obsolete platform queue DTOs`
---

---
## Overall #980 Refactor Summary

### What the Codebase Looked Like Before

Each social media platform had:
- **A platform-specific queue DTO** (`FacebookPostStatus`, `TwitterTweetMessage`, `LinkedInPostLink`, `LinkedInPostImage`, `LinkedInPostText`) used as the queue message payload
- **Hard-coded credentials** embedded in the DTO (access tokens, author IDs passed through the queue)
- **Process* functions** that composed the full message text inline using hard-coded templates
- **Send* functions** that dequeued the platform-specific DTO and called platform-specific manager methods directly
- **Manager methods** that took platform-specific parameters (text, accessToken, authorId, etc.)

### What the Codebase Looks Like Now

**One unified queue message type:** `SocialMediaPublishRequest` is the queue payload across ALL 4 platforms.

**Pipeline:**
```
EventGrid event
  → Process* Function (composes message text via IPostComposer + per-user IMessageTemplateLookup)
  → enqueues SocialMediaPublishRequest{Text, Title, LinkUrl, Hashtags, OwnerEntraOid, ...}
    to platform queue
  → Send* Function dequeues SocialMediaPublishRequest
  → resolves per-user credentials via Settings Manager (KeyVault-backed)
  → hydrates request.AccessToken, request.AuthorId
  → calls manager.PublishAsync(request)
  → platform manager dispatches internally based on request fields
```

**Key architectural improvements:**
1. **No credentials in the queue** — `OwnerEntraOid` travels instead; credentials resolved at send time from per-user settings
2. **No hard-coded templates** — `IMessageTemplateLookup` fetches per-user, per-platform templates from the database
3. **Single `ISocialMediaPublisher` interface** — all platform managers implement `PublishAsync(SocialMediaPublishRequest)`
4. **`PostComposer`** handles Handlebars template composition centrally
5. **Per-user Twitter auth** — Twitter credentials (consumerKey, consumerSecret, accessToken, accessTokenSecret) all per-user from `IUserPublisherTwitterSettingsManager`
6. **LinkedIn image/text/link dispatch unified** — `LinkedInManager.PublishAsync()` inspects `request.ImageBytes`/`request.ImageUrl`/`request.LinkUrl` to pick the right LinkedIn API call

### Phase Summary

| Phase | Change |
|-------|--------|
| 1 | Deleted dead plural `*PublisherSettings`; created `IPostComposer`, `PostComposer`, `IMessageTemplateLookup` |
| 2 | Extended `SocialMediaPublishRequest`; created `MessageTemplateLookup`; DI registrations |
| 3 | User-scoped `GetAsync` on `IMessageTemplateDataStore`; migrated all 20 Process* Functions |
| 4 | Stripped composition from all 4 publisher managers; Twitter per-user auth |
| 5 | Send* Functions dequeue `SocialMediaPublishRequest`; all managers use `PublishAsync(request)` |
| 6 | Deleted all obsolete platform-specific queue DTOs; removed dead functions and constants |

### Branch State

Branch `issue-980-publisher-architecture-refactor` is **ready for review**.  
Pending PRs #978 and #979 should merge first if they affect shared infrastructure.
---

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
---

