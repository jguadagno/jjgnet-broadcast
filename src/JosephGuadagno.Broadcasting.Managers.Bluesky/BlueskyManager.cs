using idunno.AtProto.Repo;
using idunno.AtProto.Repo.Models;
using idunno.Bluesky;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky;

public class BlueskyManager(IBlueskySettings blueskySettings, ILogger<BlueskyManager> logger)
    : IBlueskyManager
{
    private readonly IBlueskySettings _blueskySettings = blueskySettings;
    private readonly ILogger<BlueskyManager> _logger = logger;

    public async Task<CreateRecordResponse?> PostText(string postText)
    {
        return await Post(new PostBuilder(postText));
    }

    public async Task<CreateRecordResponse?> Post(PostBuilder postBuilder)
    {
        BlueskyAgent agent = new ();

        var loginResult = await agent.Login(_blueskySettings.BlueskyUserName, _blueskySettings.BlueskyPassword);
        if (loginResult.Succeeded)
        {
            var response = await agent.Post(postBuilder);
            if (response.Succeeded)
            {
                return response.Result;
            }

            // Post Failed
            _logger.LogError($"Bluesky Post failed! Status Code: {response.StatusCode}, Error Details {response.AtErrorDetail}");
            return response.Result;
        }
        // Login Failed
        _logger.LogError($"Login failed. Status Code: {loginResult.StatusCode}, Error Details {loginResult.AtErrorDetail}");
        return null;
    }

    public async Task<bool> DeletePost(StrongReference strongReference)
    {
        BlueskyAgent agent = new ();

        var loginResult = await agent.Login(_blueskySettings.BlueskyUserName, _blueskySettings.BlueskyPassword);
        if (loginResult.Succeeded)
        {
            var response = await agent.DeletePost(strongReference);
            if (response.Succeeded)
            {
                _logger.LogDebug($"Bluesky Post successfully deleted! Cid: '{strongReference.Cid}'");
                return true;
            }

            _logger.LogWarning(
                $"Failed to delete Bluesky Post! Status Code: {loginResult.StatusCode}, Message: '{loginResult.AtErrorDetail?.Message}', Cid: {strongReference.Cid}");
            return false;
        }

        _logger.LogError(
            $"Failed to delete Bluesky Post! Login Failed! Status Code: {loginResult.StatusCode}, Message: '{loginResult.AtErrorDetail?.Message}', Cid: {strongReference.Cid}");

        return false;
    }
}