# Team Focus — Now

**Last updated:** 2026-04-20  
**Sprint:** 21 (active)  
**Status:** Sprint planning complete. Ready to start collector owner work.  
**Next:** Begin implementation of #760, #761, #762 (collector owner OID threading).

## Current Focus

**Sprint 21 — Collector Owner OID Completeness (Epic #609 Round 1)**

Close the remaining Round 1 ownership gaps in the Functions collector flow:

| Issue | Title | Squad |
|---|---|---|
| #760 | Source collector owner OID from collector records | trinity |
| #761 | Remove empty-owner reader scaffolding | trinity |
| #762 | Add regression coverage for collector owner threading | tank |

**Goal:** All collector-triggered persistence paths thread a real owner OID from the source/collector record — no more global scaffold or empty-string fallbacks.

## Future Sprints

The scope-to-role migration (#763–#769) is now scheduled across dedicated sprints:

| Sprint | Phase | Issues |
|---|---|---|
| Sprint 22 | Phase 0 — Foundation | #763, #764 |
| Sprint 23 | Phases 1-2 — Controller + test migration | #765, #766 |
| Sprint 24 | Phases 3-4 — Cleanup + Entra portal | #767, #768, #769 |

## Standing Work

- #689: In-memory caching for SocialMediaPlatformManager (backlog)

## Key Patterns (Sprint 21)

1. **Owner OID threading:** Collectors load owner OID from the collector/source record and pass it through `LoadNewPosts`, `LoadAllPosts`, `LoadNewVideos`, `LoadAllVideos`.
2. **No empty-owner persistence:** Readers must require a non-empty `CreatedByEntraOid` on persisted records.
3. **Regression tests:** Tests must fail if implementation falls back to removed scaffolding.

## Team Composition

**Sprint 21:**
- Trinity — Collector implementation (#760, #761)
- Tank — Test coverage (#762)
- Neo — Review & architecture

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-04-20  
**Sprint:** 21 (Collector Owner OID)  
**Next Decision Point:** Sprint 22 kickoff after Sprint 21 closes
