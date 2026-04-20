# neo-pr-comment Skill

## Purpose

Establishes a canonical template and guidelines for Neo's pull request
comments. Ensures consistency across formal reviews and targeted
findings by defining two distinct comment modes with clear structure,
form, and decision-forcing criteria.

## What This Skill Provides

- **Formal Review Template** — For comprehensive multi-finding reviews
  with checklist results, subsystem analysis, and approval decision
- **Quick Finding Template** — For targeted blockers, guidance, or
  unambiguous next-step notes
- **Posting Guidance** — PowerShell `gh api` pattern for Windows-based
  comment posting (avoids shell-escaping issues)
- **Decision Framework** — Clear criteria for choosing between formal vs.
  quick mode

## When Neo Uses This

Every PR comment should follow one of the two templates:

1. **Use Formal Review** when:
   - Reviewing a completed feature branch
   - Multiple discrete findings or validation points to document
   - Subsystem-level analysis is appropriate (readers, managers, tests)
   - Clear approval/blocking decision with reasoning

2. **Use Quick Finding** when:
   - Single blocking issue (e.g., missing seed data, scaffold removal)
   - Guidance on next steps in stacked PR scenario
   - Brief targeted finding that doesn't need subsystem breakdown

## Key Principles

- **Structure**: Header + summary + findings/action + verdict + next
  step
- **Tone**: Direct, specific, decision-forcing. Violations of directives
  are BLOCKING (not "could be improved")
- **Signals**: Use ✅, ❌, ⚠️ for quick status; tables for checklist
  results
- **Specificity**: File paths, class names, line numbers when pointing
  to issues
- **Posting**: Always use `gh api` via PowerShell on Windows (produces
  visible comment, not approval artifact)

## Examples

See `.squad/skills/neo-pr-comment/TEMPLATE.md` for full template syntax
and production examples (PR #736 for Formal Review, PR #771 for Quick
Finding).

## Related Skills

- `.squad/skills/github-pr-comment/SKILL.md` — Base PowerShell `gh api`
  posting pattern (preserved and referenced)
