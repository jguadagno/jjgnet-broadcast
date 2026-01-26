---
name: dotnet-feature
description: Expert .NET 10 Full-Stack Developer skill. Use this when implementing new features, vertical slices, or modifying existing logic in the JJGNET Broadcasting application. It covers Domain, Data (EF Core), Managers, Api, and Web.
---

# .NET Feature Implementation Skill

## Overview

This skill guides the implementation of features following the Clean Architecture pattern used in JJGNET Broadcasting. It ensures consistency across the Domain, Data, Managers, API, and Web layers.

## Architecture Flow

1. **API**: Implement Controllers and Services. (Depends on Managers)
2. **Domain**: Define Interfaces, Models, and Enums. (No dependencies)
3.  **Data**: Implement DataStores using EF Core. (Depends on Domain)
4.  **Managers**: Implement Business Logic. (Depends on Data + Domain)
5.  **Web**: Implement UI with Razor Pages and HTMX. (Depends on Managers)

## Coding Standards

### General C# Guidelines
-   **Namespaces**: ALWAYS use File-Scoped Namespaces (e.g., `namespace JosephGuadagno.Broadcasting.Web;`).
-   **Injection**: ALWAYS inject Interfaces (`IUserManager`), never concrete types.
-   **Async**: ALWAYS use `async/await` throughout the stack.

### 1. Domain Layer (`src/JosephGuadagno.Broadcasting.Domain`)
-   Models should be POCOs with DataAnnotations for validation.
-   Interfaces (`IDataStore`, `IManager`) should return `Task<T>`.

### 2. Data Layer (`src/JosephGuadagno.Broadcasting.Data.Sql`)
-   **Pattern**: Repository/DataStore pattern.
-   **Context**: Use `BroadcastingContext`.
-   **Mapping**: Use AutoMapper to map between Entity and Domain models if they differ.
-   **NO Migrations**: Do not run EF migrations. Schema is handled by `sql-schema` skill.

```csharp
public class EngagementDataStore : IEngagementDataStore
{
    private readonly BroadcastingContext _context;
    // ... constructor ...

    public async Task<Engagement?> GetAsync(Guid id)
    {
        // Use AsNoTracking for read-only operations
        var entity = await _context.Engagements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return _mapper.Map<Engagement>(entity);
    }
}
```

### 3. Manager Layer (`src/JosephGuadagno.Broadcasting.Managers`)
-   Contains all business logic.
-   orchestrates calls between DataStores and external services (Email, etc.).

### 4. Web Layer (`src/JosephGuadagno.Broadcasting.Web`)
-   **Framework**: ASP.NET Core MVC.

## Checklist for New Features

1.  [ ] Defined Model in `Domain`.
2.  [ ] Created `IDataStore` interface in `Domain`.
3.  [ ] Implemented `DataStore` in `Data`.
4.  [ ] Registered `DataStore` in `Program.cs` (Scoped).
5.  [ ] Created `IManager` interface in `Domain`.
6.  [ ] Implemented `Manager` in `Managers`.
7.  [ ] Registered `Manager` in `Program.cs` (Scoped).
8.  [ ] Created Razor Page + PageModel.
