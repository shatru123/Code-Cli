using System.Text;

namespace CodeCli.Services;

/// <summary>
/// Builds rich context strings from a codebase for AI analysis.
/// Handles large repositories by:
///   • Skipping non-code artifacts (bin/obj/node_modules/etc.)
///   • Enforcing per-file and total-context size budgets
///   • Prioritising files matching an optional focus pattern
///   • Prepending a compact file-tree summary
/// </summary>
public static class ProjectContextBuilder
{
    // ── Settings ──────────────────────────────────────────────────────────────

    private static readonly HashSet<string> IgnoredDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", ".git", ".vs", ".vscode", ".idea",
        "dist", "build", "out", "publish", "release", "debug",
        "packages", ".nuget", "__pycache__", ".pytest_cache",
        "venv", ".venv", ".env", "coverage", ".terraform", ".aws-sam"
    };

    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".go", ".rs", ".java",
        ".kt", ".swift", ".cpp", ".c", ".h", ".hpp", ".fs", ".fsx",
        ".rb", ".php", ".scala", ".clj", ".ex", ".exs",
        ".sql", ".json", ".yaml", ".yml", ".toml", ".xml",
        ".csproj", ".fsproj", ".sln", ".props", ".targets",
        ".md", ".sh", ".bash", ".bat", ".ps1",
        "Dockerfile", ".dockerignore", ".tf", ".bicep"
    };

    private const int MaxFileChars    = 60_000;
    private const int MaxContextChars = 120_000;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Build a full project context block, optionally biased toward a focus pattern.
    /// </summary>
    public static string BuildProjectContext(string rootPath, string? focusPattern = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Project Structure");
        sb.AppendLine("```");
        sb.Append(BuildFileTree(rootPath, maxDepth: 4));
        sb.AppendLine("```");
        sb.AppendLine();

        var files     = GatherFiles(rootPath, focusPattern);
        var usedChars = sb.Length;

        sb.AppendLine("## Source Files");
        sb.AppendLine();

        for (var i = 0; i < files.Count; i++)
        {
            var content = ReadSafe(files[i]);
            if (content is null) continue;

            var relPath = Path.GetRelativePath(rootPath, files[i]);

            if (content.Length > MaxFileChars)
                content = content[..MaxFileChars]
                        + $"\n\n// ... [{content.Length - MaxFileChars:N0} chars truncated] ...";

            var lang  = LangHint(files[i]);
            var block = $"### `{relPath}`\n```{lang}\n{content}\n```\n\n";

            if (usedChars + block.Length > MaxContextChars)
            {
                sb.AppendLine($"> *{files.Count - i} more file(s) omitted — context limit reached.*");
                break;
            }

            sb.Append(block);
            usedChars += block.Length;
        }

        return sb.ToString();
    }

    /// <summary>Build a context block for a single file.</summary>
    public static string BuildFileContext(string filePath)
    {
        var content = ReadSafe(filePath) ?? "[unreadable]";
        if (content.Length > MaxFileChars)
            content = content[..MaxFileChars] + "\n// ... truncated ...";

        return $"### `{Path.GetFileName(filePath)}`\n```{LangHint(filePath)}\n{content}\n```";
    }

    /// <summary>ASCII file tree with no file content.</summary>
    public static string BuildFileTree(string rootPath, int maxDepth = 4)
    {
        var sb = new StringBuilder();
        RenderTree(sb, rootPath, prefix: "", depth: 0, maxDepth);
        return sb.ToString();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private static List<string> GatherFiles(string root, string? focus)
    {
        var all = new List<string>();
        CollectRecursive(root, all);

        if (string.IsNullOrWhiteSpace(focus)) return all;

        var focused = all.Where(f => f.Contains(focus, StringComparison.OrdinalIgnoreCase)).ToList();
        var rest    = all.Except(focused).ToList();
        return [.. focused, .. rest];
    }

    private static void CollectRecursive(string dir, List<string> result)
    {
        try
        {
            foreach (var sub in Directory.GetDirectories(dir).OrderBy(d => d))
            {
                if (IgnoredDirs.Contains(Path.GetFileName(sub))) continue;
                CollectRecursive(sub, result);
            }
            foreach (var file in Directory.GetFiles(dir).OrderBy(f => f))
            {
                var ext = Path.GetExtension(file);
                if (CodeExtensions.Contains(ext) || CodeExtensions.Contains(Path.GetFileName(file)))
                    result.Add(file);
            }
        }
        catch { /* permission denied */ }
    }

    private static void RenderTree(StringBuilder sb, string path, string prefix, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;

        if (depth == 0)
            sb.AppendLine($"{Path.GetFileName(path)}/");

        var childPrefix = depth == 0 ? "" : prefix + "│   ";

        try
        {
            var dirs  = Directory.GetDirectories(path)
                .Where(d => !IgnoredDirs.Contains(Path.GetFileName(d)))
                .OrderBy(d => d).ToArray();
            var files = Directory.GetFiles(path)
                .Where(f => CodeExtensions.Contains(Path.GetExtension(f))
                         || CodeExtensions.Contains(Path.GetFileName(f)))
                .OrderBy(f => f).ToArray();

            for (var i = 0; i < dirs.Length; i++)
            {
                var last      = i == dirs.Length - 1 && files.Length == 0;
                var connector = last ? "└── " : "├── ";
                sb.AppendLine($"{childPrefix}{connector}{Path.GetFileName(dirs[i])}/");
                RenderTree(sb, dirs[i], childPrefix + (last ? "    " : "│   "), depth + 1, maxDepth);
            }

            for (var i = 0; i < files.Length; i++)
                sb.AppendLine($"{childPrefix}{(i == files.Length - 1 ? "└── " : "├── ")}{Path.GetFileName(files[i])}");
        }
        catch { }
    }

    private static string? ReadSafe(string path)
    {
        try   { return File.ReadAllText(path); }
        catch { return null; }
    }

    private static string LangHint(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".cs"              => "csharp",
            ".ts" or ".tsx"   => "typescript",
            ".js" or ".jsx"   => "javascript",
            ".py"             => "python",
            ".go"             => "go",
            ".rs"             => "rust",
            ".java"           => "java",
            ".kt"             => "kotlin",
            ".rb"             => "ruby",
            ".cpp" or ".cc"   => "cpp",
            ".c"              => "c",
            ".fs" or ".fsx"   => "fsharp",
            ".sql"            => "sql",
            ".json"           => "json",
            ".yaml" or ".yml" => "yaml",
            ".toml"           => "toml",
            ".xml"            => "xml",
            ".sh" or ".bash"  => "bash",
            ".ps1"            => "powershell",
            ".tf"             => "hcl",
            ".md"             => "markdown",
            _                 => ""
        };
}
