using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingService.Core.Entities;

namespace RatingService.Infrastructure.Configs;

public class ProductReviewConfig : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("ReviewId");

        builder.Property(e => e.ProductId)
            .HasColumnName("ProductId")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(e => e.Rating)
            .HasColumnName("Rating")
            .IsRequired();

        builder.Property(e => e.Title)
            .HasColumnName("Title")
            .HasMaxLength(200);

        builder.Property(e => e.Text)
            .HasColumnName("Text")
            .HasMaxLength(4000);
        
        builder.Property(e => e.ImageUrls)
            .IsRequired()
            .HasColumnType("text[]");
        
        builder.Property(e => e.IsAnonymous)
            .HasColumnName("is_anonymous")
            .IsRequired();
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("UpdatedAt");
    }
}