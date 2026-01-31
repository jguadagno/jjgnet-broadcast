namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

/// <summary>
/// The settings for the Speaker Engagements Reader
/// </summary>
public interface ISpeakerEngagementsReaderSettings
{
    /// <summary>
    /// The fully qualified url to the file containing speaker engagements
    /// </summary>
    public string SpeakerEngagementsFile { get; set; }
}