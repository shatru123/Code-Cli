using System.Diagnostics;

namespace CodeCli.Services;

public interface ITool
{
    string Name { get; }

    string Description { get; }

    Task<ToolResult> ExecuteAsync(string input, CancellationToken ct = default);
}

public sealed class TerminalTool : ITool
{
    public string Name => "terminal";

    public string Description => "Execute shell commands for diagnostics and validation.";

    public async Task<ToolResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var shell = OperatingSystem.IsWindows() ? "cmd" : "/bin/zsh";
        var shellArgs = OperatingSystem.IsWindows() ? $"/c {input}" : $"-lc \"{input}\"";

        var startInfo = new ProcessStartInfo(shell, shellArgs)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null) return new ToolResult(false, string.Empty, "Failed to start process.");

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return new ToolResult(process.ExitCode == 0, stdout, stderr);
    }
}

public sealed class GitTool : ITool
{
    private readonly TerminalTool _terminal = new();

    public string Name => "git";

    public string Description => "Read git status, log, and diff information.";

    public Task<ToolResult> ExecuteAsync(string input, CancellationToken ct = default) =>
        _terminal.ExecuteAsync($"git {input}", ct);
}

public sealed class SearchTool : ITool
{
    private readonly TerminalTool _terminal = new();

    public string Name => "search";

    public string Description => "Search the repository using ripgrep.";

    public Task<ToolResult> ExecuteAsync(string input, CancellationToken ct = default) =>
        _terminal.ExecuteAsync($"rg -n \"{input.Replace("\"", "\\\"")}\" .", ct);
}

public sealed class FileEditTool : ITool
{
    public string Name => "file-edit";

    public string Description => "Summarize intended file edits before applying them.";

    public Task<ToolResult> ExecuteAsync(string input, CancellationToken ct = default) =>
        Task.FromResult(new ToolResult(true, $"File edit request: {input}", string.Empty));
}

public sealed class DiagnosticsTool : ITool
{
    private readonly TerminalTool _terminal = new();

    public string Name => "diagnostics";

    public string Description => "Run dotnet build or other diagnostics commands.";

    public Task<ToolResult> ExecuteAsync(string input, CancellationToken ct = default) =>
        _terminal.ExecuteAsync(input, ct);
}

public sealed class TestRunnerTool : ITool
{
    private readonly TerminalTool _terminal = new();

    public string Name => "test-runner";

    public string Description => "Execute automated test commands.";

    public Task<ToolResult> ExecuteAsync(string input, CancellationToken ct = default) =>
        _terminal.ExecuteAsync(input, ct);
}

public sealed record ToolResult(bool Success, string Output, string Error);
