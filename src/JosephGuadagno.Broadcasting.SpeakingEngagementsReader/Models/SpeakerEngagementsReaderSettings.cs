using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Models;

/// <summary>
/// The settings for the Speaking Engagements Reader
/// </summary>
public class SpeakingEngagementsReaderSettings: ISpeakingEngagementsReaderSettings
{
    /// <summary>
    /// The fully qualified url to the file containing speaking engagements
    /// </summary>
    public required string SpeakingEngagementsFile { get; set; }
}