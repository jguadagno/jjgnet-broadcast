using System;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    public class ScheduledSocialPost : TableEntity
    {
        protected ScheduledSocialPost()
        {
            RowKey = Guid.NewGuid().ToString();
        }
        
        public string Post { get; set; }
        public DateTime ScheduledFor { get; set; }
        public DateTime SentAt { get; set; }
        public bool WasSend { get; set; }
    }
}