using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

public interface ISpeakerEngagementsReader
{
    /// <summary>
    /// Gets all engagements since a given date
    /// </summary>
    /// <param name="sinceWhen">A date to filter engagements since</param>
    /// <returns>A list of engagements</returns>
    public Task<List<Engagement>> GetSinceDate(DateTime sinceWhen);

    /// <summary>
    /// Gets all engagements
    /// </summary>
    /// <returns>A list of engagements</returns>
    public Task<List<Engagement>> GetAll();

}