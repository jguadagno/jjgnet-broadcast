#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Extensions.Types;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class SourceDataRepository: TableRepository<SourceData>
{
    public SourceDataRepository(string connectionString) : base(connectionString, Constants.Tables.SourceData)
    {
            
    }

    /// <summary>
    /// Returns a random <see cref="SourceData" /> from the date specified
    /// </summary>
    /// <param name="sinceDate">The date to search from</param>
    /// <param name="excludeCategories">A list of categories to exclude</param>
    /// <param name="sourceSystem">The <see cref="SourceSystems"/> type to search.</param>
    /// <returns>A random SourceData</returns>
    public async Task<SourceData?> GetRandomSourceDataAsync(DateTime sinceDate, List<string> excludeCategories, string sourceSystem = SourceSystems.SyndicationFeed)
    {
        var items = (await GetAllAsync(sourceSystem)).Where(s =>
                s.PublicationDate >= sinceDate || s.UpdatedOnDate >= sinceDate).ToList();
            
        if (items.Count == 0)
        {
            return null;
        }
        
        List<SourceData> filteredList = [];

        if (excludeCategories.Count != 0)
        {
            foreach (var source in items)
            {
                if (source.Tags.IsNullOrEmpty())
                {
                    filteredList.Add(source);
                    continue;
                }

                var containsCategory = excludeCategories.Any(item =>
                    source.Tags.ToLowerInvariant().Contains(item.ToLowerInvariant()));
                if (!containsCategory)
                {
                    filteredList.Add(source);
                }
            }
        }
        else
        {
            filteredList = items;
        }

        var random = new Random();
        var randomNumber = random.Next(0, filteredList.Count);
        return filteredList[randomNumber];
    }
}