using Microsoft.Graph.Models;

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;

public interface IMicrosoftGraphManager
{
    public Task<List<User>> GetUsers(Models.ClientSecretCredential credentials);
}