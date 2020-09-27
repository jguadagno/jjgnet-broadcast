using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Models
{
    public class YouTubeSettings: IYouTubeSettings
    {
        public string ApiKey { get; set; }
        public string ChannelId { get; set; }
        public string PlaylistId { get; set; }
        public int ResultSetPageSize { get; set; }
    }
}