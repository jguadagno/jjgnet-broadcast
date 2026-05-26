# Decision: RandomPosts log sanitization fix

**Author:** Tank  
**Date:** 2026-05-26T16:27:09.011-07:00  
**Requested by:** Joseph  
**Status:** Verified

---

## Context

Neo blocked the PR because `src\JosephGuadagno.Broadcasting.Functions\Publishers\RandomPosts.cs` passed the externally controlled RSS title (`syndicationFeedItem.Title`) directly into a logger call, violating the `cs/log-forging` hard gate.

---

## Changes

- Confirmed `using JosephGuadagno.Broadcasting.Domain.Utilities;` was already present.
- Wrapped `syndicationFeedItem.Title` with `LogSanitizer.Sanitize(syndicationFeedItem.Title)` in the `logger.LogInformation(...)` dispatch message.
- Also sanitized the `title` value sent through `logger.LogCustomEvent(...)` so telemetry uses the same safe value.

---

## Verification

- `dotnet build .\src\ --no-restore --configuration Release` ✅
- `dotnet test .\src\ --no-build --configuration Release --verbosity normal --filter "FullyQualifiedName!~SyndicationFeedReader"` ✅
