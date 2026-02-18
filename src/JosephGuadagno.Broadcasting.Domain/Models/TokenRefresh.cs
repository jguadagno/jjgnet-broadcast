using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class TokenRefresh
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; }

    [Required]
    public DateTimeOffset Expires { get; set; }

    [Required]
    public DateTimeOffset LastChecked { get; set; }

    [Required]
    public DateTimeOffset LastRefreshed { get; set; }

    [Required]
    public DateTimeOffset LastUpdatedOn { get; set; }
}
