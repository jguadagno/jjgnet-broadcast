# Link — History
## Executive Summary

**Link — Platform & DevOps**

- **Focus:** Branch management, Git workflow, environment setup, CI/CD
- **Recent:** Main branch repair, dead branch cleanup, Git history hygiene
- **Key:** SSH remotes for workflow scope, branch protection validation

---


## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-04-19 | **PR #759 Merge: Sprint 20 Wrap Docs to main** — Convert 4 unpushed commits on local main into GitHub PR and merge after CI passes. Protect uncommitted `.squad` changes in working tree. | ✅ PR #759 created from temporary branch `link/sprint20-wrapup-pr`, pushed to remote. All CI checks passed (build-and-test 2m21s, CodeQL 6m18s, GitGuardian). Merged with normal merge commit (not squash) at `33154c5`. Local main fast-forwarded. Uncommitted `.squad` changes left untouched in working tree. |
| 2026-04-19 | **Retro Guardrails: Reducing Token Waste & Directive Drift** — Analyze three sprints of repeated failures (PR #738, #739 multi-round reviews); propose operational guardrails to prevent duplicate work, enforce pre-submission validation, and reduce expensive GitHub API polling | ✅ Proposal written to `.squad/decisions/inbox/link-retro-guardrails.md`. Key findings: 6 waste patterns identified; ~6,000 tokens wasted per 3-cycle review due to missing pre-checks. Guardrails target: (1) pre-execution checklist gate, (2) orchestration log deduplication, (3) cheap checks before expensive work, (4) SOP for branch readiness. Implementation priority: P1 pre-hook + coordinator gate this week; P2 skill doc next sprint; P3 task tracking + webhook handling later. |
| 2026-04-19 | **Sprint 20 Wrap — Branch Cleanup & Orchestration** — Delete local branches after PR #756 and #757 merged; update main from origin/main; prune remote-tracking branches; record orchestration log | ✅ Deleted `issue-731-user-publisher-settings`, `issue-732-owner-isolation-tests`, `issue-745`, `neo/pr-recovery-731-732`. Pruned remote tracking branches. Local main now at `0bcc1fe` (origin/main HEAD) with clean working tree. Orchestration log recorded at `.squad/orchestration-log/2026-04-19T14-47-26Z-link-sprint20-cleanup.md`. |
| 2026-04-01 | **Serilog Configuration Deduplication (#314)** — Extract duplicate Serilog bootstrap from Api/Functions/Web into shared `LoggingExtensions.ConfigureSerilog()` method; Web gains OpenTelemetry sink | ✅ PR #594 opened, targets `issue-591-reduce-production-logging` branch (depends on PR #592). Web/Program.cs now has `WriteTo.OpenTelemetry()` enabled. |
| 2026-03-20 | **Rebase v2** — Re-rebase PRs #516 and #517 after main advanced again; monitored CI for both | ✅ Both branches rebased (same conflict pattern in `.squad/decisions.md`, resolved by taking origin/main). CI green on both. PRs ready for Neo review — both merged this session. |
| 2026-03-20 | Rebase PRs #516 and #517 (`squad/319-functions-retry-policies`, `squad/324-sql-size-cap`) onto main to pick up Api.Tests fix from #518 | ✅ Both branches rebased and force-pushed. One conflict each in `.squad/decisions.md` (housekeeping commit `862fd19` vs main's newer decisions) — resolved by taking origin/main's version. Comments posted on both PRs. |
| 2025-07-14 | Fix PR #511 CI — merge main into `feature/s8-328-wire-application-insights` to pick up PR #513 test renames | ✅ Clean merge, pushed successfully. Also resolved workflow conflict in `feature/s8-315-api-dtos` stash pop (kept origin/main Critical-only vuln gate). |
| 2025-07 | S8-328: Wire Application Insights in ServiceDefaults | PR #511 opened — `UseAzureMonitor()` uncommented in ServiceDefaults, package added, redundant calls removed from Api/Web/Functions |

## Learnings

> Earlier learnings archived to history-archive.md on 2026-05-25
