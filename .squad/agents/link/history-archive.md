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
