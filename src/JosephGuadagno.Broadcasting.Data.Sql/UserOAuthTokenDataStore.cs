using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for user OAuth tokens
/// </summary>
public class UserOAuthTokenDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserOAuthTokenDataStore> logger) : IUserOAuthTokenDataStore
{
    /// <inheritdoc />
    public async Task<UserOAuthToken?> GetByUserAndPlatformAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        var entity = await broadcastingContext.UserOAuthTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.CreatedByEntraOid == ownerOid && t.SocialMediaPlatformId == platformId,
                cancellationToken);

        return entity is null ? null : mapper.Map<UserOAuthToken>(entity);
    }

    /// <inheritdoc />
    public async Task<UserOAuthToken?> UpsertAsync(
        UserOAuthToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(token.CreatedByEntraOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(token.SocialMediaPlatformId);

        try
        {
            var existing = await broadcastingContext.UserOAuthTokens
                .FirstOrDefaultAsync(
                    t => t.CreatedByEntraOid == token.CreatedByEntraOid
                         && t.SocialMediaPlatformId == token.SocialMediaPlatformId,
                    cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserOAuthToken
                {
                    CreatedByEntraOid = token.CreatedByEntraOid,
                    SocialMediaPlatformId = token.SocialMediaPlatformId,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserOAuthTokens.Add(existing);
            }

            existing.AccessToken = token.AccessToken;
            existing.RefreshToken = token.RefreshToken;
            existing.AccessTokenExpiresAt = token.AccessTokenExpiresAt;
            existing.RefreshTokenExpiresAt = token.RefreshTokenExpiresAt;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return await GetByUserAndPlatformAsync(token.CreatedByEntraOid, token.SocialMediaPlatformId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to upsert OAuth token for owner {OwnerOid} and platform {PlatformId}",
                LogSanitizer.Sanitize(token.CreatedByEntraOid),
                token.SocialMediaPlatformId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        try
        {
            var existing = await broadcastingContext.UserOAuthTokens
                .FirstOrDefaultAsync(
                    t => t.CreatedByEntraOid == ownerOid && t.SocialMediaPlatformId == platformId,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserOAuthTokens.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete OAuth token for owner {OwnerOid} and platform {PlatformId}",
                LogSanitizer.Sanitize(ownerOid),
                platformId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<UserOAuthToken>> GetExpiringAsync(
        DateTimeOffset threshold, CancellationToken cancellationToken = default)
    {
        var entities = await broadcastingContext.UserOAuthTokens
            .AsNoTracking()
            .Where(t => t.AccessTokenExpiresAt <= threshold)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserOAuthToken>>(entities);
    }

    /// <inheritdoc />
    public async Task<List<UserOAuthToken>> GetExpiringWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException($"'from' ({from:O}) must be less than or equal to 'to' ({to:O}).", nameof(from));
        }

        var entities = await broadcastingContext.UserOAuthTokens
            .AsNoTracking()
            .Where(t => t.AccessTokenExpiresAt >= from && t.AccessTokenExpiresAt <= to)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserOAuthToken>>(entities);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateLastNotifiedAtAsync(
        string ownerOid, int platformId, DateTimeOffset notifiedAt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        try
        {
            var existing = await broadcastingContext.UserOAuthTokens
                .FirstOrDefaultAsync(
                    t => t.CreatedByEntraOid == ownerOid && t.SocialMediaPlatformId == platformId,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            existing.LastNotifiedAt = notifiedAt;
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to update LastNotifiedAt for owner {OwnerOid} and platform {PlatformId}",
                LogSanitizer.Sanitize(ownerOid),
                platformId);
            return false;
        }
    }
}
