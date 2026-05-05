---
name: "social-media-publisher-contract"
description: "Implement a common social media publisher contract without breaking existing platform managers"
domain: "api-design"
confidence: "high"
source: "earned"
tools:
  - name: "powershell"
    description: "Build and test the JJGNet Broadcasting solution"
    when: "After updating shared publisher contracts or DI registrations"
---

## Context
Use this pattern when multiple platform-specific managers need a common contract for routing or contract testing, but existing function handlers or callers still depend on platform-specific methods.

## Patterns
- Define the shared contract in `JosephGuadagno.Broadcasting.Domain\Interfaces` so all manager projects can depend on it without introducing upward references.
- Use a superset request model such as `SocialMediaPublishRequest` in `JosephGuadagno.Broadcasting.Domain\Models` to normalize shared inputs like text, links, images, access tokens, and hashtags.
- Preserve existing platform-specific interfaces and methods; make them inherit the common contract instead of replacing them in one step.
- Register each manager both by its specific interface and as `ISocialMediaPublisher` so future code can resolve `IEnumerable<ISocialMediaPublisher>` while current code keeps using the existing interfaces.
- Test the contract at three layers: shared interface shape, per-manager interface inheritance, and one `PublishAsync` routing or guard path per platform manager.
- Keep `Functions\Program.cs` and `Functions.Tests\Startup.cs` in sync whenever shared publisher registrations change.

## Examples
- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\ISocialMediaPublisher.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Models\SocialMediaPublishRequest.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Program.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Twitter.Tests\TwitterManagerTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests\LinkedInManagerUnitTests.cs`

## Anti-Patterns
- Do not move platform-specific OAuth or media parameters directly onto the shared interface method signature.
- Do not remove the existing platform-specific manager methods before downstream Functions and tests have been migrated.
- Do not register a second concrete manager instance just to satisfy `ISocialMediaPublisher`; map the shared registration back to the existing interface registration.
- Do not rely on only one test layer; shape-only tests miss routing regressions, and behavior-only tests miss contract drift.
