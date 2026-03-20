# Decision: Publisher Function Unit Tests Implementation

**Date:** 2026-03-20  
**Author:** Tank (QA Engineer)  
**Issue:** #301  
**PR:** #543  
**Branch:** squad/301-publisher-tests

## Context

Issue #301 requested unit tests for all publisher Azure Functions. The Functions.Tests project had only 4 model-initialization tests. All publisher trigger functions (PostPageStatus, PostText, PostLink, SendPost, all Process*Fired variants) were completely untested, leaving critical posting logic unverified.

## Decision

Created comprehensive unit test coverage for all publisher Azure Functions across Facebook, LinkedIn, and Bluesky platforms, focusing on queue-triggered posting functions (not EventGrid-triggered Process* functions which already have tests).

## Implementation

### Test Coverage (30 tests total)

#### Facebook Publisher (5 tests)
- **PostPageStatusTests.cs**: Queue-triggered function that posts status updates
  - Successful post without image
  - Successful post with image
  - Manager returns null (graceful handling)
  - FacebookPostException (rethrows)
  - Generic exception (rethrows)

#### LinkedIn Publisher (18 tests)
- **PostTextTests.cs** (4 tests): Simple text posting
- **PostLinkTests.cs** (7 tests): Link posting with image download fallback
- **PostImageTests.cs** (7 tests): Image posting with HTTP download scenarios

#### Bluesky Publisher (10 tests)
- **SendPostTests.cs**: Text posts with URL embedding and image support
  - Text-only posts
  - URL + shortened URL embedding
  - Image thumbnail embedding (with/without shortened URL)
  - Hashtag inclusion
  - Null handling and exception scenarios

### Testing Patterns Established

1. **Naming Convention**: `Method_Scenario_ExpectedResult`
   - Example: `Run_WithValidLinkWithImage_WhenImageDownloadSucceeds_CallsPostShareTextAndImage`

2. **Mock Setup Patterns**:
   - Standard mocking: `_manager.Setup(m => m.Method(...)).ReturnsAsync(result)`
   - HttpClient mocking: Use `Moq.Protected` for `SendAsync`
   - Sealed classes: Use `Mock.Of<T>()` or return null (can't use `new`)

3. **Verification Patterns**:
   - Positive: `_manager.Verify(m => m.Method(...), Times.Once)`
   - Negative: `_manager.Verify(m => m.Method(...), Times.Never)`

4. **Exception Testing**:
   - API-specific exceptions: Verify rethrow behavior
   - Generic exceptions: Verify rethrow or swallow based on function design

### Key Technical Challenges

1. **Sealed Class Mocking** (Bluesky):
   - `CreateRecordResult` and `EmbeddedExternal` are sealed
   - Solution: Use `Mock.Of<T>()` for non-null mocks or return null
   - Cannot use `new CreateRecordResult(...)` in tests

2. **HttpClient Mocking** (LinkedIn):
   - Requires `Moq.Protected()` to mock protected `SendAsync` method
   - Pattern: `_httpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ...)`

3. **Error Handling Variations**:
   - PostText/PostLink: Rethrow all exceptions
   - PostImage: Swallow all exceptions (logs only)
   - Tests verify these different patterns

4. **LinkedIn Image Fallback Logic**:
   - If image download fails (HTTP 404/500), fallback to link post
   - Test verifies `PostShareTextAndLink` called when image unavailable

5. **Bluesky Embed Logic**:
   - With shortened URL + image → use `GetEmbeddedExternalRecordWithThumbnail`
   - With URL only → use `GetEmbeddedExternalRecord`
   - Tests verify correct method selection

## Results

- ✅ All 30 tests pass
- ✅ Clean build (0 errors)
- ✅ Comprehensive coverage of success paths, error scenarios, fallback logic
- ✅ Follows established project patterns (xUnit, Moq, FluentAssertions naming)

## Impact

- **Issue #301**: CLOSED ✅
- **Test Coverage**: Publisher functions now have comprehensive unit test coverage
- **Confidence**: Posting logic verified for all three social platforms
- **Maintainability**: Clear test patterns established for future publisher functions

## Future Considerations

1. **Process*Fired Functions**: Already have tests in existing ProcessScheduledItemFiredTests files
2. **RefreshTokens Functions**: Already have tests (e.g., LinkedIn/RefreshTokensTests.cs)
3. **Integration Tests**: Unit tests mock manager layer - consider integration tests for end-to-end validation
4. **Coverage Metrics**: Run code coverage tool to identify any gaps

## References

- Issue: #301
- PR: #543
- Branch: squad/301-publisher-tests
- Related Files:
  - `src/JosephGuadagno.Broadcasting.Functions.Tests/Facebook/PostPageStatusTests.cs`
  - `src/JosephGuadagno.Broadcasting.Functions.Tests/LinkedIn/PostTextTests.cs`
  - `src/JosephGuadagno.Broadcasting.Functions.Tests/LinkedIn/PostLinkTests.cs`
  - `src/JosephGuadagno.Broadcasting.Functions.Tests/LinkedIn/PostImageTests.cs`
  - `src/JosephGuadagno.Broadcasting.Functions.Tests/Bluesky/SendPostTests.cs`
