# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.

## Directives
### 2026-04-30T08:12:33-07:00: User directive
**By:** Copilot (via Copilot)
**What:** All work must have an issue and must start on a branch, then be committed and delivered through a pull request.
**Why:** User request — captured for team memory

---

### 2026-04-30T08:29:06-07:00: User directive
**By:** Copilot (via Copilot)
**What:** `.squad` notes are allowed in PRs when they are pertinent to that PR.
**Why:** User request — captured for team memory

---

### 2026-04-30T23-45-07Z: User directive
**By:** Copilot (via Copilot)
**What:** Do not include dotnet test .\src\ --no-build --verbosity normal --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader" in PR bodies; use the no-filter repo test command instead.
**Why:** User request — captured for team memory

---

### 2026-05-05T12:14:52: User directive
**By:** Joe (jguadagno) (via Copilot)
**What:** All manager class dependencies MUST be injected via constructor. Never use IServiceScopeFactory or the service-locator pattern (resolving dependencies from IServiceProvider inside methods). All dependencies — including ISocialMediaPlatformManager, IMessageTemplateDataStore, and any other service — must appear as constructor parameters. This applies to all managers across the codebase (LinkedIn, Twitter, Facebook, Bluesky, and any future platforms).
**Why:** User request — PR #925 was rejected for violating this rule. Captured for team memory to prevent recurrence.

---

---

# Current Sprint — 2026-05-15

### 2026-05-15T13:14:51-07:00: C# DI naming convention
**By:** Joseph Guadagno (via Copilot)
**What:** Constructor-injected parameter names must be camelCase. Private backing fields must be `_camelCase`. PascalCase parameters (as seen in BlueskyManager.cs `SyndicationFeedItemManager`, `YouTubeItemManager`) are non-conformant and should be corrected.
**Why:** Standard C# naming conventions. Consistency across the codebase.
**Reference:** `src\JosephGuadagno.Broadcasting.Managers.Bluesky\BlueskyManager.cs` (lines 39-40).

---

### 2026-05-15T13:49:35: User directives — Publisher settings refactor design decisions
**By:** Joe (via Copilot)
**What:**
1. **KV secret naming (runtime derivation):** `{type}-{ownerOid}-{name}-{setting_name}` where `{type}` = `collector` or `publisher`. Example: `publisher-{ownerOid}-bluesky-bluesky-password`. No explicit `*SecretName` columns.
2. **API route convention:** Use `/Publishers/{name}/Settings` and `/Collectors/{name}/Settings`.
3. **ShortLivedAccessToken (Facebook):** Persist as a secret in Key Vault.
4. **EventPublisherSettings:** Out of scope — application-level config, not user-scoped.
5. **All four publishers in scope:** Bluesky, LinkedIn, Facebook, Twitter/X.
6. **No production data migration needed:** `UserPublisherSettings` has never been used in production.
**Why:** User decisions answering Neo's five open questions from the publisher settings architectural analysis.

---

# Architecture Decision: Publisher Settings Refactor — Architectural Recommendation

**Date:** 2026-05-15T13:21:33-07:00
**Author:** Neo (Lead)
**Status:** Endorsed — Issues #958 (Phase 1) and #959 (Phase 2) created

Three concrete problems driving the refactor:
1. **Cleartext secrets in SQL.** `Settings NVARCHAR(MAX)` stores raw credential values. KV integration path in `GetCredentialsAsync` exists but is never populated by the write path.
2. **Magic string key names.** Typos fail at runtime, not compile time.
3. **Inconsistency with the collector pattern.** Alignment reduces cognitive load.

**Phasing:**
- **Phase 1 (#958):** SQL + EF models, new per-publisher tables, data stores. No API or Web changes.
- **Phase 2 (#959):** Manager, API, DTOs, Web, tests. After Phase 1 is deployed and validated.

**Constraint:** `EventPublisherSettings` is out of scope — infrastructure (Event Grid), not user-scoped credentials.

---

# Architecture Decision: Extract `BuildSecretName` to Shared Utility

**Date:** 2026-05-15T14:06:58.645-07:00
**Author:** Neo (Lead)
**Status:** Decision — fold into #961

**Location:** `JosephGuadagno.Broadcasting.Domain.Utilities.KeyVaultSecretNameBuilder` (static class)
**Namespace:** `JosephGuadagno.Broadcasting.Domain.Utilities`

**Signature:**
```csharp
public static string Build(
    KeyVaultSecretOwnerType ownerType,  // Collector or Publisher
    string ownerOid,
    string name,
    string settingName) → string
```

**Output:** `{ownerType}-{sanitizedOwnerOid}-{sanitizedName}-{sanitizedSettingName}`

**Examples:**
- `Build(Collector, ownerOid, "youtube-channel-{channelId}", "api-key")` → `collector-{oid}-youtube-channel-{channelId}-api-key`
- `Build(Publisher, ownerOid, "bluesky", "app-password")` → `publisher-{oid}-bluesky-app-password`

**Migration concern:** Changing KV secret name format breaks existing secrets at rest. Requires two-step deployment (write new key, verify Functions, delete old key). Existing `BuildSecretName` in `UserCollectorYouTubeChannelManager` is private — delete it, replace 3 call sites.

---

# Architecture Decision: Collectors Alignment with Publisher Settings Pattern

**Date:** 2026-05-15T13:55:01.142-07:00
**Author:** Neo (Lead)
**Status:** Decision — Issues #960 and #961 created

4 collector types found. Data layer already complete for all 4. No cleartext secrets anywhere.

**Gap summary:**

| Dimension | YouTube | FeedSource | SpeakingEngagement | ScheduledItem |
|---|---|---|---|---|
| API route `/Collectors/{name}/Settings` | ❌ | ❌ | ❌ | ❌ (missing entirely) |
| Web CRUD controller | ✅ | ✅ | ✅ | ❌ |
| Web service interface | ✅ | ✅ | ✅ | ❌ |
| KV naming `collector-` prefix | ❌ | N/A | N/A | N/A |
| No cleartext secrets | ✅ | ✅ | ✅ | ✅ |

**Key findings:**
- `HasApiKey` in YouTube is a **transient** domain property (populated at runtime from KV), not a SQL column.
- ScheduledItem is one-row-per-user (UNIQUE on `CreatedByEntraOid`) — matches publisher single-settings pattern.
- `youtube-channel-apikey-{owner}-{channel}` → missing `collector-` prefix (naming compliance gap, not security incident).

**Issues created:**
- **#960** — feat: align collector API routes to /Collectors/{name}/Settings and complete ScheduledItem layer
- **#961** — fix: align YouTube collector KV secret naming to collector- prefix convention

---

# Code Review: PR #963 — Publisher Settings Phase 2

**Date:** 2026-05-15
**Author:** Neo (Lead/Architect)
**Verdict:** BLOCKED ❌ → Fixed by Trinity (eda470e7) → UNBLOCKED ✅

**Blocking finding:** Log injection (`cs/log-forging`) in `UserPublisherSettingService.cs` — 3 `LogWarning` call sites passing user-controlled values without `LogSanitizer.Sanitize()`:
- Line 93: `setting.CreatedByEntraOid`
- Lines 156-157: `platform`, `setting.CreatedByEntraOid`
- Lines 164-167 (`LogSaveFailure`): `setting.CreatedByEntraOid`, `setting.SocialMediaPlatformName ?? setting.SocialMediaPlatform?.Name`

**What passed (19/19 other checks):**
- All 5 API controllers: `[IgnoreAntiforgeryToken]`, `[Authorize]`, ownership checks, `LogSanitizer` on all log args
- All 4 typed manager implementations: `BuildSecretName` with `SecretNameSanitizer`, `Has*` booleans only (no secret value exposure)
- All 4 manager test classes with `BuildSecretName` coverage
- Functions migration: all 3 files replace shim with typed managers
- DI registrations correct in API, Functions, Web
- SQL migration: idempotent guard present
- Build: 0 errors, 0 warnings

**Fix (eda470e7):** Trinity wrapped all 3 sites with `LogSanitizer.Sanitize()` and added missing `using JosephGuadagno.Broadcasting.Domain.Utilities;`.

---

# Decision: CollectorSettings — Inline Actions Removed, SpeakingEngagements Added

**Date:** 2026-05-12
**Author:** Trinity
**Branch:** issue-950-sanity-check

- Removed 6 redundant inline POST actions (`Add/Edit/DeleteFeedSource`, `Add/Edit/DeleteYouTubeChannel`) from `CollectorSettingsController`.
- `CollectorSettingsController` is now a **read-only page controller** — all mutations flow through dedicated controllers.
- `CollectorSettingsPageViewModel` gains `SpeakingEngagements` collection, populated via the admin/non-admin branch pattern.
- Inline `.Select(x => new ViewModel { ... }).ToList()` is the established projection pattern in `BuildPageViewModelAsync` — do not introduce AutoMapper.

---

# Frontend Decision: CollectorSettings Index Refactor

**Date:** 2026-05-12T08:29:04-07:00
**Author:** Sparks
**Branch:** issue-950-sanity-check

Replaced inline Add/Edit/Delete modals on `CollectorSettings/Index.cshtml` with redirect links to dedicated CRUD pages. Added Speaking Engagements card section with `bi-mic-fill` icon. Applied `<thead class="table-dark">` to all card tables.

**Pattern:** For settings-style pages that previously used modals, prefer redirect links to dedicated controller actions. Reserve modals for lightweight confirmations only.

---

# Decision: ApiKey Required for YouTube Channel — Split DTOs

**Date:** 2026-05-12
**Author:** Trinity
**Branch:** issue-950-sanity-check / Commit: 6a28f04

- **Create POST:** `CreateUserCollectorYouTubeChannelRequest` — `ApiKey` is `[Required]`.
- **Edit PUT:** `UpdateUserCollectorYouTubeChannelRequest` — `ApiKey` is `string?`; required only when `HasApiKey == false` on the existing record. Controller checks after fetch; Web Edit checks `viewModel.HasApiKey` from hidden field.
- `HasApiKey` is **server-computed** — ignore from client input via AutoMapper `.ForMember(d => d.HasApiKey, o => o.Ignore())`.
- Razor Edit view: round-trip `HasApiKey` via `<input type="hidden" asp-for="HasApiKey" />` — without it, Edit POST always receives `false`.

---

# CSRF Token Validation Sweep — issue-950-sanity-check

**Date:** 2026-05-12
**Author:** Trinity

Full audit of all API and Web MVC controllers. **No changes required** — all controllers were already compliant:
- API: all 8 controllers had `[IgnoreAntiforgeryToken]` at class level
- Web: all 13 controllers had `[ValidateAntiForgeryToken]` on every `[HttpPost]`

CodeQL `cs/web/missing-token-validation` alerts were stale. Read the actual files before applying mechanical security fixes.

---

# Decision: [IgnoreAntiforgeryToken] Sweep — API Controllers

**Date:** 2026-05-15T14:15:40.968-07:00
**Author:** Trinity

Full grep of all 10 API controllers. All already have class-level `[IgnoreAntiforgeryToken]`. **No code changes required.** Security bot alerts for 3 controllers were false positives — the class-level attribute was applied in a prior sweep. Do not add per-method attributes; class-level is the established pattern.

---

# Decision: Sanitize User-Controlled Values in UserPublisherSettingService Log Calls

**Date:** 2026-05-15
**Author:** Trinity
**Branch:** issue-959-publisher-settings-phase2 / PR #963 / Commit: eda470e7

Fixed Neo's blocking `cs/log-forging` finding: wrapped all 3 `LogWarning` user-controlled args with `LogSanitizer.Sanitize()`. Added `using JosephGuadagno.Broadcasting.Domain.Utilities;`.

**Learnings:**
- `LogSanitizer.Sanitize()` applies in the **service layer**, not just controllers.
- Private helper methods (like `LogSaveFailure`) are equally subject — audit the full file.
- Always check that `using JosephGuadagno.Broadcasting.Domain.Utilities;` is present in new files that log user-controlled data.

---

# Decision: Every migration that creates tables MUST also update table-create.sql

**Date:** 2026-05-16T09:52:31.833-07:00
**Author:** Morpheus (Data Engineer)
**Branch:** issue-972-end-user-validation
**Commit:** 7b88ea23

## Context

The Aspire AppHost (`AppHost.cs`) bootstraps the JJGNet database on fresh
environments by concatenating exactly three scripts:

1. `scripts\database\database-create.sql`
2. `scripts\database\table-create.sql`
3. `scripts\database\data-seed.sql`

Files under `scripts\database\migrations\` are **not** automatically run by the
AppHost. They are production deployment artifacts only.

## Problem Observed

Issue #972: `/Publishers/Settings/Index` threw `CommandError` ("Invalid object
name 'UserPublisherLinkedInSettings'") on any fresh Aspire-managed environment.
Root cause: the Phase 1 migration (`2026-05-15-publisher-settings-per-publisher-tables.sql`)
created the four per-publisher tables but never added them to `table-create.sql`.

## Decision

**Every migration PR that creates a new table MUST include the same DDL (with
`IF NOT EXISTS` guards) in `scripts\database\table-create.sql` in the same PR.**

This is a hard gate for all data-layer PRs. Reviewers should check that both
files are updated together.

## Rationale

- Omitting a table from `table-create.sql` silently breaks fresh environments
  (local Aspire, CI spin-ups, new developer onboarding).
- The IF NOT EXISTS guards make `table-create.sql` idempotent — safe for Aspire
  replay even if the migration has already been applied to a running database.

---

# Decision: Audit Web Service Base URL Constants on API Route Refactors

**Date:** 2026-05-16T09:01:41.335-07:00
**Author:** Trinity
**Branch:** issue-973 / PR #974
**Issue:** #973

## Decision

When API controller routes are refactored, the corresponding Web service `private const string` base URL constants must be updated in the same PR or as an immediate follow-up. These constants are the sole routing source of truth for Web→API calls and the compiler gives no indication when they go stale.

## Context

After the collector API route refactor (issue #960) moved controllers from `/UserCollector*` to `/Collectors/{name}/Settings`, three Web service files retained the old constants:

- `UserCollectorYouTubeChannelService.cs`: `/UserCollectorYouTubeChannels` → `/Collectors/YouTube/Settings`
- `UserCollectorFeedSourceService.cs`: `/UserCollectorFeedSources` → `/Collectors/FeedSource/Settings`
- `UserCollectorSpeakingEngagementService.cs`: `/UserCollectorSpeakingEngagements` → `/Collectors/SpeakingEngagement/Settings`

This caused all three collector CRUD operations to return 404 silently.

## Recommendation

Add a checklist item to the API route refactor process: after changing `[Route(...)]` on any API controller, grep the Web `Services/` folder for the old route string to catch stale constants.

