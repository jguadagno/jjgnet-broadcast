# Squad Ceremonies

Authoritative ceremony configuration for this squad. Use this file to decide
which ceremony to run, when to run it, who facilitates it, and what outputs are
required before work continues.

## Operating Rules

1. Run manual ceremonies only when the user asks.
2. Run auto-triggered ceremonies when their trigger condition matches.
3. After an auto-triggered ceremony completes, apply a one-step cooldown before
   checking auto-triggered ceremonies again.
4. Scribe records every ceremony outcome in `.squad/orchestration-log/` or
   `.squad/retro/` as appropriate.
5. A ceremony result is not complete until it names the owner, next action, and
   any blocker.

## Ceremony Catalog

- **Directive Preflight**
  - **Trigger:** Before multi-agent work, PR recovery, push, review, or any
    GitHub-visible action
  - **When:** before
  - **Facilitator:** Neo
  - **Recorder:** Scribe
  - **Purpose:** Prevent directive drift, duplicate work, and wrong artifact
    types before expensive work starts
- **PR Readiness Huddle**
  - **Trigger:** Before answering "is this PR ready?", before merge advice, or
    before asking Neo for PR feedback
  - **When:** before
  - **Facilitator:** Neo
  - **Recorder:** Scribe
  - **Purpose:** Confirm branch-of-record, validation state, and required
    GitHub-visible output
- **Sprint Planning**
  - **Trigger:** User asks for planning, sprint breakdown, or issue allocation
  - **When:** manual
  - **Facilitator:** Neo
  - **Recorder:** Scribe
  - **Purpose:** Decide scope, sequence, owners, and success criteria
- **Sprint Review and Wrap-Up**
  - **Trigger:** User says sprint is done, asks to wrap up, or requests branch
    cleanup
  - **When:** after
  - **Facilitator:** Neo
  - **Recorder:** Scribe
  - **Purpose:** Confirm merged work, cleanup actions, and any carryover
- **Sprint Retrospective**
  - **Trigger:** User asks for a retro or retrospective after sprint work is
    complete
  - **When:** manual
  - **Facilitator:** Neo
  - **Recorder:** Scribe
  - **Purpose:** Capture what went wrong, root causes, hard rules, and action
    items

---

## Auto-Triggered Ceremonies

### Directive Preflight

- **when:** before
- **facilitator:** Neo
- **participants:** Coordinator, intended work owner, Link for branch/GitHub
  work, Tank for test-heavy work
- **trigger when any are true:**
  - Spawning 2 or more agents
  - User asks for push, PR recovery, merge readiness, review, or GitHub comment/review
  - Work touches both local git state and GitHub state
  - A prior attempt failed, was superseded, or required a redo

#### Directive Preflight Checklist

1. Name the **branch of record**.
2. Name the required **external artifact**:
   - direct answer only
   - local-only result
   - GitHub PR comment
   - GitHub formal review
   - commit
   - push
3. State the **validation gate** before work starts.
4. Check whether the work is already in progress or recently completed in
   `.squad/orchestration-log/`.
5. Run cheap checks before expensive work:
   - current branch / branch freshness
   - PR state
   - whether needed files/tests already exist
   - whether the question can be answered directly without an agent
6. Create or refresh a single **live PR state record** when the work touches a
   PR: branch of record, blocker state, required artifact, and next owner.

#### Directive Preflight Hard Rules

- Decide **comment vs review** before Neo is spawned for PR feedback.
- Decide **GitHub-visible vs local-only** before claiming work is complete.
- Do not spawn duplicate work for the same PR/issue until the latest
  orchestration result is checked.
- If the answer can be produced with a small number of local reads or a single
  GitHub query, do that instead of spawning an agent.
- Use exact terms: **comment**, **formal review**, **local-only**, and
  **ready to merge** are not interchangeable.

#### Directive Preflight Output

- Branch of record
- Output type required
- Validation required
- Assigned owner
- Go / no-go decision

### PR Readiness Huddle

- **when:** before
- **facilitator:** Neo
- **participants:** Coordinator, Neo, Link if branch/GitHub state is involved
- **trigger when any are true:**
  - User asks whether a PR is ready to merge
  - User asks for Neo feedback on a PR
  - User asks what still needs to be pushed or approved

#### PR Readiness Checklist

1. Confirm the PR number and branch of record.
2. Confirm whether the user wants:
   - readiness assessment only
   - a GitHub-visible comment
   - a formal review
3. Confirm the current check/run state.
4. Confirm whether all requested fixes are already pushed.
5. Confirm whether any prior local-only review or approval needs a visible
   GitHub artifact.
6. Refresh the live PR state record before answering.

#### PR Readiness Hard Rules

- Never use the word **approved** unless the approval exists in the place the
  user expects.
- Never say a PR is ready based only on local squad state when the user asked
  for GitHub-visible confirmation.

#### PR Readiness Output

- Ready / not ready
- Missing items, if any
- Exact next owner
- Exact GitHub artifact needed, if any

---

## Manual Ceremonies

### Sprint Planning

- **when:** manual
- **facilitator:** Neo
- **participants:** Neo, relevant implementers, Scribe
- **run when user says:** "plan", "sprint planning", "break down the work",
  "who should do what"

#### Sprint Planning Outputs

- Scope in and out
- Issue order
- Owners
- Risks and dependencies

### Sprint Review and Wrap-Up

- **when:** after
- **facilitator:** Neo
- **participants:** Neo, Link, Scribe
- **run when user says:** "wrap up the sprint", "clean up branches",
  "close out the sprint"

#### Sprint Review Outputs

- PRs merged
- Branches to clean up
- Local state to sync
- Carryover work, if any

### Sprint Retrospective

- **when:** manual
- **facilitator:** Neo
- **participants:** Neo, Link, Scribe, plus any agent whose work materially
  affected the outcome
- **run only after:** sprint work is complete or the user explicitly asks for a retro
- **run when user says:** "retro", "retrospective", "retro time"

#### Agenda

1. What went wrong
2. What went right
3. Root causes
4. Ranked changes to prevent recurrence
5. Hard rules to adopt now
6. Soft habits to reinforce
7. Owners and action items

#### Hard rules for retros

- Focus on repeatable process failures, not one-off mistakes.
- Separate **hard rules** from **soft habits**.
- Include at least one low-cost guardrail that can be adopted immediately.
- If the retro identifies a squad-wide rule, write it to
  `.squad/decisions/inbox/`.

#### Sprint Retrospective Output

- Short summary
- Ranked root causes
- Hard rules to adopt now
- Follow-up actions with owners
- Recorded retro file in `.squad/retro/`

---

## Current Defaults

These defaults apply unless a future decision overrides them.

1. **Neo** facilitates retrospectives, planning, preflight, and PR readiness
   huddles.
2. **Scribe** records ceremony outputs.
3. For PRs under the same GitHub user account, the default visible feedback is
   a **regular PR comment**, not a formal self-review.
4. When cost/control is the concern, prefer cheap local checks before spawning
   agents or polling GitHub repeatedly.
