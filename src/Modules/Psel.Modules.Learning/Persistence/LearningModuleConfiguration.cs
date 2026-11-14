using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Persistence;

internal sealed class LearningModuleConfiguration : IEntityTypeConfiguration<LearningModule>
{
    public void Configure(EntityTypeBuilder<LearningModule> builder)
    {
        builder.ToTable("LearningModules");

        builder.HasKey(module => module.Id);

        builder.Property(module => module.Slug)
            .IsRequired()
            .HasMaxLength(120);

        builder.HasIndex(module => module.Slug)
            .IsUnique();

        builder.Property(module => module.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(module => module.Summary)
            .IsRequired()
            .HasMaxLength(600);

        builder.Property(module => module.Level)
            .IsRequired()
            .HasMaxLength(40);

        builder.HasIndex(module => new { module.DomainId, module.SortOrder });

        builder.HasMany(module => module.Objectives)
            .WithOne(objective => objective.Module)
            .HasForeignKey(objective => objective.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(module => module.Materials)
            .WithOne(material => material.Module)
            .HasForeignKey(material => material.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(module => module.Competencies)
            .WithOne(competency => competency.Module)
            .HasForeignKey(competency => competency.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(module => module.Prerequisites)
            .WithOne(prerequisite => prerequisite.Module)
            .HasForeignKey(prerequisite => prerequisite.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
