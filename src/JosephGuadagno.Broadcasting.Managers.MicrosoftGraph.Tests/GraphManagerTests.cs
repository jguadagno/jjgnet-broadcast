using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Tests;

public class GraphManagerTests
{
    private readonly IMicrosoftGraphManager _microsoftGraphManager;
    private readonly ITestOutputHelper _testOutputHelper;

    public GraphManagerTests(IMicrosoftGraphManager microsoftGraphManager, ITestOutputHelper testOutputHelper)
    {
        _microsoftGraphManager = microsoftGraphManager;
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public async Task GetUsers_ShouldReturnAllUsers()
    {
        // Arrange
        
        // Act
        var users = await _microsoftGraphManager.GetUsers();
        
        // Assert
        Assert.NotNull(users);
        Assert.True(users.Count > 0, "There should be at least one user");
    }
}