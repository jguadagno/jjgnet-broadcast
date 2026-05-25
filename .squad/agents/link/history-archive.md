# Link — History Archive

Archived DevOps and Git workflow context.

---

- **Git automatically skips already-applied commits during rebase**: The rebase skipped two commits (f814467 and 0975e43) that were already in main via the merged PR #592. This produced a clean linear history with no conflicts.
- **Shared refactoring absorbs base branch changes**: Because the Serilog deduplication work extracted the logging configuration into a shared `ConfigureSerilog()` method that already incorporated the #591 changes (Information-level logging in production), there were no merge conflicts during the rebase.
- **Post-rebase checklist**: Force-push with `--force-with-lease`, update PR base branch with `gh pr edit <number> --base main`, and verify build succeeds.

### Serilog Configuration Deduplication (Issue #314, PR #594)
- **Serilog project location**: `src/JosephGuadagno.Broadcasting.Serilog/` — contains `SerilogKeyGenerator.cs` and now `LoggingExtensions.cs`
- **Shared extension pattern**: `LoggingExtensions.ConfigureSerilog(IConfiguration, string applicationName, string logFilePath)` — encapsulates all Serilog bootstrap logic (MinimumLevel, enrichers, sinks)
- **All three entry points reference Serilog project**: Api, Functions, and Web all had `<ProjectReference Include="..\JosephGuadagno.Broadcasting.Serilog\..." />` before this change
- **Web was missing OpenTelemetry sink**: `WriteTo.OpenTelemetry()` was commented out in Web/Program.cs; the shared method now enables it for all three projects
- **Package versions must align**: Api, Functions, and Web all reference specific Serilog package versions (e.g., `Serilog.AspNetCore` 10.0.0, `Serilog.Enrichers.AssemblyName` 2.0.0, `Serilog.Sinks.OpenTelemetry` 4.2.0). The Serilog project must use matching versions to avoid NuGet downgrade errors.
- **#if DEBUG pattern preserved**: The shared method includes `#if DEBUG .MinimumLevel.Debug() #else .MinimumLevel.Information() #endif` to maintain the behavior established in issue #591

### 2026-04-07: GitHub Comment Formatting Skill Added
- Skill: .squad/skills/github-comment-formatting/SKILL.md now exists — canonical reference for formatting GitHub comments
- Rule: Use triple backticks for ALL fenced code blocks in GitHub content (PR reviews, issue comments, PR comments)
- Single backticks are for inline code only (single variable/method names, one line)
- Root cause of addition: PR #646 review used single-backtick fences; GitHub rendered broken inline code (words truncated, multi-line collapsed)
- Charter updated with enforcement rule (## How I Work)
- Read .squad/skills/github-comment-formatting/SKILL.md before posting any PR review or issue comment containing code


## Sprint 20 Conclusion — Orchestration & Decisions Management (2026-04-19T15:40:15Z)

**Decision Source:** Scribe processing (20260419T154015-link.md)

**Wrap-up Tasks Completed:**
- ✅ Merged PR #759 (4 unpushed commits from local main) after full CI pass
- ✅ Wrote .squad/orchestration-log/20260419T154015-link.md
- ✅ Wrote .squad/log/20260419T154015-pr-759-merge.md
- ✅ Merged 9 decision inbox files into .squad/decisions.md
- ✅ Deleted all .squad/decisions/inbox/*.md files
- ✅ Updated team member history files with Sprint 20 context
- ✅ Verified decisions.md size (738,980 bytes) — no immediate archival needed

**Retro Guardrails Proposal:** Recorded in decisions.md as part of Sprint 20 retrospective. Document outlines 6 waste patterns from sprint 18–20 reviews and proposes operational gates (pre-execution checklist, orchestration log dedup, cheap pre-checks) to reduce token cost per cycle from ~6,000 to near-baseline.

## Sprint 24/25 Transition — Post-#806 Local Repo Cleanup (2026-04-{DATE})

**Context:** After PR #805/#806 merged, local repo has mixed state: user work on `issue-767-scope-cleanup`, local main tracking changes, backup branches. User is taking manual scope/role work offline; squad cleans local state conservatively.

**Cleanup Completed:**
- ✅ Deleted stale `refs/original/*` backup refs (reflog artifacts from prior rebase recovery)
- ✅ Expired reflog entries; ran `git gc --prune=now` for orphaned object cleanup
- ✅ Pruned `origin/issue-767-scope-cleanup` remote-tracking ref (branch deleted on remote post-Sprint 24)

**Preserved (Intentional, Not Deleted):**
- ✅ `issue-767-scope-cleanup` local branch with 4 commits ahead of remote — **user work in progress, required for manual scope migration testing**
- ✅ `backup/issue-767-premerge` local branch — backup point for user testing recovery
- ✅ Local `main` at `7dba1e8` (+1 commit vs origin/main) — housekeeping commit documenting PR #806 payload cleanup; can fast-forward when user resumes
- ✅ `.squad/decisions/inbox/*` inbox files (3 files) — awaiting decision merge in next sprint cycle

**State After Cleanup:**
- Local branches: `backup/issue-767-premerge`, `issue-767-scope-cleanup` (checked out), `main`
- Remote tracking: `origin/main` (protected), `origin/HEAD`
- Worktree: single, no stale worktree clutter
- Git object store: clean, no orphaned refs

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


