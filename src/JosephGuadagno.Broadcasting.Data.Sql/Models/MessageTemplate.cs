#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

[ExcludeFromCodeCoverage]
public class MessageTemplate
{
    public string Platform { get; set; }
    public string MessageType { get; set; }
    public string Template { get; set; }
    public string Description { get; set; }
}
