# Session Log: Publisher Architecture Q&A — All Questions Resolved

**Date:** 2026-05-18  
**Participants:** Joseph Guadagno (Joe), Neo (Lead), Scribe (Logger)  
**Status:** Joe answered all 5 open questions from Neo's publisher architecture proposal. Architecture is ready for finalization and GitHub issue creation.

---

## Context

Neo prepared a comprehensive publisher architecture proposal (in `.squad/decisions/neo-publisher-architecture-proposal.md`) addressing copy-pasted composition logic across four platform managers, inconsistent template handling, and per-user credential flow gaps. The proposal outlines a 6-phase migration path and deferred 5 open questions to Joe for product decision.

---

## Joe's Answers to All 5 Open Questions

### Q1: Twitter Credential Model — Per-User Only

**Question:** Should the design support a shared/system Twitter account for system-level posts, or commit entirely to per-user credentials?

**Answer:** **Per-user credentials only. No shared account.** All four platforms (Twitter, Bluesky, Facebook, LinkedIn) must use per-user credentials exclusively. System-level posts are out of scope.

**Implication:** `TwitterManager.PublishAsync()` will build its own `TwitterContext` from credentials in `SocialMediaPublishRequest` (four OAuth fields: consumer key, consumer secret, access token, access token secret). The current global `TwitterContext` DI injection is removed entirely.

---

### Q2: Queue DTO Unification Timing — In-Scope Now

**Question:** Should phase 6 (queue DTO unification) defer to a future sprint with a maintenance window, or include it now?

**Answer:** **Include it now.** Unify `BlueskyPostMessage`, `TwitterTweetMessage`, `FacebookPostStatus`, `LinkedInPostLink` into a single `SocialMediaPublishRequest` queue type during the refactor. Deploy all platforms in one release.

**Implication:** Functions will be symmetric: every `Process*` outputs `SocialMediaPublishRequest`; every `Send*` accepts it. Fixes the `LinkedInPostLink` layering violation (Functions referencing `Managers.LinkedIn.Models`).

---

### Q3: User-Scoped Templates for All Event Types — Enabled

**Question:** Should user-scoped templates be enabled only for ScheduledItem events, or for all event types including `NewSyndicationFeedItem` and `RandomPost`?

**Answer:** **ALL event types must support user-scoped templates.** Including `NewSyndicationFeedItem`, `RandomPost`, and any other message template type. Users can customize templates for any event type.

**Implication:** `MessageTemplate` table must have rows for `NewSyndicationFeedItem`, `RandomPost`, `NewSpeakingEngagement`, `NewYouTubeData` types seeded at deployment. These types already use hardcoded inline templates; migration to Scriban makes them user-customizable.

**Related:** Issues #978 and #979 (from prior sessions) handle template creation and seeding workflows.

---

### Q4: IMessageTemplateLookup User-Scoped Fallback — Required (No Global Fallback)

**Question:** Should `IMessageTemplateLookup` try user-scoped first then fall back to global (users CAN override but are not forced to), or require user-scoped templates with no fallback?

**Answer:** **Templates are REQUIRED — no global fallback.** `IMessageTemplateLookup.GetAsync()` must find a user-scoped template. If no user-scoped template exists, the call fails (returns null or throws, to be determined during implementation). Users cannot compose content without an explicit template.

**Implication:** Deployment must seed `MessageTemplate` rows for all event types per platform before going live with this architecture. Missing templates will surface as composition failures at runtime (caught in Functions error handling).

---

### Q5: Hashtags in Template Text — Inline via {{ tags }}, Bluesky Parses Facets

**Question:** Should hashtags be populated on `SocialMediaPublishRequest` for the template renderer, and confirm that Bluesky renders them as facet objects post-composition?

**Answer:** **Yes, populate hashtags on the request.** The `Process*` functions populate `SocialMediaPublishRequest.Hashtags` from the source entity. Templates use `{{ tags }}` (joined string) for text platforms. **Bluesky renders hashtags as `HashTag` facet objects** appended after the rendered text body — the Bluesky publisher manager's `PublishAsync()` already handles this correctly. No change to Bluesky behavior needed.

**Implication:** Template variables for composition include `{{ tags }}` (comma-joined). Platform-specific rendering (Bluesky facets, Twitter/Facebook/LinkedIn inline) happens in the respective manager's `PublishAsync()` after text composition.

---

## Summary: Architecture Ready

With these answers, the publisher architecture proposal is **approved and ready for implementation**. The 6-phase migration path is confirmed:

1. **Phase 1** — Extract `IPostComposer` (low risk, immediate value)
2. **Phase 2** — Extract `IMessageTemplateLookup` (low risk)
3. **Phase 3** — Migrate `Process*` functions to use lookup + composer
4. **Phase 4** — Strip composition deps from publisher managers
5. **Phase 5** — Unify `Send*` functions to use `PublishAsync(SocialMediaPublishRequest)`
6. **Phase 6** — Unify queue DTOs (included now, not deferred)

**Next action:** Neo will finalize the proposal document and create a GitHub issue with implementation checklist.

---

## Files Modified in This Session

- `.squad/decisions/decisions.md` — Merged 7 inbox files (consolidated into single source)
- `.squad/decisions/inbox/` — All 7 markdown files removed (merged upstream)
- `.squad/log/2026-05-18-publisher-arch-qa-resolved.md` — This session log (new)
