using System.Reflection;

namespace Psel.Infrastructure.Persistence;

/// <summary>
/// Lists the module assemblies whose EF Core configurations participate in the
/// shared application model. The API host supplies this list during composition,
/// keeping infrastructure independent of individual modules.
/// </summary>
public sealed class ModuleAssemblyRegistry
{
    public ModuleAssemblyRegistry(IEnumerable<Assembly>? assemblies = null)
    {
        Assemblies = (assemblies ?? [])
            .Distinct()
            .ToArray();
    }

    public IReadOnlyList<Assembly> Assemblies { get; }
}
