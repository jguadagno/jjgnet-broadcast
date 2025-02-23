using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Tests;

public class GraphManagerTests
{
    private readonly IMicrosoftGraphManager _microsoftGraphManager;
    private readonly IConfiguration _configuration;
    private readonly ITestOutputHelper _testOutputHelper;

    public GraphManagerTests(IMicrosoftGraphManager microsoftGraphManager, IConfiguration configuration, ITestOutputHelper testOutputHelper)
    {
        _microsoftGraphManager = microsoftGraphManager;
        _configuration = configuration;
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public async Task GetUsersForApi_ShouldReturnAllUsers()
    {
        // Arrange
        var clientCredentials = new Models.ClientSecretCredential
        {
            TenantId = null,
            ClientId = null,
            ClientSecret = null
        };
        _configuration.Bind("Settings:ApiCredentials", clientCredentials);

        // Act
        var users = await _microsoftGraphManager.GetUsers(clientCredentials);
        
        // Assert
        Assert.NotNull(users);
        Assert.True(users.Count > 0, "There should be at least one user");
    }
    
    [Fact]
    public async Task GetUsersForWeb_ShouldReturnAllUsers()
    {
        // Arrange
        var clientCredentials = new Models.ClientSecretCredential
        {
            TenantId = null,
            ClientId = null,
            ClientSecret = null
        };
        _configuration.Bind("Settings:WebCredentials", clientCredentials);

        // Act
        var users = await _microsoftGraphManager.GetUsers(clientCredentials);
        
        // Assert
        Assert.NotNull(users);
        Assert.True(users.Count > 0, "There should be at least one user");
    }
}