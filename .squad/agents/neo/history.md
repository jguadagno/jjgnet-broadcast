# History

> Learnings before 2026-04-25 archived to history-archive.md (2026-05-25)
> Learnings from 2026-05-19 to 2026-05-26 archived to history-archive.md
> (2026-05-29)

## Process-Flow Documentation Mermaid Migration — 2026-05-29

**Task**: Replaced 4 outdated draw.io SVG files with 9 Mermaid Markdown
documents in `docs/process-flows/`.

**Outcome**: All process flow diagrams now maintained as Markdown with
embedded Mermaid syntax. Collectors, dispatchers, and publisher routing paths
documented with current architecture. SVG files deleted.

**Decision**: neo-mermaid-diagrams.md captured and merged into decisions.md.

## Scalar Documentation Tags (API Controllers) — 2026-05-28

**Task**: Added [Tags] attributes to all 17 API controllers for Scalar
documentation grouping.

**Outcome**: All controllers now emit tag metadata for Scalar API docs. Build
and tests passed.

**Decision**: See neo-scalar-tags.md in decisions.md.

## Key Architectural Learnings (Archived)

### Token Storage Architecture (2026-05-21)

- UserOAuthTokens table is authoritative for per-user OAuth tokens
- Settings Manager (KV + SQL flags) handles app credentials only
- Facebook RefreshTokens.cs needs rewrite for per-user iteration

### Event Grid vs Per-User Dispatch (2026-05-26)

- Event Grid incompatible with per-user publisher selection
- Decision: replace with direct per-user queue dispatch
- New tables: UserRandomPostSettings, UserEventDispatcherMappings

### RandomPosts Query Efficiency (2026-05-26)

- Added NextRunDateUtc to UserRandomPostSettings for efficient scheduling
- GetAllDueAsync filters by due date instead of full-table read
- Eliminates 1,440 daily unnecessary full-table scans

### MSAL Session Persistence Regression (2026-05-19)

- Root cause: AbsoluteExpirationRelativeToNow override in
  MsalDistributedTokenCacheAdapterOptions
- Fix: Remove override, restore 14-day sliding expiration
- Status: Diagnosis delivered, awaiting Joseph approval

## Learnings

- 2026-05-29T08:10:34.554-07:00 — Mermaid flow node labels that include
  method signatures need double-quoted text, and process-flow docs should use
  repo-relative Markdown links for resolvable code and SQL references.
- 2026-05-29T11:04:30.725-07:00 — The current "Dispatchers" label spans two
  different concerns: the distributor routing layer and publisher/platform
  settings UI/API. Future cleanup should rename routing artifacts to
  "Distributor" while renaming the platform settings surface to a separate term
  such as "Platforms" instead of blanket-replacing everything with
  "Distributor".

## Recent Review Outcomes

- PR #998 (NextRunDateUtc efficiency): BLOCKED on log sanitization
- AutoMapper Engagement profile: Low-risk refactor recommended
- Collector-distributor-publisher flow documented

---

**Note**: This file will be auto-archived when exceeding 15360 bytes.
Pre-2026-05-19 learnings in history-archive.md.
