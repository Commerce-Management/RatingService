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
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.AverageRating)
            .HasColumnType("double precision")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.BayesianRating)
            .HasColumnType("double precision")
            .IsRequired()
            .HasDefaultValue(0);


    }
}