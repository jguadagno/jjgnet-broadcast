# Tank — SocialMediaPublisher contract test coverage

## Decision
For shared publisher contract work, tests should cover three layers:
1. interface shape (`PublishAsync(SocialMediaPublishRequest)`),
2. interface inheritance on each platform-specific manager interface, and
3. one manager-specific `PublishAsync` routing or guard path that proves the shared contract preserves existing behavior.

## Why
That combination catches signature drift, missing shared wiring, and platform-specific regressions without duplicating every legacy method test under the new abstraction.

## Applied In
- `src\JosephGuadagno.Broadcasting.Managers.Twitter.Tests\TwitterManagerTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Bluesky.Tests\BlueskyManagerUnitTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Facebook.Tests\FacebookManagerUnitTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests\LinkedInManagerUnitTests.cs`
