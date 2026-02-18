using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class FeedCheck
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; }

    [Required]
    public DateTimeOffset LastCheckedFeed { get; set; }

    [Required]
    public DateTimeOffset LastItemAddedOrUpdated { get; set; }

    [Required]
    public DateTimeOffset LastUpdatedOn { get; set; }
}
