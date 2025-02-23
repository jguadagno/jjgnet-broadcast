using System.Collections.Generic;
using System.Threading.Tasks;
using idunno.Bluesky;
using idunno.Bluesky.RichText;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Tests;

public class BlueskyPostTests
{
    private readonly IBlueskyManager _blueskyManager;
    private readonly ITestOutputHelper _testOutputHelper;    
    private readonly ILogger<BlueskyPostTests> _logger;
    
    public BlueskyPostTests(IBlueskyManager blueskyManager, ITestOutputHelper testOutputHelper, ILogger<BlueskyPostTests> logger)
    {
        _blueskyManager = blueskyManager;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }
    
    [Fact]
    public async Task SendBlueskyPostText_Success()
    {
        // Arrange
        var message = "Test message - Testing in Production";
        
        // Act
        var response = await _blueskyManager.PostText(message);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Cid);
        
        // Clean up
        await _blueskyManager.DeletePost(response.StrongReference);
    }
    
    [Fact]
    public async Task SendBlueskyPostText_ICYMI_Success()
    {
        // Arrange
        var message = "ICYMI: (07/12/2024): \"My JetBrains Rider Plugins — July 2024 Edition\" RPs and feedback are always appreciated! ";
        message += "https://jjg.me/3LmnPiF ";
        message += "#JetBrains #Plugin #presenting #Rider";
        
        // Act
        var response = await _blueskyManager.PostText(message);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Cid);
        
        // Clean up
        await _blueskyManager.DeletePost(response.StrongReference);
    }
    
    [Fact]
    public async Task SendBlueskyPostWithLinksAndHashTags_Success()
    {
        // Arrange
        const string message = "ICYMI: (06/12/2020): \"Protecting an ASP.NET Core Web API with Microsoft Identity Platform.\" RPs and feedback are always appreciated! ";
        var postBuilder = new PostBuilder(message);
        const string url = "https://www.josephguadagno.net/2020/06/12/protecting-an-asp-net-core-api-with-microsoft-identity-platform";
        const string shortenedUrl = "https://jjg.me/30xE7PA";
        var hashTags = new List<string>
        {
            "Azure", "Identity", "WebAPI", "MSAL", "ManagedIdentity", "Entra"
        };

        postBuilder.Append(" ");
        postBuilder.Append(new Link(shortenedUrl, shortenedUrl));
        
        foreach (var hashtag in hashTags)
        {
            postBuilder.Append(" ");
            postBuilder.Append(new HashTag(hashtag));
        }
            
        // Act
        var response = await _blueskyManager.Post(postBuilder);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Cid);
        
        // Clean up
        await _blueskyManager.DeletePost(response.StrongReference);
    }
    
    [Fact]
    public async Task SendBlueskyPostWithLinksAndHashTagsAndEmbedded_Success()
    {
        // Arrange
        const string message = "ICYMI: (06/12/2020): \"Protecting an ASP.NET Core Web API with Microsoft Identity Platform.\" RPs and feedback are always appreciated! ";
        var postBuilder = new PostBuilder(message);
        const string url = "https://www.josephguadagno.net/2020/06/12/protecting-an-asp-net-core-api-with-microsoft-identity-platform";
        const string shortenedUrl = "https://jjg.me/30xE7PA";
        var hashTags = new List<string>
        {
            "Azure", "Identity", "WebAPI", "MSAL", "ManagedIdentity", "Entra"
        };
        
        postBuilder.Append(new Link(shortenedUrl, shortenedUrl));
        
        // Get the OpenGraph info to embed
        var embeddedExternalRecord = await _blueskyManager.GetEmbeddedExternalRecord(url);
        if (embeddedExternalRecord != null)
        {
            postBuilder.EmbedRecord(embeddedExternalRecord);
        }
        
        foreach (var hashtag in hashTags)
        {
            postBuilder.Append(" ");
            postBuilder.Append(new HashTag(hashtag));
        }
            
        // Act
        var response = await _blueskyManager.Post(postBuilder);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Cid);
        
        // Clean up
        await _blueskyManager.DeletePost(response.StrongReference);
    }
    
    [Fact]
    public async Task SendBlueskyPostWithLinksAndLimitedHashTagsAndEmbedded_Success()
    {
        // Arrange
        const string message = "ICYMI: (07/12/2024): \"My JetBrains Rider Plugins — July 2024 Edition\" RPs and feedback are always appreciated! ";
        var postBuilder = new PostBuilder(message);
        const string url = "https://www.josephguadagno.net/2024/07/12/my-jetbrains-rider-plugins-july-2024-edition";
        const string shortenedUrl = "https://jjg.me/3LmnPiF";
        var hashTags = new List<string>
        {
            "JetBrains", "Plugin", "presenting", "Rider"
        };

        // This line in the test will make the post highlight weird on Bluesky
        // postBuilder.Append(" ");
        postBuilder.Append(new Link(shortenedUrl, shortenedUrl));
        
        // Get the OpenGraph info to embed
        var embeddedExternalRecord = await _blueskyManager.GetEmbeddedExternalRecord(url);
        if (embeddedExternalRecord != null)
        {
            postBuilder.EmbedRecord(embeddedExternalRecord);
        }
        
        foreach (var hashtag in hashTags)
        {
            postBuilder.Append(" ");
            postBuilder.Append(new HashTag(hashtag));
        }
            
        // Act
        var response = await _blueskyManager.Post(postBuilder);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Cid);
        
        // Clean up
        await _blueskyManager.DeletePost(response.StrongReference);
    }
}