---
name: "stateful-mocks"
description: "Testing patterns for simulating sequential calls and race conditions using Moq stateful callbacks"
domain: "testing"
confidence: "high"
source: "earned (Issue #708 regression testing)"
---

## Context

When testing scenarios where the same method is called multiple times with different outcomes (e.g., double-submit, race conditions, retry logic), you need to simulate state changes in your mocks. This is particularly useful for:

- **Double-submit bugs:** First call succeeds, second call should fail
- **Race conditions:** Multiple concurrent calls with different results
- **Retry logic:** Failed calls followed by successful retry
- **State transitions:** Operations that change system state between calls

**Project:** JJGNET Broadcasting  
**Framework:** xUnit + Moq 4.20.72  
**Use Case:** Issue #708 - Testing Web controller behavior when double-submit occurs

## Patterns

### Pattern 1: Counter-Based State Simulation

Use a local counter variable to track call count and return different results:

```csharp
[Fact]
public async Task Method_DuplicateCall_HandlesSecondCallFailure()
{
    // Arrange
    var callCount = 0;
    _mockService
        .Setup(s => s.AddSomething(It.IsAny<int>(), It.IsAny<int>()))
        .ReturnsAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new SuccessResult();  // First call succeeds
            }
            throw new ConflictException("Duplicate");  // Second call fails
        });

    // Act
    var firstResult = await _controller.Action();   // Call 1: success
    var secondResult = await _controller.Action();  // Call 2: error

    // Assert
    Assert.IsType<RedirectResult>(firstResult);
    Assert.Contains("success", _controller.TempData["Message"]);
    Assert.IsType<RedirectResult>(secondResult);
    Assert.Contains("Duplicate", _controller.TempData["ErrorMessage"]);
}
```

### Pattern 2: Queue-Based Sequence

For more complex sequences, use a queue of responses:

```csharp
var responses = new Queue<Result>(new[] {
    new Result { Success = true },
    new Result { Success = false, Error = "Conflict" },
    new Result { Success = true }  // Retry succeeds
});

_mockService
    .Setup(s => s.Process())
    .ReturnsAsync(() => responses.Dequeue());
```

### Pattern 3: Callback Capture + State Check

Capture the state passed to the mock to verify it changes between calls:

```csharp
var capturedStates = new List<State>();
_mockService
    .Setup(s => s.Update(It.IsAny<State>()))
    .Callback<State>(state => capturedStates.Add(state))
    .ReturnsAsync(true);

// ... multiple calls ...

Assert.Equal(2, capturedStates.Count);
Assert.Equal("Initial", capturedStates[0].Status);
Assert.Equal("Updated", capturedStates[1].Status);
```

## Examples

### Real Example: Issue #708 AddPlatform Double-Submit Test

File: `src/JosephGuadagno.Broadcasting.Web.Tests/Controllers/EngagementsControllerTests.cs`

```csharp
[Fact]
public async Task AddPlatform_Post_DuplicateAttempt_ShouldHandleHttpRequestException()
{
    // Arrange - Simulates the double-submit scenario from issue #708
    // First call succeeds, second call would fail with 409 Conflict
    var viewModel = new EngagementSocialMediaPlatformViewModel
    {
        EngagementId = 42,
        SocialMediaPlatformId = 1,
        Handle = "@TestHandle"
    };

    var firstCallSucceeds = new EngagementSocialMediaPlatform
    {
        EngagementId = 42,
        SocialMediaPlatformId = 1,
        Handle = "@TestHandle"
    };

    var callCount = 0;
    _engagementService
        .Setup(s => s.AddPlatformToEngagementAsync(42, 1, "@TestHandle"))
        .ReturnsAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                return firstCallSucceeds;
            }
            throw new HttpRequestException("API returned 409 Conflict");
        });

    // Act - First call (succeeds)
    var firstResult = await _controller.AddPlatform(viewModel);

    // Assert first call
    var firstRedirect = Assert.IsType<RedirectToActionResult>(firstResult);
    Assert.Equal("Edit", firstRedirect.ActionName);
    Assert.Equal("Platform added successfully.", _controller.TempData["SuccessMessage"]);

    // Act - Second call (simulates double-submit, should be caught)
    var secondResult = await _controller.AddPlatform(viewModel);

    // Assert second call - Error should be caught and user-friendly message shown
    var secondRedirect = Assert.IsType<RedirectToActionResult>(secondResult);
    Assert.Equal("Edit", secondRedirect.ActionName);
    Assert.Contains("409 Conflict", (string)_controller.TempData["ErrorMessage"]!);
}
```

**Key Points:**
- Counter variable (`callCount`) tracks which call is executing
- First call returns success result
- Second call throws exception (simulating backend 409 Conflict)
- Test verifies controller handles both outcomes correctly
- Provides regression coverage for double-submit bug without requiring JavaScript tests

## Anti-Patterns

### ❌ Using `.SetupSequence()` for Exception Cases

**Don't:**
```csharp
_mock.SetupSequence(s => s.Method())
    .ReturnsAsync(new Result())
    .Throws(new Exception());  // Throws synchronously, not async!
```

**Do:**
```csharp
_mock.Setup(s => s.Method())
    .ReturnsAsync(() => callCount++ == 0 ? new Result() : throw new Exception());
```

**Reason:** `.Throws()` in SetupSequence doesn't work with async methods. Use callback-based logic instead.

### ❌ Mutating Shared State Without Isolation

**Don't:**
```csharp
private int _sharedCallCount = 0;  // Shared across tests!

[Fact]
public void Test1() { /* uses _sharedCallCount */ }

[Fact]
public void Test2() { /* uses _sharedCallCount */ }
```

**Do:**
```csharp
[Fact]
public void Test1() 
{
    var callCount = 0;  // Local to this test
    // ...
}
```

**Reason:** xUnit doesn't guarantee test execution order. Shared state causes flaky tests.

### ❌ Complex State Logic in Mock Setup

**Don't:**
```csharp
_mock.Setup(s => s.Method())
    .ReturnsAsync(() => {
        // 50 lines of complex business logic
        // ...
    });
```

**Do:**
- Keep mock logic simple (counters, flags, queues)
- If you need complex logic, extract it to a helper method or reconsider the test design
- Complex mock logic often indicates the test is too broad

## When to Use

**Use stateful mocks when:**
- ✅ Testing sequential calls to the same method with different outcomes
- ✅ Simulating state changes (e.g., resource created, then updated)
- ✅ Testing error recovery or retry logic
- ✅ Regression testing for race conditions or timing bugs
- ✅ Verifying controller behavior across multiple requests (like double-submit)

**Don't use stateful mocks when:**
- ❌ A single `.Setup()` with fixed return value suffices
- ❌ You're testing business logic that should be in the service layer, not the controller
- ❌ The state logic is so complex it becomes harder to understand than the code under test

## Related Patterns

- **Callback Capture Pattern** (Tank history #137) - Capture objects passed to mocks for assertion
- **Defense-in-Depth Testing** (Tank history #263) - Layer validation at multiple levels (client, web, API, data)
- **Web.Tests TempData Pattern** (Tank history #276) - Initialize TempData in Web controller tests

## References

- **Issue:** #708 - Double-submit bug on AddPlatform form
- **Files:** 
  - `src/JosephGuadagno.Broadcasting.Web.Tests/Controllers/EngagementsControllerTests.cs` (lines 505-560)
- **Decision:** `.squad/decisions/inbox/tank-708-web-tests.md`
- **History:** `.squad/agents/tank/history.md` (2026-04-13 session)
