namespace JosephGuadagno.Broadcasting.YouTubeReader.Interfaces
{
    public interface IYouTubeSettings
    {
        /// <summary>
        /// The Google Api Key
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// The Id of the Channel to use
        /// </summary>
        public string ChannelId { get; set; }
        /// <summary>
        /// The playlist id to use
        /// </summary>
        public string PlaylistId { get; set; }
        /// <summary>
        /// The page size of the search results call 
        /// </summary>
        public int ResultSetPageSize { get; set; }
    }
}