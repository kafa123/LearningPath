using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Submissions.Domain;

namespace Psel.Modules.Submissions.Persistence;

internal sealed class RubricReviewConfiguration : IEntityTypeConfiguration<RubricReview>
{
    public void Configure(EntityTypeBuilder<RubricReview> builder)
    {
        builder.ToTable("RubricReviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.SubmissionId)
            .IsRequired();

        builder.HasIndex(review => review.SubmissionId)
            .IsUnique();

        builder.Property(review => review.ChallengeSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(review => review.Band)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(review => review.Note)
            .HasMaxLength(2000);

        builder.HasOne<Submission>()
            .WithOne()
            .HasForeignKey<RubricReview>(review => review.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(review => review.CriterionScores)
            .WithOne()
            .HasForeignKey(score => score.RubricReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(review => review.UpdatedAtUtc);
    }
}

internal sealed class RubricCriterionScoreConfiguration
    : IEntityTypeConfiguration<RubricCriterionScore>
{
    public void Configure(EntityTypeBuilder<RubricCriterionScore> builder)
    {
        builder.ToTable("RubricCriterionScores");

        builder.HasKey(score => score.Id);

        builder.Property(score => score.CriterionId)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(score => score.Title)
            .IsRequired()
            .HasMaxLength(120);

        builder.HasIndex(score => new
        {
            score.RubricReviewId,
            score.SortOrder
        });
    }
}
