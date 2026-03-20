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
