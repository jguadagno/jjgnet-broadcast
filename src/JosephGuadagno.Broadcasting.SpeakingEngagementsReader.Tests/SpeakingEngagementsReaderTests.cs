using System.Net;
using System.Net.Http.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Tests;

public class SpeakingEngagementsReaderTests
{
    private readonly Mock<ISpeakerEngagementsReaderSettings> _mockSettings;
    private readonly Mock<ILogger<SpeakingEngagementsReader>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public SpeakingEngagementsReaderTests()
    {
        _mockSettings = new Mock<ISpeakerEngagementsReaderSettings>();
        _mockLogger = new Mock<ILogger<SpeakingEngagementsReader>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new SpeakingEngagementsReader(_httpClient, null, _mockLogger.Object));
        Assert.Equal("settings", ex.ParamName);
        Assert.Contains("The SpeakerEngagementsReaderSettings cannot be null", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptySpeakerEngagementsFile_ThrowsApplicationException()
    {
        // Arrange
        _mockSettings.Setup(s => s.SpeakerEngagementsFile).Returns(string.Empty);

        // Act & Assert
        var ex = Assert.Throws<ApplicationException>(() => new SpeakingEngagementsReader(_httpClient, _mockSettings.Object, _mockLogger.Object));
        Assert.Equal("The SpeakerEngagementsFile of the SpeakerEngagementsReaderSettings is required", ex.Message);
    }

    [Fact]
    public async Task GetAll_ReturnsAllEngagements()
    {
        // Arrange
        var sourceEngagements = new List<Models.Engagement>
        {
            new Models.Engagement
            {
                EventName = "Test Event",
                EventUrl = "https://example.com",
                EventStart = new DateTime(2023, 1, 1),
                EventEnd = new DateTime(2023, 1, 2),
                Timezone = "Eastern Standard Time",
                Comments = "Test Comments",
                Presentations = new List<Models.Presentation>
                {
                    new Models.Presentation
                    {
                        Name = "Test Talk",
                        Url = "https://example.com/talk",
                        Room = "Room 1",
                        Comments = "Talk Comments"
                    }
                }
            }
        };

        _mockSettings.Setup(s => s.SpeakerEngagementsFile).Returns("https://example.com/data.json");
        
        SetupMockHttpMessageHandler(HttpStatusCode.OK, sourceEngagements);

        var reader = new SpeakingEngagementsReader(_httpClient, _mockSettings.Object, _mockLogger.Object);

        // Act
        var result = await reader.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var engagement = result[0];
        Assert.Equal("Test Event", engagement.Name);
        Assert.Equal("https://example.com", engagement.Url);
        Assert.Equal(new DateTime(2023, 1, 1), engagement.StartDateTime);
        Assert.Equal(new DateTime(2023, 1, 2), engagement.EndDateTime);
        Assert.Equal("Eastern Standard Time", engagement.TimeZoneId);
        Assert.Equal("Test Comments", engagement.Comments);
        Assert.NotNull(engagement.Talks);
        Assert.Single(engagement.Talks);
        var talk = engagement.Talks[0];
        Assert.Equal("Test Talk", talk.Name);
        Assert.Equal("https://example.com/talk", talk.UrlForTalk);
        Assert.Equal("https://example.com/talk", talk.UrlForConferenceTalk);
        Assert.Equal("Room 1", talk.TalkLocation);
        Assert.Equal("Talk Comments", talk.Comments);
        Assert.Equal(new DateTime(2023, 1, 1), talk.StartDateTime);
        Assert.Equal(new DateTime(2023, 1, 2), talk.EndDateTime);
    }

    [Fact]
    public async Task GetSinceDate_FiltersEngagementsCorrecty()
    {
        // Arrange
        var sinceDate = new DateTime(2023, 1, 1);
        var sourceEngagements = new List<Models.Engagement>
        {
            new Models.Engagement
            {
                EventName = "Old Event",
                CreatedOrUpdatedOn = new DateTime(2022, 12, 31)
            },
            new Models.Engagement
            {
                EventName = "New Event",
                CreatedOrUpdatedOn = new DateTime(2023, 1, 2)
            }
        };

        _mockSettings.Setup(s => s.SpeakerEngagementsFile).Returns("https://example.com/data.json");
        SetupMockHttpMessageHandler(HttpStatusCode.OK, sourceEngagements);

        var reader = new SpeakingEngagementsReader(_httpClient, _mockSettings.Object, _mockLogger.Object);

        // Act
        var result = await reader.GetSinceDate(sinceDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("New Event", result[0].Name);
    }

    [Fact]
    public async Task GetAll_WithNoPresentations_ReturnsEngagementsWithoutTalks()
    {
        // Arrange
        var sourceEngagements = new List<Models.Engagement>
        {
            new Models.Engagement
            {
                EventName = "No Talk Event",
                Presentations = new List<Models.Presentation>()
            }
        };

        _mockSettings.Setup(s => s.SpeakerEngagementsFile).Returns("https://example.com/data.json");
        SetupMockHttpMessageHandler(HttpStatusCode.OK, sourceEngagements);

        var reader = new SpeakingEngagementsReader(_httpClient, _mockSettings.Object, _mockLogger.Object);

        // Act
        var result = await reader.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result[0].Talks);
    }

    [Fact]
    public async Task GetAll_WithPresentationDates_UsesPresentationDates()
    {
        // Arrange
        var eventStart = new DateTime(2023, 1, 1, 10, 0, 0);
        var eventEnd = new DateTime(2023, 1, 1, 17, 0, 0);
        var talkStart = new DateTime(2023, 1, 1, 13, 0, 0);
        var talkEnd = new DateTime(2023, 1, 1, 14, 0, 0);

        var sourceEngagements = new List<Models.Engagement>
        {
            new Models.Engagement
            {
                EventName = "Dated Event",
                EventStart = eventStart,
                EventEnd = eventEnd,
                Presentations = new List<Models.Presentation>
                {
                    new Models.Presentation
                    {
                        Name = "Dated Talk",
                        PresentationStartDateTime = talkStart,
                        PresentationEndDateTime = talkEnd
                    },
                    new Models.Presentation
                    {
                        Name = "Fallback Talk",
                        PresentationStartDateTime = null,
                        PresentationEndDateTime = null
                    }
                }
            }
        };

        _mockSettings.Setup(s => s.SpeakerEngagementsFile).Returns("https://example.com/data.json");
        SetupMockHttpMessageHandler(HttpStatusCode.OK, sourceEngagements);

        var reader = new SpeakingEngagementsReader(_httpClient, _mockSettings.Object, _mockLogger.Object);

        // Act
        var result = await reader.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(2, result[0].Talks.Count);
        
        var datedTalk = result[0].Talks.First(t => t.Name == "Dated Talk");
        Assert.Equal(talkStart, datedTalk.StartDateTime);
        Assert.Equal(talkEnd, datedTalk.EndDateTime);

        var fallbackTalk = result[0].Talks.First(t => t.Name == "Fallback Talk");
        Assert.Equal(eventStart, fallbackTalk.StartDateTime);
        Assert.Equal(eventEnd, fallbackTalk.EndDateTime);
    }

    [Fact]
    public async Task LoadAllSpeakingEngagements_OnException_LogsErrorAndReturnsEmptyList()
    {
        // Arrange
        _mockSettings.Setup(s => s.SpeakerEngagementsFile).Returns("https://example.com/data.json");
        
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new Exception("Network error"));

        var reader = new SpeakingEngagementsReader(_httpClient, _mockSettings.Object, _mockLogger.Object);

        // Act
        var result = await reader.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to load all the speaking engagements")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private void SetupMockHttpMessageHandler<T>(HttpStatusCode statusCode, T content)
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = JsonContent.Create(content)
            });
    }
}