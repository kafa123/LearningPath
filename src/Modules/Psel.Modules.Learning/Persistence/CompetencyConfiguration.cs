using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Persistence;

internal sealed class CompetencyConfiguration : IEntityTypeConfiguration<Competency>
{
    public void Configure(EntityTypeBuilder<Competency> builder)
    {
        builder.ToTable("Competencies");

        builder.HasKey(competency => competency.Id);

        builder.Property(competency => competency.Slug)
            .IsRequired()
            .HasMaxLength(140);

        builder.HasIndex(competency => competency.Slug)
            .IsUnique();

        builder.Property(competency => competency.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(competency => competency.Description)
            .IsRequired()
            .HasMaxLength(800);

        builder.HasIndex(competency => new { competency.ModuleId, competency.SortOrder });
    }
}
