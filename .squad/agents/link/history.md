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

- **Dirty worktree branch creation**: When the working tree has uncommitted changes but you need to convert unpushed commits to a PR, use `git branch <name> <sha>` (without switching branches) to create a new branch from HEAD without staging dirty content. This preserves uncommitted changes while allowing you to push a clean feature branch from the desired commit.
- **Branch-behind-main CI failures**: When a PR branch pre-dates a merge to main that renames symbols (controllers, methods), the CI merge commit will fail to compile. Fix is always `git merge origin/main --no-edit` on the feature branch — no rebase needed when the changes are non-overlapping.
- **Stash pop conflicts in shared workflow files**: Workflow files (`.github/workflows/*.yml`) change frequently across branches. When popping a stash onto a branch that has received main updates, expect conflicts in workflow steps. Always favour the `origin/main` version of vuln-scan logic since those decisions are made intentionally and tracked in PRs.
- **Stash hygiene**: After a conflicted stash pop, git leaves the stash entry in the list. Always `git stash drop stash@{N}` after manually committing the resolution.
- **Use `git worktree` for rebases in active repos**: When the main working tree has in-flight changes from other agents, `git worktree add ../tmp-wt <branch>` creates a clean isolated directory for rebasing. No stash juggling needed. Clean up with `git worktree remove` when done.
- **Rebase conflicts in squad housekeeping commits**: The `862fd19` commit (merging squad inbox into decisions.md) conflicts with main whenever main also updates decisions.md. During rebase, this is always resolvable by `git checkout --ours .squad/decisions.md` (take origin/main's version) — it is more up-to-date and the inbox content is already incorporated via other paths.
- **Rebase vs merge for CI fixes**: When branches are behind main due to a test fix, `git rebase origin/main` is preferred over merge — it produces a cleaner linear history on the PR and only the feature commit(s) show as new work.

### Application Insights / Azure Monitor wiring (S8-328)
- **ServiceDefaults was the gap**: `UseAzureMonitor()` was commented out in `ServiceDefaults/Extensions.cs` AND the `Azure.Monitor.OpenTelemetry.AspNetCore` package was missing from `ServiceDefaults.csproj`.
- **Api and Web had redundant unconditional calls**: Both `Program.cs` files called `services.AddOpenTelemetry().UseAzureMonitor()` unconditionally in their `ConfigureTelemetryAndLogging` — no connection string guard. ServiceDefaults owns this now with a proper `APPLICATIONINSIGHTS_CONNECTION_STRING` guard.
- **Functions uses a different pattern**: The isolated worker model uses `UseFunctionsWorkerDefaults()` (from `Microsoft.Azure.Functions.Worker.OpenTelemetry`) for worker-specific instrumentation. `UseAzureMonitorExporter()` was removed because ServiceDefaults' `UseAzureMonitor()` now handles the exporter centrally — including for Functions.
- **host.json** already had `telemetryMode: OpenTelemetry` — no change needed there.
- **Package versions**: Api and Web both referenced `Azure.Monitor.OpenTelemetry.AspNetCore` v1.4.0; ServiceDefaults now uses the same version for consistency.


### 2026-04-01 — Issue Specs batch #591 #575 #574 #573
- **Relevant specs:** `.squad/sessions/issue-specs-591-575-574-573.md`
- Neo specced four issues. Once implemented, these will generate PRs requiring branch management: #591 (standalone), #575 (independent), #574 (two-phase: Morpheus then Trinity), #573 (depends on #574 Trinity).
- Recommended ship order: #591 → #574-data → #575 → #574-api → #573.

### 2026-04-01 — PR #594 Rebase After Dependency PR #592 Merged
- **When a dependency PR merges, branches built on top must rebase onto main**: PR #594 (issue #314 Serilog deduplication) was originally branched from `issue-591-reduce-production-logging`. After PR #592 merged that work into main, the #314 branch needed to be rebased onto `origin/main`.
