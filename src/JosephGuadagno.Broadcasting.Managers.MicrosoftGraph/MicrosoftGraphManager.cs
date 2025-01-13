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

    private readonly string[] _scopes =
    [
        "https://graph.microsoft.com/.default"
    ];
    
    public MicrosoftGraphManager(IKeyVault keyVault, ILogger<MicrosoftGraphManager> logger)
    {
        _keyVault = keyVault;
        _logger = logger;
    }

    private GraphServiceClient GetGraphServiceClient(Models.ClientSecretCredential credential)
    {
        return new GraphServiceClient(new ChainedTokenCredential(new ManagedIdentityCredential(),
            new ClientSecretCredential(credential.TenantId, credential.ClientId,
                credential.ClientSecret)), _scopes);
    }
    
    public async Task<List<User>> GetUsers(Models.ClientSecretCredential credentials)
    {
        List<User> users = [];
        try
        {
            var graphServiceClient = GetGraphServiceClient(credentials);
            var returnedUsers = await graphServiceClient.Users.GetAsync();
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