using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Persistence;

internal sealed class LearningModulePrerequisiteConfiguration
    : IEntityTypeConfiguration<LearningModulePrerequisite>
{
    public void Configure(EntityTypeBuilder<LearningModulePrerequisite> builder)
    {
        builder.ToTable("LearningModulePrerequisites");

        builder.HasKey(prerequisite => new
        {
            prerequisite.ModuleId,
            prerequisite.PrerequisiteModuleId
        });

        builder.HasOne(prerequisite => prerequisite.PrerequisiteModule)
            .WithMany()
            .HasForeignKey(prerequisite => prerequisite.PrerequisiteModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
