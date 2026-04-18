---
name: "mock-overload-resolution"
description: "How to update Moq mock setups when a manager method gains new parameters — prevents silent overload mismatch bugs that cause MockException or wrong test results"
domain: "testing"
confidence: "high"
source: "earned (Epic #609 — GetAllAsync signature change from (page, size) to (ownerOid, page, size) caused broken test setups across multiple controller test files)"
---

## Context

When a manager interface method gains new parameters (e.g., `ownerOid` added as part of Epic #609's per-user ownership enforcement), **every existing Moq `.Setup()` call that targets the old signature becomes a dead setup**. Moq will either:

1. **Silently match nothing** — the method is called but no setup matches, so Moq returns the default value (`null` for reference types, `0` for value types, `false` for `bool`). The test passes for the wrong reason, or fails with a `NullReferenceException` that is hard to diagnose.
2. **Throw `MockException`** — when `.Verifiable()` is in play and the expected call never fired, the verify step throws.

This is a **silent failure mode**: the test compiles, may even pass, but is not testing what you think it's testing.

**Project:** JJGNET Broadcasting  
**Framework:** xUnit + Moq 4.20.72 + FluentAssertions  
**Trigger:** Any manager method signature change (new parameter added, parameter type changed, parameter removed)

---

## The Problem

### Scenario: Epic #609 — `GetAllAsync` gains `ownerOid` parameter

Before Epic #609, the manager interface was:

```csharp
// IScheduledItemManager — BEFORE
Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize);
```

After Epic #609, a new overload was added for owner-filtered access:

```csharp
// IScheduledItemManager — AFTER (two overloads)
Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize);                        // admin/unfiltered
Task<PagedResult<ScheduledItem>> GetAllAsync(string ownerOid, int page, int pageSize, ...);  // owner-filtered
```

The controller now calls the owner-filtered overload for non-admin users:

```csharp
// SchedulesController (after Epic #609)
var result = await _scheduledItemManager.GetAllAsync(currentUserOid, page, pageSize);
```

---

## Before (Broken)

The old mock setup targets the **unfiltered** overload (2 parameters). The controller now calls the **owner-filtered** overload (3+ parameters). Moq finds no matching setup.

```csharp
// ❌ BROKEN — targets old 2-parameter overload; controller now calls 3-parameter overload
_scheduledItemManagerMock
    .Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
    .ReturnsAsync(new PagedResult<ScheduledItem> { Items = items, TotalCount = 1 });

var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "test-oid-12345");

var result = await sut.GetScheduledItemsAsync();

// result.Value is null — the controller called the 3-parameter overload, no setup matched.
// The test may fail with NullReferenceException or, worse, produce a false positive.
result.Value.Should().NotBeNull(); // ← throws NullReferenceException
```

**Symptom:** `NullReferenceException`, `MockException`, or (worst case) a test that "passes" because the null result flows through a code path that returns an empty 200 OK.

---

## After (Fixed)

Match the overload that the controller **actually calls**. Use `It.IsAny<T>()` for the new parameter unless the test specifically needs to assert its value.

```csharp
// ✅ FIXED — targets the 3-parameter owner-filtered overload that the controller now calls
_scheduledItemManagerMock
    .Setup(m => m.GetAllAsync(
        It.IsAny<string>(),   // ownerOid — new parameter; use IsAny<> unless test verifies it
        It.IsAny<int>(),      // page
        It.IsAny<int>()))     // pageSize
    .ReturnsAsync(new PagedResult<ScheduledItem> { Items = items, TotalCount = 1 });

var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "test-oid-12345");

var result = await sut.GetScheduledItemsAsync();

// Now resolves correctly — setup matches the actual call.
result.Value.Should().NotBeNull();
result.Value!.Items.Should().HaveCount(1);
```

---

## When to Verify the Specific New Parameter

Use `It.IsAny<string>()` for the new parameter in **most** tests. Use an explicit value only when the test is **specifically verifying** that the controller passes the correct OID:

```csharp
// ✅ Verifying that the controller passes the caller's OID to the manager
_scheduledItemManagerMock
    .Setup(m => m.GetAllAsync(
        "test-oid-12345",     // exact OID — this test verifies the controller uses the caller's OID
        It.IsAny<int>(),
        It.IsAny<int>()))
    .ReturnsAsync(new PagedResult<ScheduledItem> { Items = items, TotalCount = 1 });

// Then verify after Act:
_scheduledItemManagerMock.Verify(
    m => m.GetAllAsync("test-oid-12345", It.IsAny<int>(), It.IsAny<int>()),
    Times.Once);
```

**Default rule:** Use `It.IsAny<T>()` for new parameters unless there is an explicit test objective around that parameter's value.

---

## Step-by-Step Fix Process

When a manager method signature changes:

1. **Find all affected `.Setup()` calls:**
   ```powershell
   Select-String -Path ".\src\**\*Tests.cs" -Pattern "\.Setup\(m => m\.GetAllAsync" -Recurse
   ```

2. **For each match, check the parameter count against the new interface.**
   - If the count is wrong, it's a dead setup — update it.

3. **Add `It.IsAny<T>()` for each new parameter** (unless the test needs to assert the specific value).

4. **Run the specific test class immediately** to verify the fix before moving on:
   ```powershell
   dotnet test .\src\ --filter "FullyQualifiedName~SchedulesControllerTests" --no-build --verbosity normal
   ```

5. **Run the full test suite** before pushing:
   ```powershell
   dotnet test .\src\ --no-build --verbosity normal --filter "FullyQualifiedName!~SyndicationFeedReader"
   ```

---

## Admin Bypass Overload (Special Case)

When a controller has an `IsSiteAdministrator()` branch that calls the **unfiltered** overload, both setups must exist simultaneously in the same test class:

```csharp
// Setup for non-admin tests (owner-filtered overload):
_scheduledItemManagerMock
    .Setup(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
    .ReturnsAsync(pagedResult);

// Setup for admin tests (unfiltered overload):
_scheduledItemManagerMock
    .Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
    .ReturnsAsync(pagedResult);
```

Then in admin-bypass tests, verify that the **unfiltered** overload fires and the **owner-filtered** one does not:

```csharp
_scheduledItemManagerMock.Verify(
    m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()),
    Times.Once);
_scheduledItemManagerMock.Verify(
    m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
    Times.Never);
```

This is the same pattern used in the security-test-checklist SKILL for admin bypass verification — the two skills are complementary.

---

## Anti-Patterns

### ❌ Leaving old 2-parameter setup after controller switches to 3-parameter call

```csharp
// WRONG — setup for old overload; controller calls new overload → null result
_managerMock.Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(result);
```

### ❌ Using a fixed OID in setup for a general happy-path test

```csharp
// WRONG — if the test OID ever changes, this setup silently stops matching
_managerMock.Setup(m => m.GetAllAsync("hardcoded-oid", It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(result);
```

### ❌ Not running the test class after updating a setup

```
// WRONG workflow: update setup → push → find out CI failed
// RIGHT workflow: update setup → dotnet test --filter "FullyQualifiedName~XTests" → green → push
```

### ❌ Assuming the compiler catches overload mismatches

Moq setups are resolved at **runtime**, not compile time. A setup for the wrong overload compiles without warning. **Always run the test** after a setup change.

---

## Related

- **security-test-checklist SKILL.md** — the admin bypass pattern in Step 5 depends on having correct setups for both overloads
- **Anti-pattern in security-test-checklist:** "Mock overload mismatch after controller signature change" (brief mention — this SKILL is the full treatment)
- **Epic #609** — the trigger: per-user content required `GetAllAsync(ownerOid, page, size)` alongside the existing `GetAllAsync(page, size)` on all four manager interfaces

## References

- **Epic:** #609 — Multi-tenancy: per-user content, publishers, and social tokens
- **Interfaces affected:** `IScheduledItemManager`, `IEngagementManager`, `ITalkManager`, `IMessageTemplateManager`
- **Test files affected:** All `*ControllerTests.cs` files in `src/JosephGuadagno.Broadcasting.Api.Tests/Controllers/`
