using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Persistence;

internal sealed class LearningObjectiveConfiguration : IEntityTypeConfiguration<LearningObjective>
{
    public void Configure(EntityTypeBuilder<LearningObjective> builder)
    {
        builder.ToTable("LearningObjectives");

        builder.HasKey(objective => objective.Id);

        builder.Property(objective => objective.Text)
            .IsRequired()
            .HasMaxLength(400);

        builder.HasIndex(objective => new { objective.ModuleId, objective.SortOrder });
    }
}
