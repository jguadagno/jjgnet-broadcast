# Neo PR Comment Decision Tree

Use this decision tree to choose between **Formal Review** and **Quick
Finding** for your next PR comment.

```text
START: About to comment on a PR

Is this a single, targeted issue or guidance note?
├─ YES → Quick Finding mode
│        Use when: one blocker, missing piece, or next step
│        Example: seed data misalignment, scaffold removal
│
└─ NO → Continue to next question

Are there multiple discrete findings across different subsystems?
├─ YES → Formal Review mode
│        Use when: readers, managers, tests all have observations
│        Document each subsystem under "Detailed Findings"
│
└─ NO → Continue to next question

Do you need to document a clear approval/blocking decision?
├─ YES → Formal Review mode
│        Use checklist for validation points, end with verdict
│
└─ NO → Continue to next question

Is this a feature branch you authored or co-approved that
deserves audit-trail quality documentation?
├─ YES → Formal Review mode
│        Set tone for team understanding of the change
│
└─ NO → Quick Finding mode
       Brief, focused, actionable
```

## Quick Decision Matrix

| Scenario | Mode | Why |
|----------|------|-----|
| Multiple subsystems to validate | Formal Review | Checklist and subsystem |
| Single seed data misalignment | Quick Finding | One issue, one action, done |
| Feature PR you wrote | Formal Review | Highest audit-trail standard |
| Author next step in stack | Quick Finding | Minimal, unambiguous |
| Architectural change + validation | Formal Review | Complex, needs subsystem |
| "Test needs new signature" | Quick Finding | Single fix, quick note |
| New reader overloads + tests | Formal Review | Coordinated across layers |

## Execution Checklist

Before posting your comment:

### Formal Review

- [ ] Title: `## Neo — PR #<number> Review`
- [ ] Summary: One sentence about what PR does right/wrong
- [ ] Checklist: 3–5 key criteria, each marked ✅ or ❌
- [ ] Findings: Organized by subsystem (readers, managers, tests)
- [ ] Verdict: APPROVED, BLOCKED, or NEEDS REVISION (make it bold)
- [ ] Next step: If blocked, specific actions; if approved,
      brief instruction
- [ ] Footer: `*(Cannot use '--approve' flag...)*` if self-review

### Quick Finding

- [ ] Title: `Neo review note:` followed by context if in stack
- [ ] Blocker: One sentence + why it matters
- [ ] Action: Specific next step (align seed data, remove scaffold,
      retarget branch, etc.)
- [ ] Keep it under 5 lines

## Posting (Both Modes)

Always use PowerShell + `gh api` on Windows:

```powershell
$payloadPath = '.\gh-comment-<number>.json'
@{ body = @'
<your comment body here>
'@ } | ConvertTo-Json -Compress | Set-Content -Path $payloadPath `
  -Encoding utf8

gh api repos\jguadagno\jjgnet-broadcast\issues\<number>\comments `
  --method POST --input $payloadPath

Remove-Item $payloadPath -Force
```

## Historical Examples

- **PR #736** (Formal Review): Multiple reader overloads + manager
  pass-throughs + test updates = comprehensive checklist-driven review
- **PR #771** (Quick Finding): Single blocker (seed data alignment) =
  targeted guidance with specific action

Both follow this skill's template; style differs only in scope.
