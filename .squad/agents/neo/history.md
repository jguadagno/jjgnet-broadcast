## Executive Summary

**Neo — Architectural Lead, Code Reviewer**

- **Current focus:** Publisher architecture refactor #980 (Phases 1-4 complete), PR reviews (#963, #967, #950)
- **Key decisions:** Per-user credentials, IPostComposer extracted, IMessageTemplateLookup extracted, Phase 4 composition stripped
- **Architecture pattern:** Domain → Data/Managers → API/Web/Functions; composition layer separated via PostComposer; templates user-scoped
- **Team pattern established:** LogSanitizer mandatory for all user-controlled log args, DI constructor injection, enum types over raw strings
- **Phase 4 outcome:** All publisher managers simplified to single PublishAsync(SocialMediaPublishRequest) entry point; TwitterHealthCheck deleted; 19 files changed, 86 insertions, 1158 deletions; all tests pass

---

## Publisher Refactor #980 — Architecture Overview

**6-Phase refactor**: Extract PostComposer, MessageTemplateLookup, migrate Functions, strip manager composition, unify queue DTOs.

### Phase 1 ✅ COMPLETE (commit dbfa2589)
- Deleted 4 dead `*PublisherSettings` (plural) domain classes
- Created IPostComposer/PostComposer with Scriban 7.2.0
- Registered in API, Functions, Web
- Result: 422 tests pass

### Phase 2 ✅ COMPLETE (commit e63c4012)
- Created IMessageTemplateLookup/MessageTemplateLookup
- Extended SocialMediaPublishRequest with OwnerEntraOid + Twitter OAuth properties
- Added user-scoped template lookup (deferred to Phase 3)
- Result: all tests pass

### Phase 3 ✅ COMPLETE (commit 47e8ecec)
- Added user-scoped GetAsync() to IMessageTemplateDataStore
- Migrated all 20 Process* Functions to use IMessageTemplateLookup + IPostComposer
- Updated 4 ProcessScheduledItemFiredTests
- Result: 155 Functions tests pass

### Phase 4 ✅ COMPLETE (current)
- Stripped ComposeMessageAsync/TryRenderTemplateAsync/GetMessageType from all 4 managers
- Twitter rewritten: per-user TwitterContext from SocialMediaPublishRequest credentials
- Removed global TwitterContext DI, TwitterHealthCheck deleted
- Result: 19 files changed, 86 insertions(+), 1158 deletions(-); all 422 tests pass

---

## Key Architectural Decisions

1. **Per-user credentials only** — TwitterManager builds TwitterContext from SocialMediaPublishRequest; no global context
2. **request.Text is canonical** — Composed output by PostComposer, received by managers as-is
3. **Hashtags inline via template** — {{ tags }} variable; Bluesky parses AT Protocol facets from request.Text
4. **Templates required, no fallback** — IMessageTemplateLookup.GetAsync() returns null; Process* functions bail on null
5. **Composition centralized** — PostComposer owns all Scriban rendering; managers own platform APIs only

---

## Recent PR Reviews

**#963** (Blocking ❌) — 3 log injection sites in UserPublisherSettingService; requires LogSanitizer.Sanitize() wraps  
**#967** (Blocking ❌) — KeyVaultSecretOwnerType enum required but missing from Domain; string parameter violates directive  
**#950** (Blocking) — Enum type validation across scope

---

## Issues Tracked

- **#975** — Site admin CRUD for publisher/collector settings (future enhancement)
- **#978** — Post-approval user onboarding setup flow (prerequisite for full template coverage)
- **#979** — Default message templates for new publishers (prerequisite for full template coverage)
- **#980** — Publisher architecture refactor (current, Phases 1-4 complete; Phase 5-6 pending)

---

## Implementation Patterns Established

- **Template composition**: Every Process* function → fetch entity → validate ownerEntraOid → build SocialMediaPublishRequest → lookup template → compose → return DTO
- **Null handling**: Null template/ownerEntraOid/composed text → log warning (with LogSanitizer), return null (skip enqueue)
- **DI pattern**: TryAddScoped<I, T>() in each consumer (API, Functions, Web) Program.cs
- **LinkedIn**: IUserOAuthTokenManager retained for OAuth; OAuth moves to PostLink function in Phase 5
- **Tests**: All manager unit tests updated; Twitter tests rewritten for per-user credentials

---

