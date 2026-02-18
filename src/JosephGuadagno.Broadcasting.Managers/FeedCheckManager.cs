using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class FeedCheckManager : IFeedCheckManager
{
    private readonly IFeedCheckRepository _feedCheckRepository;

    public FeedCheckManager(IFeedCheckRepository feedCheckRepository)
    {
        _feedCheckRepository = feedCheckRepository;
    }

    public async Task<FeedCheck> GetAsync(int primaryKey)
    {
        return await _feedCheckRepository.GetAsync(primaryKey);
    }

    public async Task<FeedCheck> SaveAsync(FeedCheck entity)
    {
        return await _feedCheckRepository.SaveAsync(entity);
    }

    public async Task<List<FeedCheck>> GetAllAsync()
    {
        return await _feedCheckRepository.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(FeedCheck entity)
    {
        return await _feedCheckRepository.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _feedCheckRepository.DeleteAsync(primaryKey);
    }

    public async Task<FeedCheck?> GetByNameAsync(string name)
    {
        return await _feedCheckRepository.GetByNameAsync(name);
    }
}