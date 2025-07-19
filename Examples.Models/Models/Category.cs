using System.ComponentModel.DataAnnotations;

namespace Examples.Models.Models;

public class Category
{
    [Key]
    public int CategoryId { get; init; }
    
    [Required]
    public string Name { get; init; }
    public List<Product> Products { get; init; }
}
