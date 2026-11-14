using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Submissions.Domain;

namespace Psel.Modules.Submissions.Persistence;

internal sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions");

        builder.HasKey(submission => submission.Id);

        builder.Property(submission => submission.ChallengeSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(submission => submission.LanguageId)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(submission => submission.Purpose)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(submission => submission.Status)
            .IsRequired()
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(submission => submission.SourceFilesJson)
            .IsRequired();

        builder.Property(submission => submission.StandardOutput)
            .IsRequired();

        builder.Property(submission => submission.StandardError)
            .IsRequired();

        builder.Property(submission => submission.FailureMessage)
            .HasMaxLength(1000);

        builder.HasIndex(submission => submission.CreatedAtUtc);

        builder.HasIndex(submission => new
        {
            submission.ChallengeSlug,
            submission.LanguageId
        });
    }
}
