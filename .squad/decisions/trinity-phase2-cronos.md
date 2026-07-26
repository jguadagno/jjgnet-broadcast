# Decision: Cronos for Per-User RandomPosts Scheduling (Issue #995 Phase 2)

**Date:** 2026-05-26  
**Author:** Trinity  
**Status:** Accepted  
**Issue:** #995

---

## Context

Issue #995 Phase 2 required rewriting `RandomPosts.cs` from a global-settings-based
Event Grid dispatcher to a per-user timer function. Each user's `UserRandomPostSettings`
row stores a cron expression that controls when their random posts are dispatched.
The function needed to evaluate those expressions and dispatch directly to platform
queues, bypassing the Event Grid fan-out chain.

This decision record covers three choices made during implementation:

1. Cron library selection (Cronos vs alternatives)
2. Azure Functions timer format vs cron expression evaluation format
3. Direct queue dispatch vs retaining the Event Grid chain

---

## Decision 1: Use Cronos 0.13.0 for cron expression evaluation

**Chosen:** `HangfireIO/Cronos` v0.13.0  
**Rejected:** NCrontab, Quartz.NET, manual parsing

### Rationale

- **Standard 5-field cron:** Cronos parses standard POSIX/Vixie cron expressions
  (`"* * * * *"`) — the same format users understand and that tooling generates.
  NCrontab also supports 5-field but has a less precise API for windowed evaluation.
- **Windowed evaluation API:** `CronExpression.GetNextOccurrence(from, tz)` returns
  the next DateTime after a given instant. This makes the "did this cron fire in the
  last minute?" check trivial:
  ```csharp
  var next = expr.GetNextOccurrence(lastMinute.UtcDateTime, TimeZoneInfo.Utc);
  bool isDue = next.HasValue && next.Value <= utcNow;
  ```
- **Lightweight:** Cronos is a single-purpose NuGet with no transitive dependencies,
  unlike Quartz.NET (which brings a full scheduler runtime).
- **Already referenced indirectly:** Hangfire is in the broader ecosystem; Cronos is
  its extracted cron-only package, well-maintained and stable.

### Trade-off accepted

Cronos uses **5-field** standard cron (`min hr dom mon dow`), while Azure Functions
uses **6-field** cron with a leading seconds field (`sec min hr dom mon dow`). The
timer trigger expression `"0 * * * * *"` (fire at second 0 of every minute) is a
different format from the user-stored expressions `"*/15 * * * *"` (every 15 minutes,
5-field standard). These are evaluated by different libraries — Azure Functions runtime
for the timer trigger, Cronos for the per-user cron — and must not be mixed up.

---

## Decision 2: Azure Functions timer trigger fires every minute; Cronos evaluates user crons

**Pattern:** One system timer (every minute) + N per-user evaluations

### Rationale

The alternatives were:
1. One Azure Durable Entity or one timer per user — unmanageable at scale, requires
   orchestration infrastructure.
2. One timer per platform — doesn't solve per-user frequency variation.
3. One minute-granularity system timer + per-user cron evaluation — simple, stateless,
   scales to many users.

Option 3 was chosen. The system timer `"0 * * * * *"` fires every minute. For each
active `UserRandomPostSettings` row, the function evaluates whether the user's stored
cron expression was due in the last minute:

```csharp
var utcNow = DateTimeOffset.UtcNow;
var lastMinute = utcNow.AddMinutes(-1);
var next = CronExpression.Parse(settings.CronExpression)
               .GetNextOccurrence(lastMinute.UtcDateTime, TimeZoneInfo.Utc);
if (!next.HasValue || next.Value > utcNow.UtcDateTime) continue; // not due
```

This is stateless — no `NextRunAt` tracking required. At most one firing can occur per
user per minute, matching the minimum cron granularity.

### Trade-off accepted

A user with a cron expression like `"30 */2 * * *"` (at minute 30 of every even hour)
expects exactly one firing. With a system timer, if clock drift causes the minute-tick
to arrive slightly late (e.g., at 02:30:01 UTC), `GetNextOccurrence(02:29:00)` returns
`02:30:00`, which is ≤ `02:30:01` — correctly fires. If the timer fires at `02:30:59`
(still within the minute), the same check fires correctly. At-least-once semantics are
maintained. At-most-once within the minute is also maintained because
`GetNextOccurrence(lastMinute)` can only return the top of the current minute, and the
next occurrence after that is `04:30:00` — outside the window until that minute arrives.

---

## Decision 3: Direct queue dispatch instead of Event Grid fan-out

**Chosen:** `QueueServiceClient.GetQueueClient(name).SendMessageAsync(json)`  
**Rejected:** Retaining the `IEventPublisher.PublishRandomPostAsync()` Event Grid chain

### Rationale

The existing chain was:
```
RandomPosts → Event Grid → ProcessNewRandomPostOnBluesky
                         → ProcessNewRandomPostOnLinkedIn
                         → ProcessNewRandomPostOnTwitter
                         → ProcessNewRandomPostOnFacebook
```

With per-user routing, `RandomPosts` already knows which platform to target
(from `UserRandomPostSettings.PlatformId` via `UserEventPublisherMapping`). Publishing
to Event Grid and letting intermediate functions fan-out adds latency and cost with no
benefit — the intermediate functions did nothing except re-publish to the final queues.

Direct dispatch:
```csharp
var queueName = PlatformQueues[platformId]; // static dictionary
await client.GetQueueClient(queueName).CreateIfNotExistsAsync();
await client.GetQueueClient(queueName).SendMessageAsync(JsonSerializer.Serialize(request));
```

### Backward compatibility

The four `ProcessNewRandomPost*` Azure Functions remain in the codebase. They are no
longer invoked by `RandomPosts.cs` but they are not removed — any external Event Grid
publisher can still trigger them. This preserves the ability to roll back or to
support future Event Grid-sourced random posts from other components.

---

## Files affected

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Functions/Publishers/RandomPosts.cs` | Full rewrite |
| `src/JosephGuadagno.Broadcasting.Functions/JosephGuadagno.Broadcasting.Functions.csproj` | Cronos 0.13.0 added |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Publishers/RandomPostsTests.cs` | New file, 4 tests |
| `scripts/database/migrations/2026-05-26-per-user-publisher-routing-tables.sql` | New migration |
