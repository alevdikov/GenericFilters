using System.ComponentModel.DataAnnotations;

namespace Examples.Models.Models;

public class Tag
{
    [Key]
    public int TagId { get; set; }

    [Required]
    public string Name { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }
}
