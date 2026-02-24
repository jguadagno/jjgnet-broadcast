using System.ComponentModel;
using JosephGuadagno.Broadcasting.Domain;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Tests;

public class ExtensionsTests
{
    public enum TestEnum
    {
        [Description("first-value")] First,
        [Description("second-value")] Second,
        Third // no description, should fall back to name
    }

    [Theory]
    [InlineData(TestEnum.First, "first-value")]
    [InlineData(TestEnum.Second, "second-value")]
    [InlineData(TestEnum.Third, "Third")]
    public void DisplayName_ReturnsExpectedDescription(TestEnum value, string expected)
    {
        // Act
        var result = value.DisplayName();

        // Assert
        Assert.Equal(expected, result);
    }
}