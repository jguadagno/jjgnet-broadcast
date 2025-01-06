using Azure.Core;
using Azure.Identity;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph;

public class MicrosoftGraphManager: IMicrosoftGraphManager
{
    private readonly IKeyVault _keyVault;
    private readonly ILogger<MicrosoftGraphManager> _logger;
    
    private readonly GraphServiceClient _graphClient;
    
    public MicrosoftGraphManager(IKeyVault keyVault, TokenCredential tokenCredential, ILogger<MicrosoftGraphManager> logger)
    {
        _keyVault = keyVault;
        _logger = logger;
        
        var credential = new ChainedTokenCredential(
            new ManagedIdentityCredential(),
            new EnvironmentCredential());

        string[] scopes = ["https://graph.microsoft.com/.default"];

        _graphClient = new GraphServiceClient(
            tokenCredential, scopes);
    }

    public async Task<List<User>> GetUsers()
    {
        List<User> users = [];
        try
        {
            var returnedUsers = await _graphClient.Users.GetAsync();
            if (returnedUsers == null)
            {
                _logger.LogWarning("No users returned");
                return users;
            }

            if (returnedUsers.Value != null)
            {
                users.AddRange(returnedUsers.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
        }
        return users;
    }
    
}