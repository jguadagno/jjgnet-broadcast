using System;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    public class SourceData : TableEntity, ISourceData
    {
        public SourceData() {}
        public SourceData(string sourceSystem): base(sourceSystem, Guid.NewGuid().ToString())
        {
            
        }
        
        public SourceData(string sourceSystem, string id)
        {
            if (string.IsNullOrEmpty(sourceSystem))
            {
                throw new ArgumentNullException(nameof(sourceSystem), "The source system cannot be null or empty.");
            }
            PartitionKey = sourceSystem;
            RowKey = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
        }

        public string SourceSystem => PartitionKey;
        public string Id => RowKey;
        
        public DateTimeOffset AddedOn { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        // Text was commented out due to a size limitation of Azure Table Storage
        //     this will be revisited if we determine text is needed.
        //public string Text { get; set; }
        public string Url { get; set; }
        public string ShortenedUrl { get; set; }
        public DateTimeOffset PublicationDate { get; set; }
        public DateTimeOffset? EndAfter { get; set; }
    }
}