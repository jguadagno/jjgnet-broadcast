---
name: "security-test-checklist"
description: "Step-by-step checklist for writing ownership enforcement (Forbid/403) tests on API controllers and Web MVC controllers that gate on CreatedByEntraOid"
domain: "testing"
confidence: "high"
source: "earned (Sprint 18 — Issues #729, #730, #738, #739 — 20+ security tests across 3 controllers)"
---

## Context

When a controller enforces resource ownership via `CreatedByEntraOid`, every action that reads, modifies, or deletes a resource must return `403 Forbidden` when the caller's Entra OID does not match the entity's stored OID. This pattern is established across `EngagementsController`, `SchedulesController`, `TalksController`, and `PlatformsController`.

Use this checklist every time you:
- Add a new controller that calls `Forbid()`
- Add ownership enforcement to an existing controller action
- Review a PR that touches any controller with `CreatedByEntraOid` checks

**Project:** JJGNET Broadcasting  
**Framework:** xUnit + Moq + FluentAssertions  
**Auth model:** `User.FindFirstValue(ApplicationClaimTypes.EntraObjectId)` vs `entity.CreatedByEntraOid`

---

## Step 1 — Pre-work: Grep the Forbid() Sites

Before writing a single test, know exactly how many `Forbid()` call sites exist in the controller:

```powershell
Select-String -Path ".\src\**\*Controller.cs" -Pattern "Forbid\(\)" -Recurse
```

Or for a specific controller:

```powershell
Select-String -Path ".\src\JosephGuadagno.Broadcasting.Api\Controllers\SchedulesController.cs" -Pattern "Forbid\(\)"
```

**Rule:** One non-owner 403 test is required per `Forbid()` call site. Do not guess — count from grep output.

---

## Step 2 — Build the Coverage Matrix

Create a table before writing any tests. One row per `Forbid()` site:

| Action Method | HTTP Verb | Forbid() Site | Non-Owner Test | Admin Bypass Test |
|---|---|---|---|---|
| `GetScheduledItemAsync` | GET | Line 47 | ✅ `WhenNonOwner_ReturnsForbid` | N/A (read-only) |
| `UpdateScheduledItemAsync` | PUT | Line 89 | ✅ `WhenNonOwner_ReturnsForbid` | N/A |
| `DeleteScheduledItemAsync` | DELETE | Line 112 | ✅ `WhenNonOwner_ReturnsForbid` | N/A |

Mark each cell only after the test is written and green. Do not close the PR with empty cells.

---

## Step 3 — OID Mismatch Pattern (API — ForbidResult)

The canonical non-owner test pattern for **API controllers**:

```csharp
[Fact]
public async Task UpdateScheduledItemAsync_WhenNonOwner_ReturnsForbid()
{
    // Arrange
    // Entity is owned by "owner-oid-12345".
    // The calling user has a DIFFERENT OID — ownership check must reject it.
    var item = BuildScheduledItem(5, oid: "owner-oid-12345");
    var request = BuildScheduledItemRequest(5);
    _scheduledItemManagerMock.Setup(m => m.GetAsync(5)).ReturnsAsync(item);

    // SUT is initialized with a non-owner OID — this is the mismatch.
    var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "non-owner-oid-99999");

    // Act
    var result = await sut.UpdateScheduledItemAsync(5, request);

    // Assert
    result.Result.Should().BeOfType<ForbidResult>();
    // Side-effect must never fire — authorization short-circuits before SaveAsync.
    _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Never);
}
```

**Key invariants:**
- Entity OID (`"owner-oid-12345"`) ≠ caller OID (`"non-owner-oid-99999"`) — they must be different strings
- Use domain-constant scope: `Domain.Scopes.Schedules.All` — no magic strings
- `Times.Never` on every side-effect mock that must not fire during a 403

---

## Step 4 — Required Test Assertions

Every non-owner 403 test **must** include both of these assertions:

### 4a. Result type assertion
```csharp
result.Result.Should().BeOfType<ForbidResult>();
```

### 4b. Side-effect Times.Never assertion
```csharp
// Example: delete action must not call DeleteAsync if ownership check fails
_managerMock.Verify(m => m.DeleteAsync(It.IsAny<int>()), Times.Never);
```

If the controller calls `GetAsync` first (pre-flight), verify that **only** `GetAsync` fires:
```csharp
_managerMock.Verify(m => m.GetAsync(5), Times.Once);        // pre-flight: must fire
_managerMock.Verify(m => m.DeleteAsync(It.IsAny<int>()), Times.Never); // side-effect: must NOT fire
```

---

## Step 5 — Admin Bypass Verification

When the controller has an `IsSiteAdministrator()` branch that skips the ownership check, add a test that proves the admin path calls the **unfiltered** overload:

```csharp
[Fact]
public async Task GetScheduledItemsAsync_WhenSiteAdmin_CallsUnfilteredGetAll()
{
    // Arrange
    var items = new List<ScheduledItem> { BuildScheduledItem(1) };
    // Set up the unfiltered overload (no ownerOid param).
    _scheduledItemManagerMock
        .Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
        .ReturnsAsync(new PagedResult<ScheduledItem> { Items = items, TotalCount = items.Count });

    // SiteAdmin = true bypasses ownership filter.
    var sut = CreateSut(Domain.Scopes.Schedules.All, isSiteAdmin: true);

    // Act
    var result = await sut.GetScheduledItemsAsync();

    // Assert
    // Unfiltered overload must be invoked exactly once …
    _scheduledItemManagerMock.Verify(
        m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()),
        Times.Once);
    // … and the owner-filtered overload must NEVER be called.
    _scheduledItemManagerMock.Verify(
        m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
        Times.Never);
}
```

---

## Step 6 — Pre-PR Gate

Before opening any PR that adds or modifies controller security logic:

```powershell
dotnet test .\src\ `
  --no-build `
  --verbosity normal `
  --configuration Release `
  --filter "FullyQualifiedName!~SyndicationFeedReader"
```

**Hard rule:** Zero failures. No exceptions. `SyndicationFeedReader` tests may be excluded (network-dependent), but all other tests must be green.

If the build is required first:
```powershell
dotnet build .\src\ --configuration Release
```

---

## Step 7 — Web MVC Variant (Redirect + TempData)

Web MVC controllers do **not** return `ForbidResult`. They redirect with an error message in `TempData`. The non-owner pattern for Web MVC:

```csharp
[Fact]
public async Task Edit_Post_WhenNonOwner_RedirectsWithError()
{
    // Arrange
    var item = BuildScheduledItem(5, oid: "owner-oid-12345");
    _scheduledItemServiceMock.Setup(s => s.GetScheduledItemAsync(5)).ReturnsAsync(item);

    // Caller has a different OID — ownership check must reject it.
    _controller.ControllerContext = CreateControllerContext(ownerOid: "non-owner-oid-99999");

    // Act
    var result = await _controller.Edit(viewModel);

    // Assert — Web MVC redirects rather than returning ForbidResult
    var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
    redirect.ActionName.Should().Be("Index");                         // or whichever action
    _controller.TempData["ErrorMessage"].Should().NotBeNull();
    // Side-effect must not fire
    _scheduledItemServiceMock.Verify(
        s => s.UpdateScheduledItemAsync(It.IsAny<ScheduledItem>()),
        Times.Never);
}
```

**Key difference from API pattern:**
| API Controller | Web MVC Controller |
|---|---|
| `result.Result.Should().BeOfType<ForbidResult>()` | `result.Should().BeOfType<RedirectToActionResult>()` |
| No TempData check | `TempData["ErrorMessage"].Should().NotBeNull()` |

---

## Examples

### Reference Files

| Controller | Test File |
|---|---|
| `EngagementsController` | `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\EngagementsControllerTests.cs` |
| `SchedulesController` (API) | `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\SchedulesControllerTests.cs` |
| `TalksController` | `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\EngagementsController_TalksTests.cs` |
| `PlatformsController` | `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\EngagementsController_PlatformsTests.cs` |

### Helper Structure (Required in Every API Test Class)

```csharp
private SchedulesController CreateSut(string scopeClaimValue, string ownerOid = "test-oid-12345", bool isSiteAdmin = false)
{
    var controller = new SchedulesController(_scheduledItemManagerMock.Object, _loggerMock.Object, _mapper)
    {
        ControllerContext = CreateControllerContext(scopeClaimValue, ownerOid, isSiteAdmin),
        ProblemDetailsFactory = new TestProblemDetailsFactory()
    };
    return controller;
}

private static ControllerContext CreateControllerContext(string scopeClaimValue, string ownerOid = "test-oid-12345", bool isSiteAdmin = false)
{
    var claims = new List<Claim>
    {
        new Claim("scp", scopeClaimValue),
        new Claim("http://schemas.microsoft.com/identity/claims/scope", scopeClaimValue),
        new Claim(Domain.Constants.ApplicationClaimTypes.EntraObjectId, ownerOid)
    };
    if (isSiteAdmin)
        claims.Add(new Claim(ClaimTypes.Role, Domain.Constants.RoleNames.SiteAdministrator));

    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
    var httpContext = new DefaultHttpContext { User = user };
    return new ControllerContext { HttpContext = httpContext };
}

private static ScheduledItem BuildScheduledItem(int id = 1, string oid = "test-oid-12345") => new()
{
    Id = id,
    // ... fields ...
    CreatedByEntraOid = oid   // ← must always be set; defaults to "test-oid-12345"
};
```

---

## Anti-Patterns

### ❌ Forgetting to set entity OID

```csharp
// WRONG — entity OID is null; ownership check behaviour is undefined
var item = new ScheduledItem { Id = 5, Message = "Test" };
```

```csharp
// RIGHT — entity OID explicitly set to a different value than the caller's OID
var item = BuildScheduledItem(5, oid: "owner-oid-12345");
var sut = CreateSut(scope, ownerOid: "non-owner-oid-99999");
```

### ❌ Using the same OID for entity and caller in a non-owner test

```csharp
// WRONG — this is an *owner* test; controller will pass, not Forbid
var item = BuildScheduledItem(5, oid: "test-oid-12345");
var sut = CreateSut(scope, ownerOid: "test-oid-12345"); // same!
```

### ❌ Missing Times.Never on side-effects

```csharp
// WRONG — does not verify that the side-effect was suppressed
result.Result.Should().BeOfType<ForbidResult>();
// (missing _managerMock.Verify(..., Times.Never))
```

### ❌ Pushing with failing tests

Opening a PR with failing tests is not allowed. Always run `dotnet test` first. If tests fail, fix them before opening the PR.

### ❌ Mock overload mismatch after controller signature change

When a controller method signature adds an `ownerOid` parameter, the Moq `.Setup()` overload must be updated immediately. Mismatched overloads silently miss the setup, causing tests to return null and producing wrong results.

---

## Patterns

### Naming Convention for Non-Owner Tests

```
{ActionMethod}_WhenNonOwner_ReturnsForbid         // API
{ActionMethod}_WhenNonOwner_RedirectsWithError    // Web MVC
{ActionMethod}_WhenSiteAdmin_CallsUnfilteredGetAll // Admin bypass
```

### OID Constants to Use

| Constant | Value | Role |
|---|---|---|
| Default owner OID | `"test-oid-12345"` | Entity owner + authorized user in happy-path tests |
| Non-owner OID | `"non-owner-oid-99999"` | Unauthorized caller in 403 tests |
| Owner OID (explicit) | `"owner-oid-12345"` | Entity owner when 403 test is explicit |

All OIDs are test-only strings. No magic strings in production code — use `Domain.Constants.ApplicationClaimTypes.EntraObjectId`.
