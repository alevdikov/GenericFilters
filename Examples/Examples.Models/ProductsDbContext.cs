using Examples.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Examples.Models;

public class ProductsDbContext : DbContext
{
    public DbSet<Category> Categories { get; init; }
    public DbSet<Product> Products { get; init; }
    public DbSet<Tag> Tags { get; init; }

    private static readonly ILoggerFactory logger = LoggerFactory.Create(builder => { builder.AddConsole(); });

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => 
        optionsBuilder
            .UseLoggerFactory(logger)
            .UseSqlite("Data Source=:memory:");
        
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Tag>()
            .HasOne(t => t.Product)
            .WithMany(p => p.Tags)
            .HasForeignKey(t => t.ProductId);

    public void SeedProducts()
    {
        Database.EnsureCreated();
        
        string sqlFilePath = "products_seed.sql";
        if (!File.Exists(sqlFilePath))
            throw new Exception("products_seed.sql file not found.");
        
        var sqlScript = File.ReadAllLines(sqlFilePath)
            .Where(i => i.Length > 0 && !i.StartsWith("--"))
            .ToList();
        foreach (var sql in sqlScript)
        {
            Database.ExecuteSqlRaw(sql);
        }
        
        Console.WriteLine($"Inserted {sqlScript.Count()} rows");
    }
}
