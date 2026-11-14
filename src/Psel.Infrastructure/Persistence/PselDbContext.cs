using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Psel.Infrastructure.Persistence;

/// <summary>
/// Application persistence context. Module entities are configured by their own
/// assemblies; the host supplies those assemblies through the registry.
/// </summary>
public sealed class PselDbContext(
    DbContextOptions<PselDbContext> options,
    ModuleAssemblyRegistry moduleAssemblyRegistry) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyConfigurations(modelBuilder, typeof(PselDbContext).Assembly);

        foreach (var assembly in moduleAssemblyRegistry.Assemblies)
        {
            ApplyConfigurations(modelBuilder, assembly);
        }
    }

    private static void ApplyConfigurations(ModelBuilder modelBuilder, Assembly assembly)
    {
        var hasConfigurations = assembly.GetTypes().Any(type =>
            type is { IsAbstract: false, IsInterface: false }
            && type.GetInterfaces().Any(contract =>
                contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

        if (hasConfigurations)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}
