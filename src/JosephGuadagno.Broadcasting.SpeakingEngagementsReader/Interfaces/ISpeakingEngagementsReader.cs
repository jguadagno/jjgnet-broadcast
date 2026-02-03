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
    public Task<List<Engagement>> GetAll(DateTime sinceWhen);

    /// <summary>
    /// Gets all engagements
    /// </summary>
    /// <returns>A list of engagements</returns>
    public Task<List<Engagement>> GetAll();

}