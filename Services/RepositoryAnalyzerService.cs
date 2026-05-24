namespace CodeCli.Services;

public sealed class RepositoryAnalyzerService
{
    public RepositoryAnalysis Analyze(string rootPath)
    {
        var solutions = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);
        var projects = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);
        var csharpFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

        return new RepositoryAnalysis
        {
            RootPath = Path.GetFullPath(rootPath),
            SolutionCount = solutions.Length,
            ProjectCount = projects.Length,
            CSharpFileCount = csharpFiles.Length,
            Projects = projects.Select(Path.GetFileName).ToList()
        };
    }
}

public sealed class RepositoryAnalysis
{
    public string RootPath { get; set; } = string.Empty;

    public int SolutionCount { get; set; }

    public int ProjectCount { get; set; }

    public int CSharpFileCount { get; set; }

    public List<string> Projects { get; set; } = [];
}
