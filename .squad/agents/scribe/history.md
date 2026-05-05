# Scribe — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Session Logger
- **Joined:** 2026-03-14T16:37:57.750Z

### Session: Sprint 29 Closeout → Sprint 30 Activation (2026-05-01)

- **Work:** Sprint milestone transition after PR #909 (#890 + #893 OAuth hardening) and PR #910 (misc fixes) merges to main
  - Updated `.squad/identity/now.md` to mark Sprint 29 COMPLETE and Sprint 30 ACTIVE
  - Changed team focus from PR submission phase to active implementation (#897 ISocialMediaPublisher interface)
  - Created session log documenting Sprint 29 deliverables, outcomes, and Sprint 30 sequence
  - Recorded #897 as active next implementation item (gating task for #902–#899–#900–#901 composition refactor chain)

- **Outcome:** Sprint transition complete. Sprint 30 sequence is clear: #897 (interface def) → #902 (Twitter) → #899 (Facebook) → #900 (Bluesky) → #901 (LinkedIn). Joe runs parallel infra tasks (#892, #856, #896) independently.

## Learnings

<!-- Append learnings below -->

### Session: RBAC Phase 2 Followup — Complete & Merged (2026-04-02)

- **Work:** Final consolidation and documentation for PR #612 (RBAC Phase 2 Followup)
  - Created 4 orchestration logs (Morpheus nullability, Switch RoleViewModel+Auth, Tank tests, Neo triage+review)
  - Wrote session log consolidating all 4 work packages (ebc5ba8, fc000a3, 66d5ba4 commits)
  - Merged 14 inbox decisions into decisions.md (nullability pattern, RoleViewModel pattern, self-demotion guard, auth layering, issue triage)
  - Cleared inbox files after merge (14 files deleted)
  - Documented follow-up (issue #613 — Viewer UX gap, Phase 3 readiness)
  - Prepared git commit (staged .squad/ directory)

- **Outcome:** All Phase 2 Followup items merged to main. 5 key patterns documented for team:
  1. Data.Sql nullability alignment with Domain models
  2. Web layer ViewModels don't reference Domain models
  3. Controllers guard against user self-demotion from critical roles
  4. Read-only operations use class-level auth, writes use method-level
  5. Read-only API endpoints accessible to Viewer role

- **Decisions Consolidated:**
  - morpheus-nullability-fix.md (pattern for nullability consistency across layers)
  - switch-role-viewmodel-and-auth-fixes.md (3 patterns: ViewModel abstraction, self-demotion guard, layered auth)
  - tank-rbac-phase2-followup-tests.md (test coverage for guard and ViewModel mapping)
  - neo-issue-triage-2026-04-02.md (34 issues triaged, all valid, no closes)
  - neo-pr-review-rbac-phase2-followup.md (PR #612 APPROVED)
  - 9 other copilot directives and phase decisions

### Session: Fix Web.Tests PagedResult<T> Mocks (2026-04-01)

- **Work:** Tank test fix session closeout
  - Created orchestration log for Tank's Web.Tests mock updates
  - Wrote session log summarizing fix for PagedResult<T> return types
  - Merged 2 inbox decisions into decisions.md (web-paging context + test pattern)
  - Cleared inbox files after merge
  - Staged/unstaged changes for git commit

- **Outcome:** Web.Tests project compiles cleanly, all 52 tests passing. PagedResult<T> mock pattern now documented in decisions.md for team reference.

### Session Consolidation: Orchestration + Merge Workflow (2026-03-22)

- **Work:** Deployment Approval Gate session closeout
  - Created issue #556 on behalf of team
  - Wrote orchestration logs for 3 agents (Cypher, Scribe, Neo) with work summaries
  - Wrote session log summarizing approval gate + staging cleanup pattern and next actions
  - Merged 2 inbox decisions into decisions.md, cleared inbox
  - Appended team updates to Cypher, Neo, and Scribe history.md files
  - Staged changes, unstaged runtime artifacts, committed to git

- **Pattern:** Scribe operations preserve full context flow:
  1. Orchestration logs capture what each agent did and status
  2. Session log ties work to issue/PR and summarizes for stakeholders
  3. Decision merge deduplicates and consolidates team knowledge
  4. History appends notify affected agents of team-level outcomes
  5. Git staging/unstaging keeps repo clean (squad/ metadata doesn't persist)

- **Key files:**
  - `.squad/orchestration-log/{timestamp}-{agent}.md` — Agent work summary
  - `.squad/log/{timestamp}-{feature}.md` — Session summary (brief, issue/PR focused)
  - `.squad/decisions.md` — Authoritative team decisions (merged from inbox)
  - `.squad/agents/{name}/history.md` — Agent's work log + learnings

### 2026-04-07: GitHub Comment Formatting Fix Session (neo-fix-encoding)
- Orchestration log: .squad/orchestration-log/2026-04-07T15-47-38Z-neo.md
- Session log: .squad/log/2026-04-07T15-47-38Z-encoding-fix.md
- Merged 18 inbox decisions into decisions.md (cypher x8, ghost x1, morpheus x1, neo x7, neo-github-comment-formatting x1)
- Added github-comment-formatting skill note to all 11 agent history.md files (neo, cypher, ghost, morpheus, switch, sparks, ralph, link, oracle, tank, trinity)
- Deleted 18 inbox files from .squad/decisions/inbox/
- Skill added: .squad/skills/github-comment-formatting/SKILL.md (canonical reference for GitHub comment formatting)

### 2026-04-07: GitHub Comment Formatting Skill Added
- Skill: .squad/skills/github-comment-formatting/SKILL.md now exists — canonical reference
- Rule: Triple backticks for fenced code blocks; single backticks for inline code only
- Charter updated with enforcement rule

### 2026-04-08T22-11-20Z — Epic #667 Architecture Decisions (Neo Session)
- Orchestration log: .squad/orchestration-log/2026-04-08T22-11-20Z-neo.md
- Session log: .squad/log/2026-04-08T22-11-20Z-667-decisions.md
- Merged 2 inbox decisions into decisions.md (neo-667-architecture-answers.md + neo-667-architecture-decisions.md), deduplicated
- Deleted 2 inbox files
- Appended epic #667 unblock notes to Morpheus, Trinity, Switch, Sparks, Tank history.md files

### Squad Upgrade Cleanup & History Compaction (2026-04-11)

**Files removed (stray duplicates from commit 0629a27):**
- Casting state at `.squad/` root: casting-history.json, casting-policy.json, casting-registry.json
- Template duplicates at `.squad/` root: charter.md, constraint-tracking.md, copilot-instructions.md, history.md, issue-lifecycle.md, mcp-config.md, multi-agent-format.md, orchestration-log.md, plugin-marketplace.md, raw-agent-output.md, roster.md, run-output.md, scribe-charter.md, skill.md
- Casting state in `.squad/templates/`: casting-history.json, casting-policy.json, casting-registry.json
- Deleted `.squad/templates/identity/now.md` (real copy at `.squad/identity/now.md`)

**Files moved to correct locations:**
- `.squad/templates/identity/wisdom.md` → `.squad/identity/wisdom.md`
- `.squad/templates/casting/Futurama.json` → `.squad/casting/Futurama.json`

**History files compacted (originals archived):**
| Agent | Before | After | Archive |
|-------|--------|-------|---------|
| Neo | 41.5KB | ~12KB | `.squad/agents/neo/history-archive.md` |
| Tank | 39KB | ~8KB | `.squad/agents/tank/history-archive.md` |
| Trinity | 30.7KB | ~9KB | `.squad/agents/trinity/history-archive.md` |
| Morpheus | 12.5KB | ~6KB | `.squad/agents/morpheus/history-archive.md` |
| Cypher | 12.3KB | ~6KB | `.squad/agents/cypher/history-archive.md` |

**Policy:** Full original content in `history-archive.md` beside each agent. Compact versions have dense Core Context + 2-3 most recent sessions verbatim.

### Session: Issue #708 Final Trace & Consolidation (2026-04-11)

- **Work:** Orchestration and decision consolidation for Sparks' Issue #708 fixes
- **Agents involved:** Sparks (completed work) → Scribe (logged and merged)
- **Created files:**
  - Orchestration log: `.squad/orchestration-log/2026-04-11T22-51-44Z-sparks.md` (both fixes documented)
  - Session log: `.squad/log/2026-04-11T22-51-44Z-issue-708-form-trace.md` (brief summary)
- **Decision merged:** `sparks-708-form-route-binding.md` merged to decisions.md (ASP.NET Core model binding pattern)
- **Inbox cleared:** 1 file deleted
- **History updated:** Sparks history.md appended with session completion note
- **Outcome:** Issue #708 fully resolved; decisions consolidated for team reference


### Session: PR #771 Comment Formatting Fix (2026-04-20)

- **Work:** Fix PR #771 GitHub comment with proper Markdown formatting
  - Updated existing GitHub comment 4284036318 to use Markdown backticks for code identifiers, paths, and PR references
  - Preserved all substantive feedback while correcting formatting violations
  
- **Outcome:** PR #771 comment now uses \path\`, \identifier\`, and #PR notation per team directive
- **Status:** ✅ COMPLETE

