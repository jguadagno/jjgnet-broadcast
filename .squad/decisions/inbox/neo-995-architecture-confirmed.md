# Decision: Issue #995 Architecture Confirmed

**Date:** 2026-05-26T08:59:04.287-07:00  
**Author:** Neo (Lead)  
**Status:** CONFIRMED  
**Trigger:** Joseph answered the five open architecture questions on
issue #995.

---

## Confirmed Decisions

1. **Storage model**
   - Joseph confirmed **Option B** from the earlier analysis.
   - In practice, that means using **dedicated normalized user-owned
     tables** for routing and scheduling instead of adding event-type flags
     onto the existing `UserPublisher*Settings` tables.
   - Existing per-platform publisher settings tables remain responsible
     for publisher-specific configuration only.

2. **Scheduling model**
   - Scheduling is **CRON-like**.
   - A user can define **multiple schedules per event type**.
   - Each schedule combines **event type + cron expression/frequency +
     target publisher(s)**.

3. **Collector event routing**
   - **Event Grid is removed for collector events too**, not just Random
     Post.
   - New speaking engagements, blog posts, videos, and other
     collector-driven events will use the same **user-selectable publisher
     routing** model.

4. **Random Post execution model**
   - `Publishers\RandomPosts.cs` should run on **one global timer every
     minute**.
   - It should poll **all users** and determine which schedules are due,
     following the same broad execution pattern as
     `Publishers\ScheduledItems.cs`.
   - Do **not** create per-user timer functions or per-user function
     instances.

5. **Migration and seeding**
   - Seed the new `UserRandomPostSettings` table with **Joseph's current
     global defaults**.
   - After the seed path exists, the old **global Random Post settings**
     can be removed.

---

## Resulting Implementation Scope

- Add new per-user scheduling and routing tables.
- Add `UserRandomPostSettings` for per-user content filtering
  (`CutoffDate`, `ExcludedCategories`).
- Replace Event Grid dispatch with direct per-user publisher routing for
  both Random Post and collector events.
- Add API and Web support so users can manage schedules, publisher
  targets, and Random Post settings.
- Remove the old global Random Post settings path after seed migration is
  in place.
