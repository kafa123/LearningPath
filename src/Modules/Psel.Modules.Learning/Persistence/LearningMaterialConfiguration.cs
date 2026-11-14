using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Persistence;

internal sealed class LearningMaterialConfiguration : IEntityTypeConfiguration<LearningMaterial>
{
    public void Configure(EntityTypeBuilder<LearningMaterial> builder)
    {
        builder.ToTable("LearningMaterials");

        builder.HasKey(material => material.Id);

        builder.Property(material => material.Slug)
            .IsRequired()
            .HasMaxLength(140);

        builder.HasIndex(material => material.Slug)
            .IsUnique();

        builder.Property(material => material.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(material => material.Kind)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(material => material.BodyMarkdown)
            .IsRequired();

        builder.HasIndex(material => new { material.ModuleId, material.SortOrder });
    }
}
