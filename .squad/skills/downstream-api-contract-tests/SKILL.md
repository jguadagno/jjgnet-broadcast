---
name: "downstream-api-contract-tests"
description: "Verify ASP.NET Web services build the right IDownstreamApi route and payload without standing up HTTP"
domain: "testing"
confidence: "high"
source: "earned (Issue #708 service coverage audit)"
---

## Context

In this repo, MVC controller tests often mock the service layer, and API tests start at the API controller. That can leave a blind spot in the middle: the Web service that calls `IDownstreamApi`.

## Pattern

For a service method that calls `IDownstreamApi`, instantiate the real service with `Mock<IDownstreamApi>` and inspect `mock.Invocations` after the call.

### Example

```csharp
var api = new Mock<IDownstreamApi>();
var sut = new EngagementService(api.Object);

await sut.AddPlatformToEngagementAsync(42, 7, "@tank");

var invocation = api.Invocations.Should().ContainSingle().Subject;
invocation.Arguments[0].Should().Be("JosephGuadagnoBroadcastingApi");

var request = invocation.Arguments[1];
request!.GetType().GetProperty("SocialMediaPlatformId")!.GetValue(request).Should().Be(7);
request.GetType().GetProperty("Handle")!.GetValue(request).Should().Be("@tank");

var configure = invocation.Arguments[2]
    .Should().BeAssignableTo<Action<DownstreamApiOptionsReadOnlyHttpMethod>>().Subject;
var options = new DownstreamApiOptionsReadOnlyHttpMethod(new DownstreamApiOptions(), HttpMethod.Post.Method);
configure(options);
options.RelativePath.Should().Be("/engagements/42/platforms");
```

## When to use

- Web controller tests stop at `IService`
- API tests start at the API controller
- A bug report points at a Web service method that builds an outbound request
- You need to prove route, payload, or optional-field shape without running a live API

## Why this helps

This catches contract bugs that controller tests and API tests both miss, while staying fast and isolated. It is especially useful for anonymous request payloads where the shape matters but there is no dedicated DTO type in the Web project.
