using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Managers.Facebook;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Tests;

public class ExtensionsTests
{
    [Theory]
    [InlineData(Constants.TokenTypes.ShortLived, "short-lived")]
    [InlineData(Constants.TokenTypes.LongLived, "long-lived")]
    [InlineData(Constants.TokenTypes.Page, "page")]
    public void DisplayName_ReturnsExpectedDescription(Constants.TokenTypes tokenType, string expectedDisplayName)
    {
        // Act
        var result = tokenType.DisplayName();

        // Assert
        Assert.Equal(expectedDisplayName, result);
    }
}