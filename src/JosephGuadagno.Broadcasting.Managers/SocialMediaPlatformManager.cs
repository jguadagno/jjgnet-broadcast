using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class SocialMediaPlatformManager : ISocialMediaPlatformManager
{
    private readonly ISocialMediaPlatformDataStore _dataStore;

    public SocialMediaPlatformManager(ISocialMediaPlatformDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<List<SocialMediaPlatform>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        return await _dataStore.GetAllAsync(includeInactive: includeInactive, cancellationToken);
    }

    public async Task<List<SocialMediaPlatform>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dataStore.GetAllAsync(includeInactive: true, cancellationToken);
    }

    public async Task<SocialMediaPlatform?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dataStore.GetAsync(id, cancellationToken);
    }

    public async Task<SocialMediaPlatform?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dataStore.GetByNameAsync(name, cancellationToken);
    }

    public async Task<SocialMediaPlatform?> AddAsync(SocialMediaPlatform platform, CancellationToken cancellationToken = default)
    {
        return await _dataStore.AddAsync(platform, cancellationToken);
    }

    public async Task<SocialMediaPlatform?> UpdateAsync(SocialMediaPlatform platform, CancellationToken cancellationToken = default)
    {
        return await _dataStore.UpdateAsync(platform, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dataStore.DeleteAsync(id, cancellationToken);
    }
}

