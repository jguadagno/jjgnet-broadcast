# Decision: .squad/ Housekeeping PR Policy

**Date:** 2026-05-08T08:37:47.421-07:00
**Author:** Neo
**Issue:** #803
**PR:** #934
**Status:** Implemented — pending merge

---

## Decision

`.squad/`-only PRs bypass the `pr-metadata` CI validation gate. Any PR where **every** changed file is under `.squad/` will skip branch-naming, PR-title-format, and single-issue-linking checks.

## Rationale

Agent housekeeping commits (state, decisions, history updates) are not feature work. Forcing them through the same strict naming and issue-linking gate as code PRs is unnecessary friction. The bypass is implemented in the existing `pr-metadata` job — no new infrastructure.

## Implementation

Modified `.github/workflows/ci.yml` `pr-metadata` job. After the Dependabot bypass, added:

```bash
changed_files="$(gh api "repos/${REPO}/pulls/${PR_NUMBER}/files" --jq '.[].filename' 2>/dev/null || true)"
if [[ -n "$changed_files" ]]; then
  non_squad_files="$(printf '%s\n' "$changed_files" | grep -v '^\.squad/' || true)"
  if [[ -z "$non_squad_files" ]]; then
    echo ".squad/-only PR on branch '$BRANCH_NAME' — skipping PR metadata validation."
    exit 0
  fi
fi
```

## Security Properties

- **Fail-secure**: API failure → validation proceeds normally. No bypass on error.
- **`build-and-test` unaffected**: runs on every PR.
- **`codeql-analysis` unaffected**: runs on every PR.
- **Scope-limited**: one non-`.squad/` file forces full validation.

## Team Impact

Scribe (or any agent) can now open a PR from any branch containing only `.squad/` changes without following the conventional-commit PR title or `Closes #N` body format.
