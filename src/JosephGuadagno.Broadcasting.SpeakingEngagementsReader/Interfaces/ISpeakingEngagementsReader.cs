using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

public interface ISpeakingEngagementsReader
{
    /// <summary>
    /// Gets all engagements since a given date
    /// </summary>
    /// <param name="sinceWhen">A date to filter engagements since</param>
    /// <returns>A list of engagements</returns>
    public Task<List<Engagement>> GetAll(DateTimeOffset sinceWhen);

    /// <summary>
    /// Gets all engagements
    /// </summary>
    /// <returns>A list of engagements</returns>
    public Task<List<Engagement>> GetAll();

    /// <summary>Gets all engagements from a specific file URL since a given date</summary>
    /// <param name="fileUrl">The URL of the speaking engagements JSON file</param>
    /// <param name="sinceWhen">A date to filter engagements since</param>
    /// <returns>A list of engagements</returns>
    public Task<List<Engagement>> GetAll(string fileUrl, DateTimeOffset sinceWhen);

    /// <summary>Gets all engagements from a specific file URL</summary>
    /// <param name="fileUrl">The URL of the speaking engagements JSON file</param>
    /// <returns>A list of engagements</returns>
    public Task<List<Engagement>> GetAll(string fileUrl);

}