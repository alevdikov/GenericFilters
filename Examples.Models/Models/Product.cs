using System.ComponentModel.DataAnnotations;

namespace Examples.Models.Models;

public class Product
{
    [Key]
    public int ProductId { get; init; }
    
    [Required]
    public int CategoryId { get; init; }
    
    [Required]
    public string Name { get; init; }
    
    public string Description { get; init; }
    
    [Required]
    public double UnitPrice { get; init; }
    
    [Required]
    public int StockQuantity { get; init; }

    [Required]
    public DateTime DateAdded { get; init; }
    public DateTime? LastUpdated { get; init; }

    public Category Category { get; init; }
    public List<Tag> Tags { get; init; } = new();
}
