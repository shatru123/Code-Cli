namespace CodeCli.Core.Repository;

public class RepositoryScanner
{
    public RepositorySummary Scan(string rootPath)
    {
        var summary = new RepositorySummary
        {
            RootPath = rootPath
        };

        summary.SolutionFiles.AddRange(
            Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories));

        summary.ProjectFiles.AddRange(
            Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories));

        summary.CSharpFiles.AddRange(
            Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj")));

        return summary;
    }
}

public class RepositorySummary
{
    public string RootPath { get; set; } = string.Empty;

    public List<string> SolutionFiles { get; set; } = [];

    public List<string> ProjectFiles { get; set; } = [];

    public List<string> CSharpFiles { get; set; } = [];
}
