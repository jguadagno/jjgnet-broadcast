# Session Log: Timezone and DateTime Decision Finalized

**Date:** 2026-05-26  
**Time:** 09:22:15-07:00  
**Coordinator:** Copilot  
**Session focus:** Capture and merge timezone/datetime architecture decision (GitHub issue #995)

---

## What was decided

Joseph Guadagno confirmed a unified datetime handling standard across the entire application:

- **Storage:** Always UTC (`datetimeoffset` in SQL, `DateTimeOffset` in C#)
- **Cron/schedule evaluation:** UTC-only (no per-schedule `TimeZoneId` needed)
- **Display:** Convert from UTC to user's local time in UI
- **Edit:** Present in user's local time; convert back to UTC before save

This is a **cross-cutting directive**, not limited to scheduler cron expressions — applies to all datetime fields.

---

## Why it matters

Resolves Trinity's open question: Do per-user schedules need `TimeZoneId` columns? Answer: No. All schedules store and evaluate in UTC. UI handles timezone conversion.

This clarification unlocks the schema design for:
- `UserPublisherSchedules`
- `UserRandomPostSettings`
- All new time-aware tables

---

## Merged to decisions.md

Three inbox files merged and deduplicated:
1. `copilot-directive-20260526-timezone-datetime.md` — Joseph's directive
2. `neo-995-architecture-confirmed.md` — Neo's architecture confirmation (all 5 decisions)
3. `trinity-995-review.md` — Trinity's schema recommendation (resolved by this decision)

All inbox files deleted.

---

## Next steps

Implementation team can now finalize table schemas without the timezone ambiguity. No per-user `TimeZoneId` columns needed.
