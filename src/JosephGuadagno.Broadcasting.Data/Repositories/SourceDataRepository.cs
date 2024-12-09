#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class SourceDataRepository: TableRepository<SourceData>
{
    public SourceDataRepository(string connectionString) : base(connectionString, Domain.Constants.Tables.SourceData)
    {
            
    }

    /// <summary>
    /// Returns a random <see cref="SourceData" /> from the date specified
    /// </summary>
    /// <param name="sinceDate">The date to search from</param>
    /// <param name="sourceSystem">The <see cref="SourceSystems"/> type to search.</param>
    /// <returns>A random SourceData</returns>
    public async Task<SourceData?> GetRandomSourceDataAsync(DateTime sinceDate, string sourceSystem = SourceSystems.SyndicationFeed)
    {
        var items = (await GetAllAsync(sourceSystem)).Where(s =>
            s.PublicationDate >= sinceDate || s.UpdatedOnDate >= sinceDate).ToList();

        if (items.Count == 0)
        {
            return null;
        }
        var random = new Random();
        var randomNumber = random.Next(0, items.Count);
        return items[randomNumber];
    }
}