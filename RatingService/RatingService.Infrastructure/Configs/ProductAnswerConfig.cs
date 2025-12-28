using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingService.Core.Entities;

namespace RatingService.Infrastructure.Configs;

public class ProductAnswerConfig : IEntityTypeConfiguration<ProductAnswer>
{
    public void Configure(EntityTypeBuilder<ProductAnswer> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("AnswerId")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.QuestionId)
            .HasColumnName("QuestionId")
            .IsRequired();
        
        builder.Property(x => x.ShopId)
            .HasColumnName("ShopId")
            .IsRequired();
        
        builder.Property(x => x.Text)
            .HasMaxLength(2000)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.HasIndex(x => x.QuestionId)
            .HasDatabaseName("ix_product_answers_question_id");
    }
}