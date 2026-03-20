# Decision: Sparks PR Batch Review — Forms UX & Accessibility

**By:** Neo (Lead)
**Date:** 2026-03-20
**Context:** Review of PRs #520, #522, #524 (Sparks' work)

---

## Decision: PR #520 — APPROVED (confirmed merged)

**PR:** feat(web): add loading/submitting state to forms (Closes #333)
**Branch:** squad/333-form-loading-state
**Status:** Squash-merged to main, branch deleted, issue #333 closed ✅

All criteria met:
1. JS uses existing jQuery — no new dependencies
2. Button re-enabled on `invalid-form.validate` (no permanent lock)
3. Calendar and theme toggle unaffected
4. Bootstrap 5 spinner markup correct
5. Change in `wwwroot/js/site.js` only

---

## Decision: PR #522 — HELD (code correct, CI red — not Sparks' fault)

**PR:** feat(web): add form accessibility (Closes #332)
**Branch:** squad/332-form-accessibility
**Status:** Open, awaiting fix of pre-existing AutoMapper issue

### Code Review: PASS (all 5 criteria met)
1. Every `<span asp-validation-for="X">` has `id="val-X"` ✅
2. Every input has `aria-describedby="val-{FieldName}"` ✅
3. `autocomplete` values correct (url for URLs, off for others) ✅
4. No structural changes — purely additive attributes ✅
5. WCAG 2.1 AA intent preserved ✅

### CI: FAIL (pre-existing issue from PR #523)

The `MappingTests.MappingProfile_IsValid` test fails because PR #523 (BlueSkyHandle
schema work) added `BlueSkyHandle` to `Domain.Models.Engagement` and
`Domain.Models.Talk` but did NOT add it to `Web.Models.EngagementViewModel` or
`Web.Models.TalkViewModel`. AutoMapper's `AssertConfigurationIsValid()` catches this.

PR #523 was merged at 15:21:46. PR #522's CI started at 15:21:52 (6 seconds later).
GitHub CI runs against the merged state with main, so #522 inherited the broken mapping.

### Required Fix (NOT Sparks' work)
1. Add `BlueSkyHandle` string? property to `EngagementViewModel`
2. Add `BlueSkyHandle` string? property to `TalkViewModel`
3. Ensure AutoMapper maps it (likely automatic via convention, or add `.Ignore()` if not
   yet exposed in the form)

Once fixed on main, Sparks should rebase #522 and re-run CI.

---

## Decision: PR #524 — APPROVED (confirmed merged)

**PR:** feat(web): add privacy page content (Closes #191)
**Branch:** squad/191-privacy-page
**Status:** Squash-merged to main, issue #191 closed ✅

All criteria met:
1. Placeholder replaced with real content — no TODO or lorem ipsum ✅
2. Appropriate for a personal broadcasting tool ✅
3. No broken HTML or Razor syntax ✅
4. Layout consistent with other content pages (Bootstrap table, standard headings) ✅

---

## Cross-PR Interference Pattern

When multiple feature branches are simultaneously open and one merges while another's CI
is queued/running, the second PR's CI will test against the merged state of main. This
means a branch with perfectly correct code can show red CI due to incomplete follow-on
work from a different PR.

**Protocol going forward:**
- When a schema/model PR (like BlueSkyHandle) merges, all open PRs against the same area
  should have their CI re-run after the follow-on ViewModel/mapping work is also merged
- Do not attribute a CI failure to the PR author without tracing the root cause
