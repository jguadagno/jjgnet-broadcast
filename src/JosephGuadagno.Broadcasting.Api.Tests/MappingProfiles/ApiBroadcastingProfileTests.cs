using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.MappingProfiles;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Api.Tests.MappingProfiles;

public class ApiBroadcastingProfileTests
{
    private static readonly string[] RemovedEngagementFields =
    [
        nameof(EngagementContract.BlueSkyHandle),
        nameof(EngagementContract.ConferenceHashtag),
        nameof(EngagementContract.ConferenceTwitterHandle)
    ];

    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<ApiBroadcastingProfile>(),
        new LoggerFactory())
        .CreateMapper();

    [Theory]
    [InlineData(typeof(EngagementRequest))]
    [InlineData(typeof(EngagementResponse))]
    public void EngagementDtos_WhenInspectingPublicProperties_ShouldNotExposeRemovedSocialFields(Type dtoType)
    {
        var propertyNames = dtoType.GetProperties().Select(property => property.Name);

        propertyNames.Should().NotContain(RemovedEngagementFields);
    }

    [Fact]
    public void ApiBroadcastingProfile_WhenMappingEngagementRequest_ShouldPopulateSupportedDomainFields()
    {
        var request = new EngagementRequest
        {
            Name = "SpringOne",
            Url = "https://springone.example.com",
            StartDateTime = DateTimeOffset.Parse("2026-09-14T09:00:00+00:00"),
            EndDateTime = DateTimeOffset.Parse("2026-09-16T17:00:00+00:00"),
            TimeZoneId = "UTC",
            Comments = "Bring backup batteries."
        };

        var result = Mapper.Map<Engagement>(request);

        result.Name.Should().Be(request.Name);
        result.Url.Should().Be(request.Url);
        result.StartDateTime.Should().Be(request.StartDateTime);
        result.EndDateTime.Should().Be(request.EndDateTime);
        result.TimeZoneId.Should().Be(request.TimeZoneId);
        result.Comments.Should().Be(request.Comments);
    }

    [Fact]
    public void ApiBroadcastingProfile_WhenMappingEngagementResponse_ShouldPopulateSupportedDtoFields()
    {
        var engagement = new Engagement
        {
            Id = 42,
            Name = "SpringOne",
            Url = "https://springone.example.com",
            StartDateTime = DateTimeOffset.Parse("2026-09-14T09:00:00+00:00"),
            EndDateTime = DateTimeOffset.Parse("2026-09-16T17:00:00+00:00"),
            TimeZoneId = "UTC",
            Comments = "Bring backup batteries.",
            CreatedOn = DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"),
            LastUpdatedOn = DateTimeOffset.Parse("2026-01-15T00:00:00+00:00")
        };

        var result = Mapper.Map<EngagementResponse>(engagement);

        result.Should().BeEquivalentTo(engagement, options => options
            .ExcludingMissingMembers()
            .Excluding(response => response.Talks));
    }

    private sealed class EngagementContract
    {
        public string? BlueSkyHandle { get; init; }
        public string? ConferenceHashtag { get; init; }
        public string? ConferenceTwitterHandle { get; init; }
    }
}
