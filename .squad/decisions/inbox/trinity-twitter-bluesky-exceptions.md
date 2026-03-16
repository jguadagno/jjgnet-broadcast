# Twitter/Bluesky Exception Implementation Decisions

## Files Created
- `src/JosephGuadagno.Broadcasting.Managers.Twitter/Exceptions/TwitterPostException.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/Exceptions/BlueskyPostException.cs`

## Files Modified
- `src/JosephGuadagno.Broadcasting.Managers.Twitter/TwitterManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/BlueskyManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/JosephGuadagno.Broadcasting.Managers.Bluesky.csproj`

## Decisions

### TwitterManager
- `SendTweetAsync` now throws `TwitterPostException` instead of returning `null` on both null-tweet and exception paths
- Added a `catch (TwitterPostException) { throw; }` re-throw guard so the inner TwitterPostException created from a null tweet propagates cleanly through the outer catch

### BlueskyManager
- `Post()` now throws `BlueskyPostException` for both login failure and post failure paths, with HTTP status code and API error message captured via the `apiErrorCode`/`apiErrorMessage` constructor
- `DeletePost()` left unchanged — returns `false` on failure (boolean method, not a post operation)
- `GetEmbeddedExternalRecord()` thumbnail `HttpRequestException` catch left intentionally silent (per existing comment)
- Added `ProjectReference` to `JosephGuadagno.Broadcasting.Domain` in Bluesky csproj (was missing)

### BroadcastingException Wait
- Waited ~2 minutes for the base class to be created by the other Trinity agent before proceeding
