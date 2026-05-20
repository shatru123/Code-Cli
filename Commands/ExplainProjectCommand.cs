using CodeCli.Core.Repository;
using CodeCli.UI;

namespace CodeCli.Commands;

public class ExplainProjectCommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        var currentDirectory = Directory.GetCurrentDirectory();

        var scanner = new RepositoryScanner();

        var summary = scanner.Scan(currentDirectory);

        ConsoleUI.SectionHeader("PROJECT ANALYSIS");

        Console.WriteLine();

        Console.WriteLine($"Root Path      : {summary.RootPath}");
        Console.WriteLine($"Solutions      : {summary.SolutionFiles.Count}");
        Console.WriteLine($"Projects       : {summary.ProjectFiles.Count}");
        Console.WriteLine($"C# Files       : {summary.CSharpFiles.Count}");

        Console.WriteLine();

        if (summary.ProjectFiles.Count > 0)
        {
            ConsoleUI.Success("Detected Projects:");

            foreach (var project in summary.ProjectFiles)
            {
                Console.WriteLine($"  • {Path.GetFileName(project)}");
            }
        }

        Console.WriteLine();

        ConsoleUI.Info("Repository intelligence is now enabled.");
        ConsoleUI.Info("Next upgrade: semantic search + Roslyn architecture analysis.");
    }
}
