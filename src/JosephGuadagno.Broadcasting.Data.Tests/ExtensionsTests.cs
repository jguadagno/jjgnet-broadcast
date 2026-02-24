using System.ComponentModel;
using JosephGuadagno.Broadcasting.Domain;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Tests;

public class ExtensionsTests
{
    public enum TestEnum
    {
        [Description("Test Description")]
        TestMember,
        NoDescription
    }

    [Theory]
    [InlineData(TestEnum.TestMember, "Test Description")]
    [InlineData(TestEnum.NoDescription, "NoDescription")]
    public void DisplayName_ReturnsExpectedDescription(TestEnum value, string expectedDisplayName)
    {
        // Act
        var result = value.DisplayName();

        // Assert
        Assert.Equal(expectedDisplayName, result);
    }
}