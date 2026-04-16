# Skill: API DTO Layer Review

**Purpose:** Review API DTO implementations for contract/domain separation, mapping patterns, and REST compliance.

## When to Use

- Reviewing PRs that introduce or modify Request/Response DTOs
- Validating DTO mapping patterns (ToResponse/ToModel helpers)
- Checking API contract decoupling from domain models

## Review Checklist

### 1. **DTO Structure**
- [ ] Request DTOs in `Api/Dtos/*Request.cs`, Response DTOs in `Api/Dtos/*Response.cs`
- [ ] Request DTOs use `[Required]`, `[Url]`, and other validation attributes
- [ ] Response DTOs include all fields clients need (IDs, timestamps, related entities)
- [ ] Request DTOs **do NOT** include route parameters (e.g., no `Id` in `EngagementRequest` for `PUT /engagements/{id}`)

### 2. **Mapping Pattern**
- [ ] Each controller has private static `ToResponse(DomainModel)` helper
- [ ] Each controller has private static `ToModel(RequestDTO, routeParams...)` helper
- [ ] No AutoMapper or external mapping library used (unless project-wide decision changes)
- [ ] Route parameters passed to `ToModel` as arguments, not extracted from DTO

### 3. **Controller Integration**
- [ ] All action methods accept Request DTOs (not domain models)
- [ ] All action methods return Response DTOs (not domain models)
- [ ] `[ProducesResponseType]` attributes updated to DTO types
- [ ] CreatedAtAction returns `ToResponse(savedEntity)`, not raw entity

### 4. **Validation & Error Handling**
- [ ] No "route id must match body id" checks (route is ground truth)
- [ ] ModelState validation before mapping: `if (!ModelState.IsValid) return BadRequest(ModelState);`
- [ ] Null checks on manager results before mapping to Response DTO

### 5. **REST Compliance**
- [ ] POST endpoints use Request DTOs (no ID in DTO)
- [ ] PUT endpoints use Request DTOs with route ID: `ToModel(request, idFromRoute)`
- [ ] GET endpoints return Response DTOs
- [ ] Collection endpoints return `List<ResponseDTO>` with `.Select(ToResponse).ToList()`

### 6. **Edge Cases**
- [ ] Optional/nullable properties handled correctly (`e.Talks?.Select(ToResponse).ToList()`)
- [ ] Computed fields (e.g., `ItemTableName`) excluded from Request DTOs
- [ ] No BOM (Byte Order Mark) characters in DTO files (check with hex editor if suspicious)

## Common Issues

### ❌ Anti-Pattern: Route Parameter in Request DTO
```csharp
// WRONG: TalkRequest includes EngagementId but route provides it
public class TalkRequest
{
    [Required] public int EngagementId { get; set; }  // ← Route provides this!
}

[HttpPost("{engagementId:int}/talks")]
public async Task<ActionResult> CreateTalkAsync(int engagementId, TalkRequest request)
```

**Fix:** Remove `EngagementId` from `TalkRequest`. Use route parameter in mapping:
```csharp
var talk = ToModel(request, engagementId);  // engagementId from route
```

### ❌ Anti-Pattern: Returning Domain Models
```csharp
// WRONG: Returns domain model directly
[ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Engagement))]
public async Task<ActionResult<Engagement>> GetEngagementAsync(int id)
{
    return await _manager.GetAsync(id);
}
```

**Fix:** Map to Response DTO:
```csharp
[ProducesResponseType(StatusCodes.Status200OK, Type=typeof(EngagementResponse))]
public async Task<ActionResult<EngagementResponse>> GetEngagementAsync(int id)
{
    var engagement = await _manager.GetAsync(id);
    return ToResponse(engagement);
}
```

## Testing Verification

After DTO changes:
1. Run existing API tests: `dotnet test --filter FullyQualifiedName~Api.Tests --no-build`
2. Check test count matches PR description (e.g., "all 45 API tests pass")
3. Verify no new test failures introduced

## Related Decisions

- `.squad/decisions/inbox/trinity-pr512-dtos.md` — DTO mapping pattern (private static helpers)
- `.squad/decisions/inbox/neo-pr512-review-dto-route-params.md` — Route parameters must not be in Request DTOs
