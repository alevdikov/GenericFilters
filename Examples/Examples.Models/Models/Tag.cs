using System.ComponentModel.DataAnnotations;

namespace Examples.Models.Models;

public class Tag
{
    [Key]
    public int TagId { get; init; }

    [Required]
    public string Name { get; init; }

    public int ProductId { get; init; }
    public Product Product { get; init; }
}
