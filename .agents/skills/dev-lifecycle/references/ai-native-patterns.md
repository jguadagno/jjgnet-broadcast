# AI-Native Code Patterns

Patterns for writing code that is easy for LLMs (and humans) to understand, modify, and maintain.
These principles optimize for the reality that AI agents are now co-authors and maintainers of
your codebase alongside human engineers.

---

## 1. Explicit Over Implicit

**Pattern:** Make every decision visible in the code. No magic, no convention-only behavior.

- Explicit imports over barrel files or re-exports
- Explicit types/interfaces over inferred types (especially at module boundaries)
- Explicit error handling over silent swallowing
- Explicit configuration over environment-dependent behavior
- Named parameters or options objects over positional arguments with booleans

**Why:** An LLM reading your code has no memory of your team's conventions. If a behavior
depends on an unwritten convention, the AI will miss it and introduce bugs. Explicit code
is also self-documenting for new human engineers.

**Anti-pattern:** `export * from './utils'` — which utils? What's the public API?
**Better:** `export { parseDate, formatCurrency } from './utils'`

---

## 2. Small, Single-Purpose Files

**Pattern:** One concept per file. If a file has two unrelated responsibilities, split it.

- Target 50-200 lines per file (not a hard rule, but a smell threshold)
- File name should describe its single responsibility
- Components, stores, utilities, types — each in their own file
- Avoid "god files" that accumulate unrelated functions over time

**Why:** LLMs process files as context units. A 500-line file with 8 concerns means the AI
loads 500 lines to understand 1 concern. Small files enable precise context loading —
read only what you need for the current task.

**Anti-pattern:** `helpers.js` with 40 unrelated functions
**Better:** `date-utils.js`, `format-currency.js`, `validation.js`

---

## 3. Self-Documenting Names

**Pattern:** Names should encode intent, scope, and type without needing a comment.

- Functions: verb + noun (`parseUserInput`, `calculateTotalPrice`, `emitSessionEvent`)
- Booleans: `is`/`has`/`should` prefix (`isActive`, `hasPermission`, `shouldRetry`)
- Collections: plural (`users`, `activeConnections`, `pendingTasks`)
- Handlers: `handle` + event (`handleClick`, `handleSessionTimeout`)
- Transformers: `to` + target (`toJSON`, `toDisplayName`, `toSnakeCase`)
- Constants: SCREAMING_SNAKE for true constants, camelCase for config

**Why:** LLMs use names as the primary signal for understanding code intent. A well-named
function rarely needs a comment. A poorly named one will be misunderstood regardless of comments.

**Anti-pattern:** `const d = getData(); process(d);`
**Better:** `const activeUsers = fetchActiveUsers(); renderUserList(activeUsers);`

---

## 4. Clear Module Boundaries

**Pattern:** Every module has a clear public API. Internal details are not accessible.

- Use index files as the public API surface (but with explicit named exports, not `export *`)
- Mark internal functions as private or prefix with underscore by convention
- Types that cross module boundaries get their own file
- Avoid circular dependencies — they signal confused boundaries

**Why:** When an AI agent needs to modify module A, it should know exactly what module B
exposes without reading B's internals. Clear boundaries mean the AI can work on one module
without needing the entire codebase in context.

---

## 5. Flat Over Nested

**Pattern:** Prefer flat structures over deeply nested ones.

- Flat directory structures (2-3 levels max for most projects)
- Early returns over nested if/else chains
- Flat data structures over deeply nested objects
- Flat component hierarchies over 5-level wrapper chains

**Why:** Deep nesting increases cognitive load for both humans and LLMs. A function with
4 levels of nesting is harder to reason about than 4 sequential guard clauses with early returns.
Flat structures also produce cleaner diffs.

**Anti-pattern:**
```
if (user) {
  if (user.isActive) {
    if (user.hasPermission('admin')) {
      // finally, the actual logic
    }
  }
}
```

**Better:**
```
if (!user) return;
if (!user.isActive) return;
if (!user.hasPermission('admin')) return;
// the actual logic, at top level
```

---

## 6. Composable Over Inherited

**Pattern:** Build with composition (functions, mixins, hooks) not inheritance hierarchies.

- Functions that take data and return data (pure where possible)
- Composable hooks/utilities over base classes
- Props + events over internal state where possible
- Dependency injection over hard-coded imports for external services

**Why:** LLMs understand function composition naturally — input goes in, output comes out.
Inheritance requires understanding the full class hierarchy to know what a method actually does.
Composition keeps the blast radius of changes small and predictable.

---

## 7. Colocation

**Pattern:** Keep related things together. Tests next to source. Types next to usage.

- Component + test + styles + types in the same directory
- API route + handler + validation in the same module
- Types at the top of the file that uses them, or in a sibling `types.ts`

**Why:** When an AI (or human) works on a feature, everything they need is in one place.
No hunting across `src/types/`, `src/utils/`, `src/tests/` to find related pieces.

---

## 8. Predictable Patterns

**Pattern:** Do the same thing the same way everywhere. Consistency over cleverness.

- One way to fetch data (not sometimes fetch, sometimes axios, sometimes useSWR)
- One way to handle errors (not sometimes try/catch, sometimes .catch, sometimes error boundary)
- One way to define components (not sometimes class, sometimes function, sometimes arrow)
- One state management pattern per layer

**Why:** LLMs learn patterns from the codebase they're reading. If you fetch data 3 different
ways, the AI has to guess which pattern to follow. Consistency means the AI can pattern-match
confidently and produce code that fits.

---

## 9. Semantic Commits & Branch Names

**Pattern:** Git history should tell the story of why changes were made.

- Commit messages: imperative mood, "why" over "what" (`Fix race condition in session cleanup`
  not `Updated terminal.rs`)
- Branch names: `feature/path-navigator`, `fix/session-leak`, `refactor/store-cleanup`
- One logical change per commit — don't mix refactoring with feature work

**Why:** AI agents (and future you) use git history to understand intent. A clean history
means the AI can `git log` to understand why code exists, not just what it does. This is
especially valuable when the AI needs to decide whether to modify or preserve existing behavior.

---

## 10. Error Messages as Documentation

**Pattern:** Error messages should tell the developer exactly what went wrong and what to do.

- Include the actual value that caused the error
- Include what was expected
- Include a suggested fix when possible
- Use structured errors (error codes + messages) at system boundaries

**Why:** When an AI hits an error during Build or Test phases, a good error message
is the difference between a 1-step fix and a 10-minute debugging session. The error message
IS the documentation for failure modes.

**Anti-pattern:** `Error: invalid input`
**Better:** `Error: expected session ID as UUID, got "abc123". Ensure the session was created via pty_spawn before referencing it.`

---

## Summary Principles

1. Write code as if the next reader has zero context about your codebase
2. Explicit beats implicit — always
3. Small files, small functions, flat structures
4. One pattern per concern — consistency enables pattern matching
5. Names carry meaning — invest time in naming
6. Boundaries are contracts — define them clearly
7. Error messages are user interfaces for debugging
8. Git history is documentation — keep it clean
9. Composition over inheritance — always
10. Colocate related code — reduce the search radius
