using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Persistence;

internal sealed class LearningDomainConfiguration : IEntityTypeConfiguration<LearningDomain>
{
    public void Configure(EntityTypeBuilder<LearningDomain> builder)
    {
        builder.ToTable("LearningDomains");

        builder.HasKey(domain => domain.Id);

        builder.Property(domain => domain.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(domain => domain.Slug)
            .IsUnique();

        builder.Property(domain => domain.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(domain => domain.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasMany(domain => domain.Modules)
            .WithOne(module => module.Domain)
            .HasForeignKey(module => module.DomainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
