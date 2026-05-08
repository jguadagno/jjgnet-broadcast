# Decision: Introduce IMessageTemplateManager Caching Layer

**Date:** 2025-07-14
**Author:** Trinity (Backend Dev)
**Issue:** #936 (part of #78 — Add caching to WebApi)
**PR:** #940
**Status:** Implemented

---

## Context

`MessageTemplatesController` called `IMessageTemplateDataStore` directly — the only API
controller in the project without a manager layer. Every request hit SQL Server with no
caching, and there was no place to intercept reads for cache invalidation on writes.

The `SocialMediaPlatformManager` was the established gold-standard pattern for
`IMemoryCache` caching in this project (see PR #935 / issue #933).

## Decision

Introduce `IMessageTemplateManager` (interface) and `MessageTemplateManager`
(implementation) following the `SocialMediaPlatformManager` pattern exactly:

- **Cache keys**
  - `MessageTemplate_All` — full unfiltered list, shared by paged admin + owner paths
  - `MessageTemplate_{platformId}_{messageType}` — individual item
- **Expiry** — 5-minute absolute, via `static readonly MemoryCacheEntryOptions`
- **Invalidation** — `InvalidateListCaches()` removes `MessageTemplate_All`; called
  from `UpdateAsync` (only mutating operation on this entity)
- **In-memory filtering** — `ApplyFilterSortPage` private helper applies `ownerEntraOid`
  filter, optional text filter (MessageType/Template), sort, and skip/take after
  retrieving the full cached list — avoids cache fragmentation from parameterized keys
- **`IMemoryCache` already registered** — `builder.Services.AddMemoryCache()` existed;
  no new DI registration needed
- **No NuGet package needed** — `Microsoft.AspNetCore.App` FrameworkReference in
  `Managers.csproj` already covers `Microsoft.Extensions.Caching.Memory`

## Consequences

- **Positive:** SQL reads for message templates are reduced dramatically; repeated
  controller calls within a 5-minute window hit only the in-memory cache
- **Positive:** Controller is now consistent with all other controllers — it calls a
  manager, not a data store directly
- **Positive:** Single invalidation point in `UpdateAsync`; cache never goes stale
  beyond 5 minutes even if invalidation were missed
- **Neutral:** Full list is loaded into cache on first read; for small tables like
  message templates this is a net win (no query per item), but would need revisiting
  if the table grew large
- **Negative (minor):** One additional abstraction layer; mitigated by the fact that
  the same pattern is used throughout the project

## Alternatives Considered

1. **Keep calling DataStore directly, add cache there** — rejected; DataStore is a
   generic repository layer and shouldn't contain business-level cache policies
2. **Per-query cache keys** — rejected; leads to cache fragmentation and complex
   invalidation logic for filtered/paged queries
3. **Distributed cache (IDistributedCache)** — out of scope for this issue; in-process
   `IMemoryCache` matches the pattern used by all other managers in this project
