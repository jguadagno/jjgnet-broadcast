## Executive Summary

**Neo — Architectural Lead, Code Reviewer**

- **Recent focus:** PR #963 (publisher settings), #967 (secret naming), #950 (collector sanity check)
- **Key decisions:** .squad/-only CI bypass (PR #934), Enum type validation, Log sanitization in services
- **Top finding:** All 3 recent PRs had blocking issues requiring fixes (log injection, missing enum, namespace gaps)
- **Team impact:** Established pattern: DI constructor injection mandatory, LogSanitizer in service layer, enum types over raw strings
- **Files:** UserPublisherSettingService.cs, KeyVaultSecretNameBuilder, PR #963/#967/#950

---

## 2026-05-15 — PR #967 Formal Review: KeyVaultSecretNameBuilder Utility Extraction

**Status:** ✅ COMPLETE — BLOCKED ❌. Comment posted at https://github.com/jguadagno/jjgnet-broadcast/pull/967#issuecomment-4464503690.

**Verdict:** BLOCKED ❌ — 1 blocking finding (directive violation: `KeyVaultSecretOwnerType` enum not implemented)

**What was verified:**
- `KeyVaultSecretNameBuilder` static class: correct namespace, location, regex pattern ✅
- All 5 managers refactored; local `BuildSecretName` + private `SecretNameSanitizer` deleted ✅
- YouTube format preserved: `collector-{sanitizedOwner}-youtube-channel-{channelId}-api-key` ✅
- `LogSanitizer.Sanitize()` intact on all log args in all 5 managers ✅
- 11 new tests: all pass ✅
- Build: 0 errors, 0 code warnings ✅

**Blocking finding:**
Architectural decision (decisions.md 2026-05-15T14:06) specifies `KeyVaultSecretOwnerType ownerType` (enum) as the first parameter. Implementation uses `string type`. No `KeyVaultSecretOwnerType` enum exists anywhere in the codebase. Directive violation — BLOCKING.

**Fix required:** Create `KeyVaultSecretOwnerType` enum in Domain project, update `Build` signature, update all 5 call sites (4 `"publisher"` → `KeyVaultSecretOwnerType.Publisher`, 1 `"collector"` → `KeyVaultSecretOwnerType.Collector`).

**Non-blocking observation:**
- `discriminator` not sanitized — pre-existing behavior (old private method also didn't sanitize channelId). Not a regression, but utility should either sanitize it or document caller responsibility.

**Learnings:**
- When the architectural decision spec says "enum for type parameter," verify the enum type exists; using a raw string is a directive violation even when callers are all hardcoded literals.
- The `discriminator` concept (for YouTube channelId) was not in the original decision spec but is a correct additive improvement. Accept design additions that don't break the spec intent.

---

## 2026-05-15 — PR #963 Formal Review: Publisher Settings Phase 2

**Status:** ✅ COMPLETE — BLOCKED ❌. Comment posted at https://github.com/jguadagno/jjgnet-broadcast/pull/963#issuecomment-4464281385. Decision written to `.squad/decisions/inbox/neo-pr963-review.md`.

**Verdict:** BLOCKED ❌ — 1 blocking finding (3 instances of log injection).

**What was verified:**
- All 5 API controllers: `[IgnoreAntiforgeryToken]`, `[Authorize]`, per-action policies, `User.ResolveOwnerOid()`, `LogSanitizer` on all log args ✅
- All 4 typed manager implementations: `BuildSecretName` with `SecretNameSanitizer`, `Has*` booleans only, constructor injection ✅
- All 4 manager test classes: `[Theory]` `BuildSecretName` coverage; Bluesky has special-char test ✅
- Functions migration (`SendPost`, `SendTweet`, `PostPageStatus`): shim replaced with typed managers ✅
- DI registrations: API, Functions, `ServiceCollectionExtensions` all correct; shims removed ✅
- `ApiBroadcastingProfile.cs`: all 4 publisher mappings present ✅
- SQL migration: idempotent `IF OBJECT_ID` guard ✅
- `ControllerAuthorizationPolicyTests`: all 5 new controllers registered ✅
- Data store tests: `GetByIdAsync_ReturnsNullForMissingId` added to Twitter/LinkedIn/Facebook ✅
- Build: 0 errors, 0 warnings ✅

**Blocking finding:**
`UserPublisherSettingService.cs` — 3 log call sites pass user-controlled strings without `LogSanitizer.Sanitize()`:
1. Line 93: `setting.CreatedByEntraOid` in `SaveAsync` early-return warning
2. Lines 156-157: `platform` and `setting.CreatedByEntraOid` in unrecognized platform warning
3. Lines 164-167: both args in `LogSaveFailure` helper

Fix: wrap all three sites with `LogSanitizer.Sanitize()` — the `using` directive is already present (line 8).

**Non-blocking observations:**
- Twitter/LinkedIn/Facebook `BuildSecretName` `[Theory]` tests use `"owner-1"` only (no special-char case) — coverage gap, not blocking
- `SendTweet.cs` line 46: `tweetMessage.ImageUrl` pre-existing unsanitized log arg — predates this PR, track separately

**Learnings:**
- Web service rewrites (341 additions) can introduce log injection even when the API layer is clean. Always scan every `Log*` call in substantially-rewritten files against the `LogSanitizer.Sanitize()` requirement.
- The `using JosephGuadagno.Broadcasting.Domain.Utilities;` directive may already be present from other `LogSanitizer` calls in the same file — check before flagging a missing import.

---

## 2026-05-16 — GitHub Issue #975: Site Admin CRUD for Publisher/Collector Settings

**Status:** ✅ COMPLETE — issue created and opened

**Issue Details:**
- **Number:** #975
- **Title:** feat: Build Site Admin section for CRUD management of publisher and collector settings for any user
- **Labels:** squad, enhancement
- **Scope:** New feature for administrators to manage publisher and collector settings on behalf of end users

**Context:**
- Complements the per-user self-service settings pages already refactored (Trinity's 5 per-publisher controllers)
- Operational requirement: admins need the ability to troubleshoot, override, or reset any user's settings without requiring that user's login
- Self-contained architecture directive supports this: each publisher/collector already has dedicated controller/service

**Technical notes:**
- New admin controller: parallel to existing per-publisher (Bluesky, LinkedIn, Facebook, Twitter) + per-collector (YouTube, FeedSource, SpeakingEngagement, ScheduledItem) pattern
- API endpoint: `/Admin/Settings/{settingType}/{name}` where `settingType` ∈ {publishers, collectors} and `name` ∈ {bluesky, youtube, etc.}
- Authorization: `[Authorize(Policy = "AdminOnly")]` — only admins with elevated role
- Dependency: relies on Trinity's self-contained refactor being stable first

**Learnings:**
- Site admin CRUD is a planned follow-up, not a blocker for per-user refactors
- Self-contained directive makes admin override simple: admin controller just delegates to existing typed managers/services with `?adminOverride=true` or separate logic path
- Enqueuing this now keeps it visible for sprint planning

---

---

## 2026-05-17 — Created GitHub Issues #978 and #979: Onboarding and Default Templates

**Status:** ✅ COMPLETE — Issues created

**Issue #978: feat: Add post-approval user onboarding setup flow**
- **Goal:** Create guided onboarding flow post-approval for configuring collectors, publishers, and message templates
- **Key requirements:** Multi-step flow showing status, skip capability, navigation indicator
- **Integration point:** Existing `UseUserApprovalGate()` middleware in Web project
- **URL:** https://github.com/jguadagno/jjgnet-broadcast/issues/978

**Issue #979: feat: Provide default message templates for new publishers**
- **Goal:** Offer system-provided default templates for Bluesky, Twitter, LinkedIn, Facebook
- **Key requirements:** One-action adoption from defaults, full customization possible, no forced defaults if user already has template
- **Related:** References Issue #978 — onboarding flow's template step should offer these defaults
- **Service:** `MessageTemplateService.cs` (Web layer)
- **URL:** https://github.com/jguadagno/jjgnet-broadcast/issues/979

**Decisions:**
- Both issues labeled `enhancement` (no `squad:*` labels — future work, not assigned)
- Issue #979 explicitly references Issue #978 for workflow integration
- Issues complement recent publisher/collector settings refactor work

---

---

## 2026-05-18 — Publisher Architecture Analysis

**Status:** ✅ COMPLETE — Proposal written to `.squad/decisions/inbox/neo-publisher-architecture-proposal.md`

**Scope:** Full analysis of Bluesky/Facebook/LinkedIn/Twitter publisher managers and their Azure Functions.

**Key findings:**

1. **All four publisher managers carry five composition dependencies they should not own**: `ISocialMediaPlatformManager`, `IMessageTemplateDataStore`, `ISyndicationFeedItemManager`, `IYouTubeItemManager`, `IEngagementManager`. Their `ComposeMessageAsync()` + `TryRenderTemplateAsync()` logic is copy-pasted verbatim across all four.

2. **Composition is inconsistent across function types**: ScheduledItemFired functions delegate to manager; NewSyndication/RandomPost functions compose inline with hardcoded text. No Scriban in the non-scheduled path.

3. **`Twitter/SendTweet.cs` bypasses `TwitterManager` entirely** — builds its own `TwitterContext` from per-user credentials and calls `twitterContext.TweetAsync()` directly. The `TwitterContext` injected into `TwitterManager` is a global shared context — irreconcilable with per-user posting.

4. **`Bluesky/SendPost.cs` reimplements PostBuilder logic** that already exists in `BlueskyManager.PublishAsync()`.

5. **`MessageTemplate.CreatedByEntraOid` field is unused** in template lookup — all users share the same template; user-scoped templates are architecturally impossible today.

6. **Four redundant queue DTOs** (`BlueskyPostMessage`, `TwitterTweetMessage`, `FacebookPostStatus`, `LinkedInPostLink`) for structurally identical data. `LinkedInPostLink` in `Managers.LinkedIn.Models` referenced by Functions project — layering violation.

**Proposed solution (6 phases):**
- Phase 1: Extract `IPostComposer`/`PostComposer` (centralizes Scriban rendering)
- Phase 2: Extract `IMessageTemplateLookup` (user-scoped template resolution)
- Phase 3: Migrate all `Process*` Functions to use new utilities
- Phase 4: Strip composition from all four publisher managers
- Phase 5: Unify `Send*` Functions to use `manager.PublishAsync(SocialMediaPublishRequest)`
- Phase 6 (optional): Unify queue DTOs to `SocialMediaPublishRequest`

**Key files examined:**
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/BlueskyManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Facebook/FacebookManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.LinkedIn/LinkedInManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Twitter/TwitterManager.cs`
- `src/JosephGuadagno.Broadcasting.Functions/Bluesky/{SendPost,ProcessScheduledItemFired,ProcessNewSyndicationDataFired,ProcessNewRandomPost}.cs`
- `src/JosephGuadagno.Broadcasting.Functions/Twitter/{SendTweet,ProcessScheduledItemFired,ProcessNewSyndicationDataFired}.cs`
- `src/JosephGuadagno.Broadcasting.Functions/Facebook/{PostPageStatus,ProcessScheduledItemFired,ProcessNewSyndicationDataFired,ProcessNewRandomPost}.cs`
- `src/JosephGuadagno.Broadcasting.Functions/LinkedIn/{PostLink,ProcessScheduledItemFired,ProcessNewSyndicationDataFired}.cs`
- `src/JosephGuadagno.Broadcasting.Domain/Models/SocialMediaPublishRequest.cs`
- `src/JosephGuadagno.Broadcasting.Domain/Models/MessageTemplate.cs`
- `src/JosephGuadagno.Broadcasting.Domain/Interfaces/ISocialMediaPublisher.cs`

---

## Learnings

### 2026-05-15 — PR #963 Formal Review: Publisher Settings Phase 2

