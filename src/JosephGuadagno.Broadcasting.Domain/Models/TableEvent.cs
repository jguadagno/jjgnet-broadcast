namespace JosephGuadagno.Broadcasting.Domain.Models
{
    public class TableEvent
    {
        public string TableName { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }
}