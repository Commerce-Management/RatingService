using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingService.Core.Entities;

namespace RatingService.Infrastructure.Configs;

public class ProductQuestionConfig : IEntityTypeConfiguration<ProductQuestion>
{
    public void Configure(EntityTypeBuilder<ProductQuestion> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .HasColumnName("QuestionId");

        builder.Property(x => x.ProductId)
            .HasColumnName("ProductId")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(x => x.Text)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.IsAnonymous)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.HasMany(x => x.Answers)
            .WithOne()                        
            .HasForeignKey(a => a.QuestionId)
            .HasConstraintName("fk_product_answers_question_id")
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("ix_product_questions_product_id");
    }
}