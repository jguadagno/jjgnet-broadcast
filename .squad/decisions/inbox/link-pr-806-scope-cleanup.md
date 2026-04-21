# Decision: PR #806 Payload Cleanup — One Issue Per Branch

**Date:** 2026-04-21  
**Author:** Link  
**Status:** Implemented

## Context

PR #806 (`issue-767-scope-cleanup`) was blocked for merge with message: _"Neo review outcome: blocked because unrelated `.squad/agents/*/history.md` files are included in the PR and violate one-PR-per-issue"_

The PR had accumulated history file changes from at least 3 team members' agents (Switch, Tank, Trinity) plus infrastructure docs (Ghost history, SKILL file) during the conflict resolution process.

## Problem

- PR should contain **only** issue #767 scope cleanup implementation
- Unrelated `.squad/` changes violate the "one issue per branch" rule
- The payload included 6 files that do not belong:
  - `.squad/agents/switch/history.md` (modified)
  - `.squad/agents/tank/history.md` (modified)
  - `.squad/agents/trinity/history.md` (modified)
  - `.squad/agents/ghost/history.md` (added)
  - `.squad/decisions/inbox/trinity-806-merge-conflict-resolution.md` (added)
  - `.squad/skills/git-pr-recovery/SKILL.md` (added)

## Solution

Rebuilt the branch as a **single clean commit** containing only the intended implementation:

**Files retained (14 total):**
- Domain: `src/.../Domain/Scopes.cs` (1)
- API: `src/.../Api/{Program.cs, Interfaces/ISettings.cs, Models/Settings.cs, XmlDocumentTransformer.cs, appsettings*.json}` (5)
- Web: `src/.../Web/{Program.cs, appsettings*.json}` (3)
- API Tests: `src/.../Api.Tests/{Controllers/*, Helpers/ApiControllerTestHelpers.cs, Infrastructure/*}` (4)

**Execution:**
1. Reset local to `origin/main`
2. Manually extracted issue-767 implementation files from remote branch
3. Staged clean payload (src/ only, no `.squad/`)
4. Committed with original PR message + Co-authored-by trailer
5. Force-pushed to `origin/issue-767-scope-cleanup`

**Result:** PR now shows 14 changed files (down from 20+), all related to issue #767. Ready for merge.

## Learnings

- **Squad file edits during merge conflict resolution**: When conflicts arise on a feature branch, avoid committing temporary `.squad/` housekeeping files. Merge conflicts should be resolved on source code only; squad updates belong in separate commit/PR if needed.
- **One-branch-per-issue enforcement**: The rule exists to keep PR payloads clean and reviewable. When accumulated unrelated changes appear, strip them before merge rather than letting them accumulate.
- **Force-push for payload cleanup**: A force-push after rebuilding the branch is acceptable when the goal is **payload cleanup** (removing unrelated files), not history rewrite. The feature implementation (the actual code changes) remains identical.

## Related Issues

- Closes issue #767 (via PR #806)
- Completes Neo's blocking review condition
