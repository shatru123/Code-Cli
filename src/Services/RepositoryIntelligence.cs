using System.Text;
using System.Text.RegularExpressions;

namespace CodeCli.Services;

public sealed class RepositoryScanner
{
    private static readonly string[] IgnoredDirectories =
    [
        ".git", "bin", "obj", "publish", "node_modules", ".idea", ".vs", ".vscode"
    ];

    public RepositorySnapshot Scan(string rootPath, int maxFiles = 8)
    {
        var allFiles = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnored(path, rootPath))
            .Take(500)
            .ToList();

        var interestingFiles = allFiles
            .OrderByDescending(ScoreFile)
            .Take(maxFiles)
            .Select(path => new RepositoryFile(
                path,
                Path.GetRelativePath(rootPath, path),
                new FileInfo(path).Length,
                SafeReadSnippet(path)))
            .ToList();

        var languages = allFiles
            .Select(Path.GetExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .GroupBy(x => x.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();

        return new RepositorySnapshot(
            rootPath,
            allFiles.Count,
            languages,
            interestingFiles);
    }

    private static bool IsIgnored(string path, string rootPath)
    {
        var relative = Path.GetRelativePath(rootPath, path);
        return relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => IgnoredDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }

    private static int ScoreFile(string path)
    {
        var fileName = Path.GetFileName(path).ToLowerInvariant();
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var score = extension switch
        {
            ".cs" => 10,
            ".csproj" => 9,
            ".sln" => 8,
            ".md" => 6,
            ".json" => 5,
            ".yml" or ".yaml" => 4,
            _ => 1
        };

        if (fileName.Contains("program")) score += 5;
        if (fileName.Contains("readme")) score += 4;
        if (fileName.Contains("service")) score += 3;
        return score;
    }

    private static string SafeReadSnippet(string path)
    {
        try
        {
            return File.ReadLines(path).Take(40).Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line)).ToString();
        }
        catch
        {
            return string.Empty;
        }
    }
}

public sealed class CSharpProjectAnalyzer
{
    private static readonly Regex TypeRegex = new(@"\b(class|interface|record|enum)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex UsingRegex = new(@"^\s*using\s+([A-Za-z0-9_.]+);", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ProjectReferenceRegex = new(@"<ProjectReference\s+Include=""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex PackageReferenceRegex = new(@"<PackageReference\s+Include=""([^""]+)""", RegexOptions.Compiled);

    public ProjectAnalysis Analyze(string rootPath)
    {
        var csFiles = Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Take(300)
            .ToList();

        var discoveredTypes = new List<string>();
        var namespaces = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in csFiles)
        {
            string content;
            try { content = File.ReadAllText(file); }
            catch { continue; }

            foreach (Match match in UsingRegex.Matches(content))
                namespaces.Add(match.Groups[1].Value);

            foreach (Match match in TypeRegex.Matches(content))
                discoveredTypes.Add(match.Groups[2].Value);
        }

        var projectFiles = Directory.EnumerateFiles(rootPath, "*.csproj", SearchOption.AllDirectories).ToList();
        var packageReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var projectReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectFile in projectFiles)
        {
            string content;
            try { content = File.ReadAllText(projectFile); }
            catch { continue; }

            foreach (Match match in PackageReferenceRegex.Matches(content))
                packageReferences.Add(match.Groups[1].Value);

            foreach (Match match in ProjectReferenceRegex.Matches(content))
                projectReferences.Add(match.Groups[1].Value);
        }

        return new ProjectAnalysis(
            csFiles.Count,
            discoveredTypes.Distinct(StringComparer.Ordinal).OrderBy(x => x).Take(50).ToList(),
            namespaces.OrderBy(x => x).Take(50).ToList(),
            packageReferences.OrderBy(x => x).ToList(),
            projectReferences.OrderBy(x => x).ToList());
    }
}

public sealed class CodeContextBuilder(RepositoryScanner scanner, CSharpProjectAnalyzer analyzer)
{
    private readonly RepositoryScanner _scanner = scanner;
    private readonly CSharpProjectAnalyzer _analyzer = analyzer;

    public string BuildRepositoryContext(string rootPath, int maxFiles)
    {
        var snapshot = _scanner.Scan(rootPath, maxFiles);
        var analysis = _analyzer.Analyze(rootPath);

        var sb = new StringBuilder();
        sb.AppendLine($"Repository root: {snapshot.RootPath}");
        sb.AppendLine($"Files scanned: {snapshot.FileCount}");
        sb.AppendLine($"Languages: {string.Join(", ", snapshot.Languages)}");
        sb.AppendLine($"C# source files: {analysis.CSharpFileCount}");

        if (analysis.PackageReferences.Count > 0)
            sb.AppendLine($"Packages: {string.Join(", ", analysis.PackageReferences)}");

        if (analysis.ProjectReferences.Count > 0)
            sb.AppendLine($"Project references: {string.Join(", ", analysis.ProjectReferences)}");

        if (analysis.DiscoveredTypes.Count > 0)
            sb.AppendLine($"Key types: {string.Join(", ", analysis.DiscoveredTypes.Take(20))}");

        sb.AppendLine("Important files:");
        foreach (var file in snapshot.ImportantFiles)
        {
            sb.AppendLine($"- {file.RelativePath}");
            if (!string.IsNullOrWhiteSpace(file.Snippet))
            {
                sb.AppendLine("```");
                sb.AppendLine(file.Snippet.TrimEnd());
                sb.AppendLine("```");
            }
        }

        return sb.ToString();
    }
}

public sealed record RepositorySnapshot(
    string RootPath,
    int FileCount,
    IReadOnlyList<string> Languages,
    IReadOnlyList<RepositoryFile> ImportantFiles
);

public sealed record RepositoryFile(
    string FullPath,
    string RelativePath,
    long SizeBytes,
    string Snippet
);

public sealed record ProjectAnalysis(
    int CSharpFileCount,
    IReadOnlyList<string> DiscoveredTypes,
    IReadOnlyList<string> ImportedNamespaces,
    IReadOnlyList<string> PackageReferences,
    IReadOnlyList<string> ProjectReferences
);
