using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ISourceReader
{
    public List<SourceData> GetSinceDate(DateTime sinceWhen);
    public Task<List<SourceData>> GetAsync(DateTime sinceWhen);
}