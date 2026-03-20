### 2026-03-20: Pagination parameter validation pattern
**By:** Morpheus
**What:** Paginated endpoints clamp page to min 1, pageSize to range 1-100. Applied as inline guards at the top of each list action method.
**Why:** Neo review blocked on division-by-zero (pageSize=0) and negative Skip (page=0).
