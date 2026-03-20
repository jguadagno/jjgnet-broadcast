# Neo — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Lead
- **Joined:** 2026-03-14T16:37:57.748Z

## Learnings

<!-- Append learnings below -->

### 2026-03-20: Sprint 7 & 8 Planning

**Milestones created:**
- **Sprint 7** ([#2](https://github.com/jguadagno/jjgnet-broadcast/milestone/2)): Message Templating & Testing Foundations
  - 6 issues: #474, #475, #476, #477, #478, #302
  - Theme: Implement Scriban-based message templates for all 4 platforms (Twitter, Facebook, LinkedIn, Bluesky) + establish JsonFeedReader test project
  - Ready to start immediately (no Sprint 6 blockers)

- **Sprint 8** ([#3](https://github.com/jguadagno/jjgnet-broadcast/milestone/3)): API Improvements, Security, & Infrastructure
  - 7 issues: #315, #316, #317, #303, #336, #328, #335
  - Theme: Production-ready API (DTOs, pagination, REST compliance) + security hardening (HTTP headers for API, cookie security) + observability (App Insights, vulnerable package scanning)
  - Builds on Sprint 6's security foundation

**Planning rationale:**
- Sprint 7 delivers user-facing value (better social media messages) and establishes testing patterns (#302 is first test project for a collector library)
- Sprint 8 prepares infrastructure for external API consumers and completes security hardening started in Sprint 6
- Deferred 30+ issues to future sprints — clustered database work (#322-325), larger test efforts (#300-301), and architectural refactors (#309-312) for dedicated sprints
- Noted potential duplicate issues (#306, #308, #327) for closure verification

**Sprint 6 status:** 1 open PR (#500, HTTP security headers for Web), 9 closed issues. Should be wrapped up before Sprint 7 starts.

### 2026-03-20: Sprint 9 Planning

**Milestone created:**
- **Sprint 9** ([#4](https://github.com/jguadagno/jjgnet-broadcast/milestone/4)): Test Coverage Expansion — Azure Functions & Managers
  - 5 issues: #300, #301, #330, #331, #319
  - Theme: Comprehensive unit tests for collectors, publishers, and manager business logic + removal of external network dependencies in tests + Functions retry/DLQ configuration
  - All 4 testing issues are priority: high; #319 adds Functions reliability (retry policies + dead-letter queues)
  - Builds on Sprint 7's testing foundation (#302) and follows Sprint 8's API/security work

**Planning rationale:**
- Testing Cluster was identified as deferred high-priority work during Sprint 7/8 planning
- Natural progression: Sprint 7 establishes test patterns → Sprint 8 hardens API/security → Sprint 9 expands test coverage
- Cohesive theme: All issues focus on Azure Functions reliability (testing + error handling)
- Addresses flaky tests: #331 removes network dependency from SyndicationFeedReader tests (noted as expected failures in CI)
- Deferred Database Improvements Cluster (#322-325) and Architectural Refactors (#309-312, #314) to future sprints for dedicated focus

**Session note (2026-03-20):** Sprint 9 milestone (#4) created this session as part of Sprint 7 kickoff + Sprint 9 planning orchestration. Neo's sprint planning work (trilogy: S7→S8→S9) documented in orchestration-log/2026-03-20T00-51-00-neo.md.
