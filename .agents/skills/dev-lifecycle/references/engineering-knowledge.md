# Engineering Knowledge

Reference card for LLM context activation. Distilled from foundational software engineering
books. Each category: source, why it matters, key concepts to dump into working memory,
and core principles.

---

## 1. Clean Code & Naming

**Clean Code** — Robert C. Martin
Use for: naming conventions, function design, formatting, the smell catalog
Dump: meaningful names over comments; functions do one thing; no side effects in queries;
command-query separation; small functions (< 20 lines ideal); extract till you drop;
avoid negative conditionals; no flag arguments; error handling is one thing;
don't return null — throw or use Optional; newspaper metaphor (high-level first, details below);
the boy scout rule (leave code cleaner than you found it)

**Core principles:**
- Code is read 10x more than written — optimize for the reader
- A name should tell you why it exists, what it does, and how it's used
- If you need a comment, the code isn't clear enough — rename or extract
- Functions: one level of abstraction per function, no mixing
- Error handling: use exceptions for exceptional cases, not control flow

---

## 2. Architecture & Boundaries

**Clean Architecture** — Robert C. Martin
Use for: dependency rules, layer separation, use-case driven design
Dump: dependency rule (dependencies point inward); entities (business rules) at center;
use cases wrap entities; interface adapters convert between use cases and external world;
frameworks are details, not architecture; the database is a detail;
screaming architecture (folder structure tells you what the app does, not what framework it uses);
humble objects at boundaries; plugin architecture (UI, DB, web are plugins to business rules)

**A Philosophy of Software Design** — John Ousterhout
Use for: complexity management, deep vs shallow modules, strategic programming
Dump: complexity = dependencies + obscurity; deep modules (simple interface, rich functionality);
shallow modules are a red flag; information hiding vs information leakage;
define errors out of existence; design it twice; strategic vs tactical programming;
comments describe things that aren't obvious from the code (why, not what);
general-purpose classes are better than special-purpose even for single use

**Core principles:**
- Business logic must not depend on frameworks, UI, or database
- Boundaries between modules are the most important design decision
- Make modules deep: simple interfaces that hide complex implementations
- When two modules share knowledge, one should own it
- Design for change by isolating what changes from what doesn't

---

## 3. Distributed Systems & Data

**Designing Data-Intensive Applications** — Martin Kleppmann
Use for: data models, replication, partitioning, consistency, batch/stream processing
Dump: relational vs document vs graph models; write-ahead log; B-trees vs LSM trees;
single-leader vs multi-leader vs leaderless replication; consistency models (linearizability,
causal, eventual); partitioning strategies (key range, hash); distributed transactions
(2PC, saga pattern); exactly-once semantics; event sourcing; CQRS;
stream processing (event time vs processing time); backpressure;
the unreliable network assumption; Byzantine faults vs crash faults

**Core principles:**
- There is no "one size fits all" — choose data model for access patterns
- Replication and partitioning are independent concerns
- Exactly-once delivery is impossible — design for idempotency instead
- Distributed consensus is expensive — avoid it when you can
- Event logs are more fundamental than tables — you can derive tables from logs

---

## 4. Observability & Debugging in Production

**Observability Engineering** — Charity Majors, Liz Fong-Jones, George Miranda
Use for: structured logging, tracing, metrics, debugging unknown-unknowns
Dump: observability vs monitoring (monitoring checks known failure modes;
observability lets you ask new questions); three pillars are a simplification —
the real goal is high-cardinality, high-dimensionality data; structured events over log lines;
trace context propagation (W3C Trace Context); span-based instrumentation;
SLOs over SLAs (internal contract with your users); error budgets;
sampling strategies (head-based vs tail-based); correlation IDs;
the instrumentation-first approach (instrument before you ship, not after it breaks)

**Core principles:**
- If you can't debug it from the logs, you didn't instrument it well enough
- Structured logging: key-value pairs, not printf strings
- Every request should carry a correlation ID end-to-end
- Measure at the boundaries: request in, response out, external call, queue consume
- SLOs define "good enough" — error budgets define when to stop shipping features

---

## 5. Testing Strategy

**Unit Testing: Principles, Practices, and Patterns** — Vladimir Khorikov
Use for: what to test, test boundaries, test doubles, the testing pyramid in practice
Dump: test behavior, not implementation; the classical school vs London school;
output-based testing > state-based > communication-based; the four pillars of a good test
(protection against regressions, resistance to refactoring, fast feedback, maintainability);
don't test trivial code; test at the boundary (public API), not internals;
mocks vs stubs (stubs for queries, mocks for commands); integration test = tests code path
through real dependencies; unit test = tests one behavior in isolation;
the humble object pattern for hard-to-test code; test data builders

**Core principles:**
- Test the behavior, not the implementation — tests should survive refactoring
- The best test catches regressions without breaking on irrelevant changes
- Integration tests over mocks for anything touching infrastructure
- Arrange-Act-Assert, one assertion per behavior, clear test names
- If a test is hard to write, the code design is wrong — fix the design

---

## 6. Pragmatic Craft

**The Pragmatic Programmer** — David Thomas, Andrew Hunt
Use for: DRY, orthogonality, tracer bullets, pragmatic philosophy
Dump: DRY (Don't Repeat Knowledge, not just code — every piece of knowledge should have
a single, authoritative representation); orthogonality (change one thing without affecting
others); tracer bullets (end-to-end skeleton first, then fill in); prototypes vs tracer bullets
(prototypes are disposable, tracer bullets are lean production code); design by contract;
programming by coincidence (understand why code works, don't just accept that it does);
the broken window theory (don't leave bad code unfixed — it breeds more bad code);
reversibility (avoid decisions that can't be undone); good enough software
(know when to stop polishing); rubber ducking; property-based testing

**Refactoring** — Martin Fowler
Use for: when and how to refactor, the refactoring catalog
Dump: refactoring is behavior-preserving transformation; refactor before adding features;
code smells (long method, large class, feature envy, data clumps, primitive obsession);
extract method, extract class, move method, replace conditional with polymorphism;
inline when extraction went too far; refactor in small, tested steps; never refactor
and add features at the same time; the two hats (refactoring hat vs feature hat)

**Core principles:**
- DRY applies to knowledge, not just code — two similar loops aren't necessarily duplication
- Build tracer bullets first: end-to-end, thin, working — then widen
- Refactor in small steps with tests green at every step
- Understand why code works — don't program by coincidence
- Good enough is a pragmatic choice, not laziness — know when to ship

---

## 7. Security Fundamentals

**OWASP Top 10 + Secure Coding Practices**
Use for: security hygiene checklist during Build and Review phases
Dump: injection (SQL, command, XSS) — parameterize everything, never concatenate user input;
broken auth — hash passwords (bcrypt/argon2), use constant-time comparison;
sensitive data exposure — encrypt at rest and in transit, never log secrets;
XXE — disable external entities in XML parsers; broken access control — check auth on every
endpoint, deny by default; security misconfiguration — disable debug in production,
remove default credentials; XSS — escape output by context (HTML, JS, URL, CSS);
insecure deserialization — validate types before deserializing; using components with known
vulnerabilities — keep dependencies updated; insufficient logging — log auth events,
access control failures, input validation failures

**Core principles:**
- Never trust user input — validate at the system boundary
- Defense in depth: multiple layers, never rely on a single check
- Principle of least privilege: grant minimum necessary access
- Fail secure: on error, deny access rather than grant it
- Secrets never go in code, logs, or error messages

---

## 8. Performance & Scalability

**Systems Performance** — Brendan Gregg (concepts, not full dump)
Use for: profiling mental model, avoiding premature optimization
Dump: USE method (Utilization, Saturation, Errors) for resource analysis;
the latency hierarchy (L1 cache 1ns → RAM 100ns → SSD 100us → network 1ms → disk 10ms);
Amdahl's law (speedup limited by serial fraction); flame graphs for CPU profiling;
off-CPU analysis for I/O-bound work; the importance of measuring before optimizing;
premature optimization is evil, but so is premature pessimization;
the 90/10 rule (90% of time in 10% of code — find the 10%)

**Core principles:**
- Measure first, optimize second — intuition about performance is usually wrong
- Optimize the bottleneck, not the fast path
- Caching is the most common performance solution and the most common source of bugs
- N+1 queries are the #1 performance killer in application code
- Premature optimization obscures intent — optimize only after profiling confirms the need
