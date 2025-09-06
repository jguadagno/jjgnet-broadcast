namespace JosephGuadagno.Broadcasting.Domain.Models;

public class TableEvent
{
    public string TableName { get; init; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
}