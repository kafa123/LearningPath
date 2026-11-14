using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Assessment.Domain;

namespace Psel.Modules.Assessment.Persistence;

internal sealed class AssessmentTestResultConfiguration : IEntityTypeConfiguration<AssessmentTestResult>
{
    public void Configure(EntityTypeBuilder<AssessmentTestResult> builder)
    {
        builder.ToTable("AssessmentTestResults");

        builder.HasKey(result => result.Id);

        builder.Property(result => result.TestId)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(result => result.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(result => result.Description)
            .HasMaxLength(400);

        builder.Property(result => result.Visibility)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(result => result.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.HasIndex(result => new
        {
            result.AssessmentId,
            result.SortOrder
        });
    }
}
