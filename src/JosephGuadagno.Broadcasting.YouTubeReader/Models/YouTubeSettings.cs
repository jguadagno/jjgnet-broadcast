using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Models;

public class YouTubeSettings: IYouTubeSettings
{
    public string ApiKey { get; set; } = null!;
    public string ChannelId { get; set; } = null!;
    public string PlaylistId { get; set; } = null!;
    public int ResultSetPageSize { get; set; }
}