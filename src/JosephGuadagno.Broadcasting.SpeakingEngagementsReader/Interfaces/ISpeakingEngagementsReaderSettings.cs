namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

/// <summary>
/// The settings for the Speaking Engagements Reader
/// </summary>
public interface ISpeakingEngagementsReaderSettings
{
    /// <summary>
    /// The fully qualified url to the file containing speaking engagements
    /// </summary>
    public string SpeakingEngagementsFile { get; set; }
}