using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RatingService.Core.Entities;

namespace RatingService.Infrastructure.Context;

public class RatingDbContext : DbContext
{
    public RatingDbContext()
    {
        
    }

    public RatingDbContext(DbContextOptions<RatingDbContext> options) : base(options)
    {
    }
    
    public virtual DbSet<ProductQuestion> ProductQuestion { get; set; }
    public virtual DbSet<ProductAnswer> ProductAnswer { get; set; }
    
    public virtual DbSet<ProductReview> ProductReview { get; set; }
    public virtual DbSet<ProductReviewAggregate> ProductReviewAggregate { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RatingDbContext).Assembly);
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
      
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}