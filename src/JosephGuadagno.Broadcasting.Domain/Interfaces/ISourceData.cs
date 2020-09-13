using System;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces
{
    public interface ISourceData
    {
        public string SourceSystem { get; }
        public string Id { get; }
        public DateTimeOffset AddedOn { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        //public string Text { get; set; }
        public string Url { get; set; }
        public string ShortenedUrl { get; set; }
        public DateTimeOffset PublicationDate { get; set; }
        public DateTimeOffset? EndAfter { get; set; }
    }
}