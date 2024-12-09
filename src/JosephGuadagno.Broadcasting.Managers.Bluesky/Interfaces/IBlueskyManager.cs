using idunno.AtProto.Repo;
using idunno.AtProto.Repo.Models;
using idunno.Bluesky;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;

public interface IBlueskyManager
{
    Task<CreateRecordResponse?> PostText(string postText);
    Task<CreateRecordResponse?> Post(PostBuilder postBuilder);
    Task<bool> DeletePost(StrongReference strongReference);
}