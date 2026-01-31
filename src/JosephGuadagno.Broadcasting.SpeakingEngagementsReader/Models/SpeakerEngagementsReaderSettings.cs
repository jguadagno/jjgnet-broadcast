using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Models;

/// <summary>
/// The settings for the Speaker Engagements Reader
/// </summary>
public class SpeakerEngagementsReaderSettings: ISpeakerEngagementsReaderSettings
{
    /// <summary>
    /// The fully qualified url to the file containing speaker engagements
    /// </summary>
    public required string SpeakerEngagementsFile { get; set; }
}