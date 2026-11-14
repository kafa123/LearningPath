namespace Psel.Modules.Execution.Runners;

/// <summary>
/// Creates and cleans the per-attempt workspace shared by language runners.
/// The server writes every file into a fresh directory that is mounted
/// read-only into the container.
/// </summary>
internal static class ExecutionWorkspace
{
    public static string Create(ExecutionOptions settings)
    {
        var root = string.IsNullOrWhiteSpace(settings.WorkspaceRoot)
            ? Path.Combine(Path.GetTempPath(), "psel-execution")
            : settings.WorkspaceRoot;

        Directory.CreateDirectory(root);

        var workspace = Path.Combine(
            root,
            $"submission-{Guid.NewGuid():N}");

        Directory.CreateDirectory(workspace);

        return workspace;
    }

    public static void WriteFiles(
        string workspace,
        IReadOnlyList<ExecutionFile> files)
    {
        foreach (var file in files)
        {
            ExecutionFileRules.IsSafeRelativePath(file.Path, out var relativePath);

            var fullPath = Path.Combine(
                workspace,
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, file.Content);
        }
    }

    public static void TryDelete(string workspace)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(workspace))
                {
                    Directory.Delete(workspace, recursive: true);
                }

                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}
