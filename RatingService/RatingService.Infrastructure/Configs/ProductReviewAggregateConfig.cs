using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingService.Core.Entities;

namespace RatingService.Infrastructure.Configs;

public class ProductReviewAggregateConfig : IEntityTypeConfiguration<ProductReviewAggregate>
{
    public void Configure(EntityTypeBuilder<ProductReviewAggregate> builder)
    {
        builder.HasKey(x => x.ProductId);

        builder.Property(x => x.ReviewCount)
            .IsRequired();
        
        builder.Property(x => x.AverageRating)
            .HasColumnType("double precision")
            .IsRequired();
        
        builder.Property(x => x.BayesianRating)
            .HasColumnType("double precision")
            .IsRequired();
    }
}