using idunno.AtProto.Repo;
using idunno.Bluesky;
using idunno.Bluesky.Embed;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;

public interface IBlueskyManager
{
    Task<CreateRecordResponse?> PostText(string postText);
    Task<CreateRecordResponse?> Post(PostBuilder postBuilder);
    Task<bool> DeletePost(StrongReference strongReference);
    Task<EmbeddedExternal?> GetEmbeddedExternalRecord(string externalUrl);
}