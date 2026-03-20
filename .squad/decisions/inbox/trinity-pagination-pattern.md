# Decision: API Pagination Pattern

**Author:** Trinity  
**Date:** 2026-03-20  
**Context:** Issue #316 - Add pagination to all list API endpoints

## Decision

All list endpoints in API controllers use **query parameter-based pagination** with `PagedResponse<T>` wrapper.

## Pattern

```csharp
// Add using statement
using JosephGuadagno.Broadcasting.Api.Models;

// Endpoint signature
public async Task<ActionResult<PagedResponse<TResponse>>> GetItemsAsync(
    int page = 1, 
    int pageSize = 25)
{
    var allItems = await _manager.GetAllAsync();
    var totalCount = allItems.Count;
    var items = allItems
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(ToResponse)
        .ToList();
    
    return new PagedResponse<TResponse>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

## PagedResponse Model

Located at `src/JosephGuadagno.Broadcasting.Api/Models/PagedResponse.cs`:

```csharp
public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

## Defaults

- **page**: 1 (first page)
- **pageSize**: 25 (items per page)

## Rationale

1. **Query parameters** - RESTful convention for pagination, allows optional parameters
2. **Default values** - Backward compatible - omitting params gives sensible defaults
3. **Client-side pagination** - Current managers return full collections; pagination happens in controller (acceptable for current data volumes)
4. **Consistent wrapper** - `PagedResponse<T>` provides uniform structure across all list endpoints
5. **TotalPages calculation** - Derived property eliminates need for clients to calculate page count

## Endpoints Updated (Issue #316)

### EngagementsController
- `GET /engagements?page={page}&pageSize={pageSize}`
- `GET /engagements/{id}/talks?page={page}&pageSize={pageSize}`

### SchedulesController
- `GET /schedules?page={page}&pageSize={pageSize}`
- `GET /schedules/unsent?page={page}&pageSize={pageSize}`
- `GET /schedules/upcoming?page={page}&pageSize={pageSize}`
- `GET /schedules/calendar/{year}/{month}?page={page}&pageSize={pageSize}`
- `GET /schedules/orphaned?page={page}&pageSize={pageSize}`

### MessageTemplatesController
- `GET /messagetemplates?page={page}&pageSize={pageSize}`

## Special Cases: 404 Endpoints

Endpoints that return `404 NotFound` when no items exist (e.g., unsent, orphaned) check count **before** pagination to maintain existing behavior:

```csharp
var allItems = await _manager.GetUnsentScheduledItemsAsync();
if (allItems.Count == 0)
{
    return NotFound();
}
// ... then paginate
```

## Future Considerations

- If data volumes grow significantly, consider adding server-side pagination to managers/data stores
- Could add sorting parameters (e.g., `?sortBy=createdOn&sortDirection=desc`)
- Could add filtering parameters (e.g., `?status=unsent`)

## References

- PR #514: https://github.com/jguadagno/jjgnet-broadcast/pull/514
- Issue #316: https://github.com/jguadagno/jjgnet-broadcast/issues/316
