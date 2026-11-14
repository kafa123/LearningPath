using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Assessment.Domain;
using AssessmentEntity = Psel.Modules.Assessment.Domain.Assessment;

namespace Psel.Modules.Assessment.Persistence;

internal sealed class AssessmentConfiguration : IEntityTypeConfiguration<AssessmentEntity>
{
    public void Configure(EntityTypeBuilder<AssessmentEntity> builder)
    {
        builder.ToTable("Assessments");

        builder.HasKey(assessment => assessment.Id);

        builder.Property(assessment => assessment.SubmissionId)
            .IsRequired();

        builder.HasIndex(assessment => assessment.SubmissionId)
            .IsUnique();

        builder.Property(assessment => assessment.ChallengeSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(assessment => assessment.LanguageId)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(assessment => assessment.Purpose)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(assessment => assessment.Outcome)
            .IsRequired()
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.HasMany(assessment => assessment.TestResults)
            .WithOne()
            .HasForeignKey(result => result.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(assessment => assessment.CreatedAtUtc);

        builder.HasIndex(assessment => new
        {
            assessment.ChallengeSlug,
            assessment.CreatedAtUtc
        });
    }
}
