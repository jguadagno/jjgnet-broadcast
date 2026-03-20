# Decision: Comprehensive EngagementManager Test Coverage (Issue #330)

**Date:** 2026-03-21  
**Context:** Issue #330 requested real-logic unit tests for EngagementManager  
**Status:** Implemented and submitted as PR #539

## Problem Statement

EngagementManagerTests only verified that repository methods were called, without testing:
1. **Timezone conversion logic** — UpdateDateTimeOffsetWithTimeZone with known inputs/outputs
2. **Deduplication logic** — GetByNameAndUrlAndYearAsync and SaveAsync ID reuse
3. **Practical scenarios** — EDT vs EST, PDT vs PST, CET, UTC edge cases

## Solution Approach

### 1. Timezone Conversion Tests (6 new tests)

Created comprehensive UpdateDateTimeOffsetWithTimeZone tests covering:

**EST Winter (UTC-5):**
```csharp
// Input: 12:00 with UTC-7 offset (simulating mismatch)
// Expected: 12:00 with UTC-5 (re-interpreted in EST zone)
UpdateDateTimeOffsetWithTimeZone("America/New_York", new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-7, 0, 0)))
// Should return: DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-5, 0, 0))
```

**EDT Summer (UTC-4):**
```csharp
// Same input but June 15 (daylight saving active)
UpdateDateTimeOffsetWithTimeZone("America/New_York", new DateTimeOffset(2022, 6, 15, 14, 0, 0, new TimeSpan(-7, 0, 0)))
// Should return: DateTimeOffset(2022, 6, 15, 14, 0, 0, new TimeSpan(-4, 0, 0))
```

**Why this matters:**
- NodaTime's `DateTimeZoneProviders.Tzdb[timeZoneId]` automatically handles DST transitions
- Input offset is ignored; only the local time (Y/M/D/H/M) and timezone ID matter
- Critical for feed readers that may receive times with wrong offsets

### 2. Deduplication Tests (3 new tests)

**SaveAsync_WithDeduplication_ShouldReuseDuplicateEngagementId:**
```csharp
// When ID = 0, SaveAsync triggers GetByNameAndUrlAndYearAsync lookup
var newEngagement = new Engagement { Id = 0, Name = "Tech Conf", Url = "...", ... };
// Finds existing: Engagement { Id = 42, ... }
// Result: new engagement gets ID = 42 before save
```

**SaveAsync_WithoutDeduplication_ShouldNotSearchIfIdIsNonZero:**
```csharp
// When ID > 0, deduplication check is skipped
var existing = new Engagement { Id = 15, ... };
// Repository.GetByNameAndUrlAndYearAsync NOT called
```

**SaveAsync_WithTimezoneCorrection_ShouldApplyToStartAndEndDateTime:**
```csharp
// Both StartDateTime and EndDateTime are timezone-corrected before save
engagement.StartDateTime.Offset == TimeSpan.FromHours(-4)  // EDT
engagement.EndDateTime.Offset == TimeSpan.FromHours(-4)    // EDT
```

### 3. Repository Delegation Tests (2 new tests)

**GetByNameAndUrlAndYearAsync_WithValidParameters_ShouldReturnEngagementFromRepository:**
- Ensures manager correctly delegates to data store

**GetByNameAndUrlAndYearAsync_WithNoDuplicateFound_ShouldReturnNull:**
- Validates null handling in deduplication path

## Implementation Details

### Test Framework Upgrades
- **Added:** FluentAssertions 7.1.1 to Managers.Tests.csproj
- **Pattern:** Method_Scenario_ExpectedResult naming (xUnit convention)
- **Assertions:** Switched from `Assert.Equal()` to `.Should().Be()` for fluent readability

### Why FluentAssertions?
```csharp
// Old style (hard to read complex assertions)
Assert.Equal(expectedDateTimeOffset, actualDateTimeOffset);
Assert.Equal(TimeSpan.FromHours(-5), result.Offset);

// New style (chainable, clear intent)
result.Should().Be(expectedDateTimeOffset);
result.Offset.Should().Be(TimeSpan.FromHours(-5));
```

## Test Results

**All 26 tests pass** (14 existing + 12 new):
- 0 failures
- Timezone offset validation: ✅ All DST transitions correct
- Deduplication logic: ✅ ID reuse and skip conditions work
- Repository isolation: ✅ Moq mocks prevent data store calls

## Decision Rationale

1. **Real-world scenarios:** EDT/EST/UTC tests reflect actual engagement data from different regions
2. **Deduplication critical:** Feed readers may produce duplicate engagement records; ID=0 triggers upsert logic
3. **Maintainability:** FluentAssertions + descriptive names make future failures easier to debug
4. **Isolation:** Mocked repository ensures tests validate EngagementManager logic, not data access layer

## Future Considerations

- If SaveTalkAsync deduplication is added, similar tests should apply
- Consider parameterized tests for DST edge cases (spring forward, fall back dates)
- Integration tests could verify actual database deduplication behavior

## Approval & Merge

- **Submitted:** PR #539
- **Reviewer:** Pending
- **Expected merge:** Once code review passes
