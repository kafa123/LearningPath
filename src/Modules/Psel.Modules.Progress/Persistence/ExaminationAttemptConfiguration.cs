using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Progress.Domain;

namespace Psel.Modules.Progress.Persistence;

internal sealed class ExaminationAttemptConfiguration : IEntityTypeConfiguration<ExaminationAttempt>
{
    public void Configure(EntityTypeBuilder<ExaminationAttempt> builder)
    {
        builder.ToTable("ExaminationAttempts");

        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.SubmissionId)
            .IsRequired();

        builder.HasIndex(attempt => attempt.SubmissionId)
            .IsUnique();

        builder.HasIndex(attempt => attempt.CreatedAtUtc);
    }
}
