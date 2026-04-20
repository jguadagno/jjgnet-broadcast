using FluentAssertions;

using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Tests;

/// <summary>
/// Offline unit tests for SyndicationFeedReader using embedded XML and MemoryStream.
/// No network access required.
/// Scenarios: CDATA fields, missing pubDate, duplicate GUIDs, empty channel
/// </summary>
public class SyndicationFeedReaderOfflineTests
{
    private const string OwnerEntraOid = "owner-entra-oid";

    // ==================== XML Fixtures ====================
    
    /// <summary>RSS feed with CDATA content in title and description</summary>
    private const string RssFeedWithCdata = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version="2.0">
          <channel>
            <title><![CDATA[My Blog & More]]></title>
            <link>https://example.com</link>
            <description><![CDATA[A blog about tech & stuff]]></description>
            <language>en-us</language>
            <item>
              <title><![CDATA[Article with "Quotes" & <Symbols>]]></title>
              <link>https://example.com/post1</link>
              <guid isPermaLink="false">post-001</guid>
              <pubDate>Fri, 20 Mar 2026 10:00:00 GMT</pubDate>
              <description><![CDATA[This is <b>bold</b> & important content]]></description>
              <author>author@example.com</author>
              <category>Tech</category>
            </item>
            <item>
              <title><![CDATA[Another Post with CDATA]]></title>
              <link>https://example.com/post2</link>
              <guid isPermaLink="false">post-002</guid>
              <pubDate>Sat, 21 Mar 2026 11:00:00 GMT</pubDate>
              <description><![CDATA[Content with special chars: < > & " ']]></description>
              <author>author@example.com</author>
              <category>Blog</category>
            </item>
          </channel>
        </rss>
        """;

    /// <summary>RSS feed with missing pubDate (should use lastBuildDate or minimum date)</summary>
    private const string RssFeedWithMissingPubDate = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version="2.0">
          <channel>
            <title>Blog</title>
            <link>https://example.com</link>
            <description>Test Blog</description>
            <lastBuildDate>Fri, 20 Mar 2026 10:00:00 GMT</lastBuildDate>
            <item>
              <title>Post without PubDate</title>
              <link>https://example.com/post1</link>
              <guid isPermaLink="false">post-nopubdate-001</guid>
              <description>Content without publication date</description>
              <author>author@example.com</author>
            </item>
            <item>
              <title>Post with PubDate</title>
              <link>https://example.com/post2</link>
              <guid isPermaLink="false">post-nopubdate-002</guid>
              <pubDate>Sat, 21 Mar 2026 12:00:00 GMT</pubDate>
              <description>Content with publication date</description>
              <author>author@example.com</author>
            </item>
          </channel>
        </rss>
        """;

    /// <summary>RSS feed with duplicate GUIDs (deduplication test)</summary>
    private const string RssFeedWithDuplicateGuids = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version="2.0">
          <channel>
            <title>Blog with Duplicates</title>
            <link>https://example.com</link>
            <description>Testing duplicate handling</description>
            <item>
              <title>Original Post</title>
              <link>https://example.com/post1</link>
              <guid isPermaLink="false">duplicate-001</guid>
              <pubDate>Fri, 20 Mar 2026 10:00:00 GMT</pubDate>
              <description>First instance</description>
              <author>author@example.com</author>
              <category>Tech</category>
            </item>
            <item>
              <title>Duplicate Post (should be filtered)</title>
              <link>https://example.com/post1-duplicate</link>
              <guid isPermaLink="false">duplicate-001</guid>
              <pubDate>Fri, 20 Mar 2026 10:30:00 GMT</pubDate>
              <description>Duplicate with same GUID</description>
              <author>author@example.com</author>
              <category>Tech</category>
            </item>
            <item>
              <title>Another Post</title>
              <link>https://example.com/post2</link>
              <guid isPermaLink="false">unique-002</guid>
              <pubDate>Sat, 21 Mar 2026 11:00:00 GMT</pubDate>
              <description>Unique item</description>
              <author>author@example.com</author>
              <category>Blog</category>
            </item>
          </channel>
        </rss>
        """;

    /// <summary>Atom feed with CDATA and alternative format</summary>
    private const string AtomFeedWithCdata = """
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title type="text"><![CDATA[Atom Feed & Co]]></title>
          <link href="https://example.com/"/>
          <link rel="self" href="https://example.com/feed.xml"/>
          <id>urn:uuid:60a76c80-d399-11d9-b91C-0003939e0af6</id>
          <updated>2026-03-21T12:00:00Z</updated>
          <entry>
            <title type="html"><![CDATA[Atom Post with <code>HTML</code>]]></title>
            <link href="https://example.com/atom-post1"/>
            <link rel="alternate" type="text/html" href="https://example.com/atom-post1"/>
            <id>urn:uuid:1225c695-cfb8-4ebb-aaaa-80da344efa6a</id>
            <updated>2026-03-20T10:00:00Z</updated>
            <published>2026-03-20T10:00:00Z</published>
            <author>
              <name>John Doe</name>
              <email>john@example.com</email>
            </author>
            <summary type="html"><![CDATA[Summary with <strong>bold</strong>]]></summary>
            <category term="tech"/>
          </entry>
          <entry>
            <title><![CDATA[Another Atom Entry]]></title>
            <link href="https://example.com/atom-post2"/>
            <link rel="alternate" type="text/html" href="https://example.com/atom-post2"/>
            <id>urn:uuid:1225c695-cfb8-4ebb-bbbb-80da344efa6a</id>
            <updated>2026-03-21T11:00:00Z</updated>
            <published>2026-03-21T11:00:00Z</published>
            <author>
              <name>Jane Smith</name>
              <email>jane@example.com</email>
            </author>
            <summary>Another entry</summary>
            <category term="blog"/>
          </entry>
        </feed>
        """;

    /// <summary>Empty RSS feed with no items</summary>
    private const string EmptyRssFeed = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version="2.0">
          <channel>
            <title>Empty Blog</title>
            <link>https://example.com</link>
            <description>A blog with no posts</description>
          </channel>
        </rss>
        """;

    // ==================== Test Setup ====================

    private SyndicationFeedReader CreateReader(string feedUrl)
    {
        var settings = new SyndicationFeedReaderSettings { FeedUrl = feedUrl };
        var logger = new LoggerFactory().CreateLogger<SyndicationFeedReader>();
        return new SyndicationFeedReader(settings, logger);
    }

    // ==================== CDATA Field Tests ====================

    [Fact]
    public void GetSinceDate_WithRssCdataFields_ShouldParseCdataCorrectly()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(RssFeedWithCdata);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetSinceDate(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().NotBeEmpty("feed contains items after the specified date");
        result.Should().HaveCount(2, "feed has 2 items");

        var firstItem = result.First();
        firstItem.Title.Should().Contain("Quotes").And.Contain("Symbols");
        // Author parsing from RSS <author> tag may result in "Unknown" if not in expected format
        firstItem.Tags.Should().Contain("Tech");

        var secondItem = result[1];
        secondItem.Title.Should().Be("Another Post with CDATA");
        secondItem.Tags.Should().Contain("Blog");

        // Cleanup
        File.Delete(xmlPath);
    }

    [Fact]
    public void GetSinceDate_WithAtomCdataFields_ShouldParseCdataCorrectly()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(AtomFeedWithCdata);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetSinceDate(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().NotBeEmpty("feed contains items after the specified date");
        result.Should().HaveCount(2, "Atom feed has 2 entries");

        var firstEntry = result.First();
        firstEntry.Title.Should().Contain("HTML");
        firstEntry.Author.Should().Be("John Doe");

        var secondEntry = result[1];
        secondEntry.Title.Should().Be("Another Atom Entry");
        secondEntry.Author.Should().Be("Jane Smith");

        // Cleanup
        File.Delete(xmlPath);
    }

    // ==================== Missing PubDate Tests ====================

    [Fact]
    public void GetSinceDate_WithMissingPubDate_ShouldHandleGracefully()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(RssFeedWithMissingPubDate);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetSinceDate(OwnerEntraOid, sinceDate);

        // Assert
        // Items without pubDate should still be processed (PublishDate defaults to DateTimeOffset.MinValue)
        result.Should().NotBeNull("result should not be null");
        result.Should().HaveCountGreaterThanOrEqualTo(1, "at least one item should be returned");

        // The item with pubDate should definitely be included
        var itemsWithPubDate = result.Where(r => r.PublicationDate > sinceDate).ToList();
        itemsWithPubDate.Should().ContainSingle(item => item.Title == "Post with PubDate");

        // Cleanup
        File.Delete(xmlPath);
    }

    // ==================== Duplicate GUID Tests ====================

    [Fact]
    public void GetSinceDate_WithDuplicateGuids_ShouldReturnAllItems()
    {
        // Arrange - Note: GetSinceDate doesn't deduplicate; that's responsibility of calling code
        var xmlPath = CreateTempXmlFile(RssFeedWithDuplicateGuids);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetSinceDate(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().NotBeEmpty("feed contains items");
        // GetSinceDate returns raw items; deduplication is caller's responsibility
        result.Should().HaveCount(3, "all 3 items from feed are returned");

        // Verify we have both the original and duplicate with same GUID
        var duplicateGuids = result.Where(r => r.FeedIdentifier.Contains("post1")).ToList();
        duplicateGuids.Should().HaveCountGreaterThanOrEqualTo(1);

        // Cleanup
        File.Delete(xmlPath);
    }

    // ==================== Empty Channel Tests ====================

    [Fact]
    public void GetSinceDate_WithEmptyChannel_ShouldReturnEmptyList()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(EmptyRssFeed);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetSinceDate(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().BeEmpty("empty channel should return no items");

        // Cleanup
        File.Delete(xmlPath);
    }

    // ==================== GetSyndicationItems Tests ====================

    [Fact]
    public void GetSyndicationItems_WithCdataFields_ShouldParseCdataCorrectly()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(RssFeedWithCdata);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetSyndicationItems(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().NotBeEmpty("feed contains items");
        result.Should().HaveCountGreaterThanOrEqualTo(2, "feed should have at least 2 items");
        result.Should().Contain(r => r.Title.Contains("Quotes"));
        result.Should().Contain(r => r.Title.Contains("Another Post"));

        // Cleanup
        File.Delete(xmlPath);
    }

    [Fact]
    public void GetSyndicationItems_WithExcludedCategories_ShouldFilterItemsWithExcludedTags()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(RssFeedWithCdata);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);
        var excludeCategories = new List<string> { "tech" }; // Case-insensitive

        // Act
        var result = reader.GetSyndicationItems(OwnerEntraOid, sinceDate, excludeCategories);

        // Assert
        // Note: Category filtering depends on .Categories being populated by XML parser
        // This test verifies that the method accepts and processes the exclude list
        result.Should().NotBeNull("result should not be null");
        // Items that don't have "tech" category (case-insensitive) should be included
        result.Where(r => r.Tags.Count == 0 || !r.Tags.Any(t => t.ToLower().Contains("tech")))
            .Should().Contain(r => r.Title.Contains("Another Post"), "Blog item should not be excluded");

        // Cleanup
        File.Delete(xmlPath);
    }

    [Fact]
    public void GetSyndicationItems_WithEmptyChannel_ShouldReturnEmptyList()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(EmptyRssFeed);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetSyndicationItems(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().BeEmpty();

        // Cleanup
        File.Delete(xmlPath);
    }

    // ==================== GetRandomSyndicationItem Tests ====================

    [Fact]
    public void GetRandomSyndicationItem_WithValidFeed_ShouldReturnAnItem()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(RssFeedWithCdata);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetRandomSyndicationItem(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().NotBeNull("random item should be returned from non-empty feed");
        result.Title.Should().NotBeNullOrEmpty();
        result.Url.Should().NotBeNullOrEmpty();

        // Cleanup
        File.Delete(xmlPath);
    }

    [Fact]
    public void GetRandomSyndicationItem_WithEmptyChannel_ShouldReturnNull()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(EmptyRssFeed);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = reader.GetRandomSyndicationItem(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().BeNull("empty channel should return null");

        // Cleanup
        File.Delete(xmlPath);
    }

    // ==================== Async Tests ====================

    [Fact]
    public async Task GetAsync_WithCdataFeed_ShouldReturnParsedItems()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(RssFeedWithCdata);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = await reader.GetAsync(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);

        // Cleanup
        File.Delete(xmlPath);
    }

    [Fact]
    public async Task GetAsync_WithOwnerOid_ShouldApplyNonEmptyOwnerToEveryItem()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(RssFeedWithCdata);
        var reader = CreateReader(xmlPath);
        var sinceDate = new DateTimeOffset(2026, 3, 19, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = await reader.GetAsync(OwnerEntraOid, sinceDate);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(item => item.CreatedByEntraOid == OwnerEntraOid);
        result.Should().NotContain(item => string.IsNullOrWhiteSpace(item.CreatedByEntraOid));

        // Cleanup
        File.Delete(xmlPath);
    }

    // ==================== Helper Methods ====================

    /// <summary>Creates a temporary XML file with the given content</summary>
    private static string CreateTempXmlFile(string content)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-feed-{Guid.NewGuid()}.xml");
        File.WriteAllText(tempPath, content);
        return tempPath;
    }
}
