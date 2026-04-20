# Neo PR Comment Template

## When to use

Use this template for **any** Neo comment on a pull request. Neo posts
two types of comments:

1. **Formal Review** — When a PR has multiple discrete findings,
   subsystem-level analysis, or a clear approval decision
2. **Quick Finding** — When a single targeted blocker or guidance note
   is needed (e.g., pointing to specific data-seed misalignment)

Both types follow the same structure at the core: header, summary,
action, verdict.

---

## Template 1: Formal Structured Review

Use this when reviewing a completed feature branch, architecture change,
or complex implementation. Structure is: **title + summary +
checklist/findings + verdict + next step**.

```text
## Neo — PR #<number> Review

### Summary

<One-sentence core finding about what this PR does correctly or needs
to address.>

### Checklist Results

| Criterion | Status |
|-----------|--------|
| Criterion 1 | ✅ or ❌ with brief note |
| Criterion 2 | ✅ or ❌ with brief note |

### Detailed Findings

**✅ Subsystem 1:**
- Finding or observation
- Supporting detail

**⚠️ or ❌ Subsystem 2:**
- Issue or concern
- Why this matters

### Issues Found / No Issues Found

Summary of blocking issues if any, or affirmation if clean.

**Verdict: [APPROVED ✅ / BLOCKED ❌ / NEEDS REVISION]**

If blocked, state specific actions required. If approved, brief next
step (e.g., "Ready to merge" or "Author can merge after...")

*(Cannot use '--approve' flag because this is my own PR; posting as
comment)*
```

---

## Template 2: Quick Finding / Targeted Guidance

Use this when a single actionable issue, blocker, or guidance note is
the primary purpose. Structure is: **context + blocker + action**.

```text
Neo review note: Context or PR number reference if part of a stack.

Brief statement of blocker or guidance. Reason or impact if not obvious.

Specific action required.
```

**Example:**

```text
Neo review note: #770 is already merged and #771 is now the next branch
to clear, but this PR is blocked on clean-environment bootstrap.

scripts\database\data-seed.sql still seeds collector source rows
without CreatedByEntraOid, so the new fail-closed owner resolution has
no bootstrap path on a fresh database. Seed data needs alignment.

Align the seed data, then revalidate this PR against main.
```

---

## Guidelines

### Formal Review — When?

- ✅ Reviewing a completed feature branch that you authored or
  co-approved
- ✅ Multiple architectural findings or validation points to document
- ✅ Clear approval/blocking decision with reasoning
- ✅ Feature completeness checklist makes sense (e.g., "all manager
  overloads present")
- ✅ Pattern or subsystem-level analysis needed

### Quick Finding — When?

- ✅ Single blocking issue (e.g., seed data misalignment, missing
  scaffold removal)
- ✅ Guidance for author on next steps in a stacked PR scenario
- ✅ Brief note to unblock or clarify scope
- ✅ Targeted finding that doesn't require subsystem breakdown

### Form & Tone

- **Use Markdown tables** for checklist results (2–3 columns: criterion,
  status, note)
- **Organize by subsystem** in formal reviews (readers, managers,
  tests, etc.)
- **Use ✅, ❌, ⚠️** for quick status signals
- **Be specific**: Reference file paths, class names, line numbers when
  pointing to issues
- **Verdict at end**: APPROVED, BLOCKED, or NEEDS REVISION — make the
  decision clear
- **Avoid ambiguity**: "could be improved" or "minor observation" is
  not how Neo comments. Violations of directives are BLOCKING; findings
  are actionable

### Posting via `gh api`

Always post via PowerShell on Windows using `gh api` (not GitHub web
UI):

```powershell
$payloadPath = '.\gh-comment-<number>.json'
@{ body = @'
## Neo — PR #<number> Review
...comment body here...
'@ } | ConvertTo-Json -Compress | Set-Content -Path $payloadPath `
  -Encoding utf8

gh api repos\jguadagno\jjgnet-broadcast\issues\<number>\comments `
  --method POST --input $payloadPath

Remove-Item $payloadPath -Force
```

This avoids shell-escaping issues and produces a normal visible
comment (required for squad protocol on author-owned PRs).

---

## Examples from Production

### Example 1: PR #736 (Formal Review Pattern)

Title: `## Neo — PR #736 Review`  
Structure: Summary → Checklist table → Subsystem findings (✅ Readers,
✅ Managers, etc.) → No Issues Found → APPROVED verdict  
Format: Comprehensive, audit-trail quality, decision-forcing

### Example 2: PR #771 (Quick Finding Pattern)

Title: `Neo review note:`  
Structure: Context + blocker + specific action  
Format: Minimal, targeted, unambiguous next step

---

## Preservation Note

This template preserves existing `gh api` posting guidance and
Markdown/backtick conventions from
`.squad/skills/github-pr-comment/SKILL.md`. It adds structure and
decision-forcing criteria so Neo's comments are consistently
recognizable and actionable.
