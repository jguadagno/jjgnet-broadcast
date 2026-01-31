namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Tests;

public class ModelsTests
{
    [Fact]
    public void SpeakerEngagementsReaderSettings_Properties_Work()
    {
        // Arrange & Act
        var settings = new Models.SpeakerEngagementsReaderSettings
        {
            SpeakerEngagementsFile = "https://example.com"
        };

        // Assert
        Assert.Equal("https://example.com", settings.SpeakerEngagementsFile);
    }

    [Fact]
    public void Engagement_Properties_Work()
    {
        // Arrange & Act
        var engagement = new Models.Engagement
        {
            EventName = "Event",
            EventUrl = "Url",
            Location = "Location",
            EventStart = new DateTime(2023, 1, 1),
            EventEnd = new DateTime(2023, 1, 2),
            Comments = "Comments",
            IsCanceled = true,
            IsCurrent = true,
            Timezone = "Timezone",
            InPerson = "Yes",
            CreatedOrUpdatedOn = new DateTime(2023, 1, 1),
            Presentations = new List<Models.Presentation>()
        };

        // Assert
        Assert.Equal("Event", engagement.EventName);
        Assert.Equal("Url", engagement.EventUrl);
        Assert.Equal("Location", engagement.Location);
        Assert.Equal(new DateTime(2023, 1, 1), engagement.EventStart);
        Assert.Equal(new DateTime(2023, 1, 2), engagement.EventEnd);
        Assert.Equal("Comments", engagement.Comments);
        Assert.True(engagement.IsCanceled);
        Assert.True(engagement.IsCurrent);
        Assert.Equal("Timezone", engagement.Timezone);
        Assert.Equal("Yes", engagement.InPerson);
        Assert.Equal(new DateTime(2023, 1, 1), engagement.CreatedOrUpdatedOn);
        Assert.NotNull(engagement.Presentations);
    }

    [Fact]
    public void Presentation_Properties_Work()
    {
        // Arrange & Act
        var presentation = new Models.Presentation
        {
            Name = "Name",
            Url = "Url",
            PresentationStartDateTime = new DateTime(2023, 1, 1),
            PresentationEndDateTime = new DateTime(2023, 1, 2),
            Room = "Room",
            Comments = "Comments",
            IsCanceled = true,
            IsWorkshop = true,
            IsKeynote = true,
            IsPanel = true
        };

        // Assert
        Assert.Equal("Name", presentation.Name);
        Assert.Equal("Url", presentation.Url);
        Assert.Equal(new DateTime(2023, 1, 1), presentation.PresentationStartDateTime);
        Assert.Equal(new DateTime(2023, 1, 2), presentation.PresentationEndDateTime);
        Assert.Equal("Room", presentation.Room);
        Assert.Equal("Comments", presentation.Comments);
        Assert.True(presentation.IsCanceled);
        Assert.True(presentation.IsWorkshop);
        Assert.True(presentation.IsKeynote);
        Assert.True(presentation.IsPanel);
    }
}