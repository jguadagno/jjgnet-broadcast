using JosephGuadagno.Broadcasting.Domain.Interfaces;
using idunno.AtProto.Repo;
using idunno.Bluesky;
using idunno.Bluesky.Embed;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;

public interface IBlueskyManager
    : ISocialMediaPublisher
{
    Task<CreateRecordResult?> PostText(string postText);
    Task<CreateRecordResult?> Post(PostBuilder postBuilder);
    Task<bool> DeletePost(StrongReference strongReference);
    Task<EmbeddedExternal?> GetEmbeddedExternalRecord(string? externalUrl);

    /// <summary>
    /// Builds an <see cref="EmbeddedExternal"/> link card for <paramref name="externalUrl"/>,
    /// using <paramref name="thumbnailImageUrl"/> as the card thumbnail instead of fetching
    /// the og:image from the page.
    /// </summary>
    Task<EmbeddedExternal?> GetEmbeddedExternalRecordWithThumbnail(string externalUrl, string thumbnailImageUrl);
}
