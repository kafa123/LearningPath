using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Progress.Domain;

namespace Psel.Modules.Progress.Persistence;

internal sealed class ExaminationSessionConfiguration : IEntityTypeConfiguration<ExaminationSession>
{
    public void Configure(EntityTypeBuilder<ExaminationSession> builder)
    {
        builder.ToTable("ExaminationSessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.ChallengeSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(session => session.VariantId)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(session => session.VariantLabel)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(session => session.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.HasMany(session => session.Attempts)
            .WithOne(attempt => attempt.Examination)
            .HasForeignKey(attempt => attempt.ExaminationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(session => session.StartedAtUtc);
        builder.HasIndex(session => new { session.ChallengeSlug, session.StartedAtUtc });
    }
}
