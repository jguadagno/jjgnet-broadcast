---
name: dev-lifecycle
description: >
  This skill should be used when the user asks to "build a feature", "fix a bug",
  "implement something", "start a dev cycle", types "/dev", or describes a software
  task that requires design, implementation, testing, and shipping. Orchestrates the
  full software development lifecycle from interrogation through shipping, with
  self-learning that improves over time.
version: 0.1.0
---

# Dev Lifecycle — Self-Learning Software Development Skill

A full-cycle software development orchestrator that grills the design, plans the work,
builds vertically, tests, instruments for observability, reviews, and ships — all within
a single continuous conversation that leverages the full context window.

## Activation

On every invocation, before any interaction with the user:

1. Read `references/engineering-knowledge.md` — prime with industry best practices
2. Read `references/ai-native-patterns.md` — prime with AI-native code principles
3. Read `references/process-learnings.md` — load codebase-specific patterns and past debt
4. Read `references/dev-log.md` — scan recent entries for recurring patterns (apply 3x rule)
5. Record `__cycle_start_time` — wall-clock timestamp for metrics

If the 3x pattern rule triggers on any process debt entry, surface it to the user immediately:
*"This is the Nth time [pattern]. Proposing: [fix]."* Ask whether to apply the fix to this cycle.

## Phase Definitions

### Phase 1: GRILL

Interrogate the feature/fix relentlessly until reaching shared understanding.

**Process:**
- Read the codebase first — explore relevant files, understand current architecture
- Walk down each branch of the design tree, resolving dependencies one-by-one
- Ask only what the codebase cannot answer — do not ask questions that can be resolved by reading code
- Target 5-8 questions for UI features, 8-12 for architecture changes, 3-5 for bug fixes

**Outputs:**
1. **Resolved Design** — the what, captured in concise bullet points
2. **Checkpoint Plan** — which phases need user approval before advancing:
   - Always checkpoint: Phase 1 (Grill output), Phase 7 (Review), Phase 8 (Ship)
   - Grill determines which of phases 2-6 need checkpoints based on complexity
   - Simple bug fix: `[Grill] → Build → Review → Ship` (2 checkpoints)
   - Complex feature: `[Grill] → [Plan] → [Scaffold] → Build → [Test] → Review → Ship`
3. **Timing estimate** — rough phase breakdown (not time predictions, just relative weight)

Present both outputs. Wait for user approval before proceeding.

### Phase 2: PLAN

Produce the implementation plan.

**Process:**
- List files to create, modify, or delete
- Define the sequence — what depends on what
- Identify architecture decisions and their trade-offs
- Reference `engineering-knowledge.md` principles that apply
- Check `process-learnings.md` for relevant codebase patterns

**Output:** Structured plan with file list, sequence, and rationale.

### Phase 3: SCAFFOLD

Create the skeleton before logic.

**Process:**
- Create new files with interfaces, types, function signatures
- Add TODO comments marking where logic will go
- Wire up imports, registrations, routing — the connective tissue
- Do NOT write implementation logic yet

**Output:** Compilable/parseable skeleton. No business logic.

### Phase 4: BUILD

Write the implementation, one vertical slice at a time.

**Process:**
- Implement one complete slice (backend + frontend + wiring) before starting the next
- Follow `engineering-knowledge.md` principles: small functions, clear names, explicit types
- Follow `ai-native-patterns.md`: clear file boundaries, no magic, self-documenting
- Reference `process-learnings.md` for codebase-specific conventions
- After each slice, verify it works before moving to the next

**Output:** Working implementation code.

### Phase 5: TEST

Write and run tests.

**Process:**
- Test at the boundaries — inputs, outputs, error cases
- Integration tests over mocks when touching real infrastructure
- Do not test framework internals or obvious getters/setters
- Run the tests — fix failures before advancing

**Output:** Passing test suite for the new code.

### Phase 6: OBSERVE

Add logging, metrics, and error boundaries.

**Process:**
- Add structured logging at key decision points (not every line)
- Add error boundaries with actionable messages (not generic catches)
- Consider: "If this breaks at 2am, can the oncall engineer debug it from the logs?"
- Reference observability principles from `engineering-knowledge.md`

**Output:** Instrumented code ready for production debugging.

### Phase 7: REVIEW

Self-review the complete diff.

**Process:**
- Review the full diff as if seeing it for the first time
- Check against `engineering-knowledge.md` principles
- Check against `ai-native-patterns.md` patterns
- Check for: security (OWASP top 10), performance (N+1, unnecessary re-renders), correctness
- Optionally invoke cross-model audit if the user requests it
- Present findings with severity: MUST FIX / SHOULD FIX / NIT

**Output:** Review summary. Fix MUST FIX items before advancing. Wait for user approval.

### Phase 8: SHIP

Commit, PR, push.

**Process:**
- Stage only relevant files (never .env, credentials, build artifacts)
- Draft commit message — concise, focused on "why" not "what"
- Commit under user's git identity (never Claude)
- Ask user: push? create PR? Neither?

**Output:** Clean commit on the branch.

## Phase Transitions

At each phase boundary:
1. Record `__phase_end_time` — wall-clock timestamp
2. Calculate phase duration
3. If the checkpoint plan marks this phase as a checkpoint:
   - Present phase output to the user
   - Wait for go/no-go
   - Record checkpoint wait start time
   - On user response, record checkpoint wait end time
4. If auto-advance: announce the phase transition briefly, continue

## Cycle Completion — Self-Learning

After Phase 8 (or if the user aborts), execute the self-learning loop:

### 1. Write Dev Log Entry

Append a structured entry to `references/dev-log.md`:

```markdown
## YYYY-MM-DD | <project> | <feature name>

- **Total duration:** Xmin (execution: Ymin, checkpoint-wait: Zmin)
- **Phase breakdown:** Grill: Xmin, Plan: Xmin, Build: Xmin, ...
- **Checkpoints hit:** N (list which, pass/revision/fail)
- **Files touched:** N new, N modified, N deleted
- **Issues caught at checkpoint:** (list)
- **Process debt identified:** (list)
- **Codebase patterns learned:** (list)
- **Outcome:** Merged / PR created / Aborted — reason
```

### 2. Update Process Learnings

If any new patterns or debt were identified, append to `references/process-learnings.md`:

**For codebase patterns:**
```markdown
### <Pattern Name> (project: <project>)
**Discovered:** YYYY-MM-DD
**Occurrences:** 1
**Pattern:** <what to do>
**Why:** <what went wrong without it>
```

**For process debt:**
```markdown
### <Debt Name>
**Discovered:** YYYY-MM-DD
**Occurrences:** 1
**Problem:** <what was inefficient>
**Proposed fix:** <recommendation for next cycle>
```

### 3. Apply 3x Rule

Scan `process-learnings.md` for any entry with occurrences >= 3.
- For codebase patterns: propose promotion to a permanent convention
- For process debt: propose a concrete change to the skill's phase logic
- Surface the proposal to the user. If approved, update the relevant file.

## Context Window Strategy

This skill operates in a single continuous conversation to maximize context coherence.
The 1M token window is the advantage — design decisions made in Grill remain visible
during Build, test failures during Test can reference the original Plan.

**High-signal token discipline:**
- Do not repeat information already in context — reference it
- Keep phase outputs concise and structured (tables, bullet points)
- Avoid verbose explanations of obvious code
- When reading files, read only what is needed for the current phase
- The goal: a linear history of high-signal tokens from grill to ship

## Skipping Phases

The user may say "skip to build" or "I already know what I want." In that case:
- Ask for the design summary (replaces Grill output)
- Ask which phases to include and which checkpoints to set
- Proceed from the requested phase

## Additional Resources

### Reference Files

Consult these files at activation and as needed during phases:

- **`references/engineering-knowledge.md`** — Industry best practices distilled from foundational books. Categories: clean code, architecture, systems, observability, testing, pragmatic craft.
- **`references/ai-native-patterns.md`** — Patterns for writing code that is AI-readable, AI-editable, and future-proof.
- **`references/process-learnings.md`** — Self-learning file. Codebase-specific patterns and process debt discovered in prior cycles. Grows over time.
- **`references/dev-log.md`** — Structured metrics log. One entry per completed dev cycle. Used for pattern detection and retrospective analysis.
