using CodeCli.Services;
using CodeCli.UI;

namespace CodeCli.Commands;

// ── Base handler ──────────────────────────────────────────────────────────────

public abstract class CommandBase(CodeAssistantService assistant)
{
    protected CodeAssistantService Assistant { get; } = assistant;

    protected static async Task StreamToConsoleAsync(
        IAsyncEnumerable<string> stream,
        string? outputFile = null,
        CancellationToken ct = default)
    {
        var buffer = outputFile != null ? new System.Text.StringBuilder() : null;

        ConsoleUI.AssistantPrefix();
        ConsoleUI.ResetStreamState();

        await foreach (var token in stream.WithCancellation(ct))
        {
            ConsoleUI.StreamToken(token);
            buffer?.Append(token);
        }

        Console.WriteLine();

        if (outputFile != null && buffer != null)
        {
            await File.WriteAllTextAsync(outputFile, buffer.ToString(), ct);
            ConsoleUI.Success($"Response saved to: {outputFile}");
        }
    }

    protected static string? ReadFileOrNull(string path)
    {
        if (!File.Exists(path))
        {
            ConsoleUI.Error($"File not found: {path}");
            return null;
        }
        try   { return File.ReadAllText(path); }
        catch (Exception ex) { ConsoleUI.Error($"Cannot read file: {ex.Message}"); return null; }
    }

    protected static string GetRepositoryRoot() => Directory.GetCurrentDirectory();
}

// ── ask ───────────────────────────────────────────────────────────────────────

public class AskCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(string question, string? outputFile, CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader($"ASK → {question.Truncate(60)}");
        await StreamToConsoleAsync(Assistant.AskAsync(question, ct), outputFile, ct);
    }
}

// ── write ─────────────────────────────────────────────────────────────────────

public class WriteCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(string description, string? outputFile, CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader($"WRITE → {description.Truncate(60)}");
        await StreamToConsoleAsync(Assistant.WriteCodeAsync(description, ct), outputFile, ct);
    }
}

// ── fix ───────────────────────────────────────────────────────────────────────

public class FixCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string filePath, string? errorMessage, string? outputFile, CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"FIX → {Path.GetFileName(filePath)}");
        if (!string.IsNullOrWhiteSpace(errorMessage))
            ConsoleUI.Info($"Error context: {errorMessage}");

        await StreamToConsoleAsync(Assistant.FixCodeAsync(code, errorMessage, ct), outputFile, ct);
    }
}

// ── review ────────────────────────────────────────────────────────────────────

public class ReviewCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(string filePath, string? outputFile, CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"REVIEW → {Path.GetFileName(filePath)}");
        await StreamToConsoleAsync(Assistant.ReviewCodeAsync(code, ct), outputFile, ct);
    }
}

// ── explain ───────────────────────────────────────────────────────────────────

public class ExplainCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(string filePath, string? outputFile, CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"EXPLAIN → {Path.GetFileName(filePath)}");
        await StreamToConsoleAsync(Assistant.ExplainCodeAsync(code, ct), outputFile, ct);
    }
}

// ── refactor ──────────────────────────────────────────────────────────────────

public class RefactorCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string filePath, string goal, string? outputFile, CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"REFACTOR → {Path.GetFileName(filePath)}");
        ConsoleUI.Info($"Goal: {goal}");
        await StreamToConsoleAsync(Assistant.RefactorCodeAsync(code, goal, ct), outputFile, ct);
    }
}

// ── test ──────────────────────────────────────────────────────────────────────

public class TestCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string filePath, string? framework, string? outputFile, CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"TEST → {Path.GetFileName(filePath)}");
        if (framework is not null) ConsoleUI.Info($"Framework: {framework}");
        await StreamToConsoleAsync(Assistant.WriteTestsAsync(code, framework, ct), outputFile, ct);
    }
}

// ── analyse (file or whole project) ──────────────────────────────────────────

public class AnalyseCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string targetPath, string? focus, string? outputFile, CancellationToken ct = default)
    {
        string context;
        string header;

        if (File.Exists(targetPath))
        {
            var code = ReadFileOrNull(targetPath);
            if (code is null) return;
            context = ProjectContextBuilder.BuildFileContext(targetPath);
            header  = $"ANALYSE → {Path.GetFileName(targetPath)}";
        }
        else if (Directory.Exists(targetPath))
        {
            header  = $"ANALYSE PROJECT → {Path.GetFullPath(targetPath)}";
            context = await ConsoleUI.WithSpinnerAsync(
                "Scanning codebase",
                () => Task.FromResult(ProjectContextBuilder.BuildProjectContext(targetPath, focus)),
                ct);
        }
        else
        {
            ConsoleUI.Error($"Path not found: {targetPath}");
            return;
        }

        ConsoleUI.SectionHeader(header);
        if (focus is not null) ConsoleUI.Info($"Focus: {focus}");
        await StreamToConsoleAsync(Assistant.AnalyseProjectAsync(context, ct), outputFile, ct);
    }
}

// ── diagnose ──────────────────────────────────────────────────────────────────

public class DiagnoseCommand(CodeAssistantService assistant, AutonomousCodingAgent agent) : CommandBase(assistant)
{
    private readonly AutonomousCodingAgent _agent = agent;

    public async Task ExecuteAsync(string? targetPath, string? outputFile, CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader($"DIAGNOSE → {(targetPath is null ? "repository" : Path.GetFileName(targetPath))}");
        var root = GetRepositoryRoot();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            await StreamToConsoleAsync(_agent.DiagnoseRepositoryAsync(root, 8, ct), outputFile, ct);
            return;
        }

        var code = ReadFileOrNull(targetPath);
        if (code is null) return;

        var prompt = $"Diagnose the risks, bugs, and production issues in this file:\n\n```{Path.GetExtension(targetPath)}\n{code}\n```";
        await StreamToConsoleAsync(Assistant.AskWithPromptAsync(Prompts.RepositoryDiagnostician, prompt, ct), outputFile, ct);
    }
}

// ── optimize ──────────────────────────────────────────────────────────────────

public class OptimizeCommand(CodeAssistantService assistant, AutonomousCodingAgent agent) : CommandBase(assistant)
{
    private readonly AutonomousCodingAgent _agent = agent;

    public async Task ExecuteAsync(string? targetPath, string? outputFile, CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader($"OPTIMIZE → {(targetPath is null ? "repository" : Path.GetFileName(targetPath))}");
        var root = GetRepositoryRoot();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            await StreamToConsoleAsync(_agent.OptimizeRepositoryAsync(root, 8, ct), outputFile, ct);
            return;
        }

        var code = ReadFileOrNull(targetPath);
        if (code is null) return;

        var prompt = $"Optimise the following code for maintainability, performance, and reliability:\n\n```{Path.GetExtension(targetPath)}\n{code}\n```";
        await StreamToConsoleAsync(Assistant.AskWithPromptAsync(Prompts.Optimizer, prompt, ct), outputFile, ct);
    }
}

// ── architecture ──────────────────────────────────────────────────────────────

public class ArchitectureCommand(CodeAssistantService assistant, AutonomousCodingAgent agent) : CommandBase(assistant)
{
    private readonly AutonomousCodingAgent _agent = agent;

    public async Task ExecuteAsync(string? outputFile, CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader("ARCHITECTURE → repository");
        await StreamToConsoleAsync(_agent.ExplainArchitectureAsync(GetRepositoryRoot(), 8, ct), outputFile, ct);
    }
}

// ── provider ──────────────────────────────────────────────────────────────────

public class ProviderCommand
{
    public void Execute(string activeProvider, string endpoint, string model, IReadOnlyList<string> supportedProviders)
    {
        ConsoleUI.SectionHeader("PROVIDER STATUS");
        Console.WriteLine($"  Active provider : {activeProvider}");
        Console.WriteLine($"  Endpoint        : {endpoint}");
        Console.WriteLine($"  Model           : {model}");
        Console.WriteLine($"  Supported       : {string.Join(", ", supportedProviders)}");
        Console.WriteLine();
        ConsoleUI.Info("Switch provider:  code-cli config --set-provider <name>");
        ConsoleUI.Info("Set Claude key:   code-cli config --set-key sk-ant-...");
    }
}

// ── explain-project ───────────────────────────────────────────────────────────

public class ExplainProjectCommand(CodeAssistantService assistant, AutonomousCodingAgent agent) : CommandBase(assistant)
{
    private readonly AutonomousCodingAgent _agent = agent;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader("EXPLAIN PROJECT");
        await StreamToConsoleAsync(_agent.ExplainArchitectureAsync(GetRepositoryRoot(), 8, ct), null, ct);
    }
}

// ── chat ──────────────────────────────────────────────────────────────────────

public class ChatCommand : CommandBase
{
    private readonly CodeAssistantService _chat;
    public ChatCommand(CodeAssistantService assistant) : base(assistant) { _chat = assistant; }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        ConsoleUI.PrintBanner();
        ConsoleUI.Info($"Provider: {_chat.ProviderName}   |   Model: {_chat.Model}   |   Type 'exit' to leave, 'clear' to reset");
        ConsoleUI.Info("Slash commands: /fix  /review  /explain  /refactor  /test  /analyse  /model  /help");
        ConsoleUI.Separator();

        var history = new List<(string role, string content)>();

        while (!ct.IsCancellationRequested)
        {
            Console.WriteLine();
            ConsoleUI.UserPrefix();
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input)) continue;

            if (input is "exit" or "quit" or "/exit" or "/quit")
            {
                ConsoleUI.Info("Goodbye! Happy coding! 🚀");
                break;
            }

            if (input is "clear" or "/clear")
            {
                history.Clear();
                Console.Clear();
                ConsoleUI.PrintBanner();
                ConsoleUI.Info("Chat history cleared.");
                continue;
            }

            if (input.StartsWith("/fix "))
            {
                await new FixCommand(_chat).ExecuteAsync(input[5..].Trim(), null, null, ct);
                continue;
            }

            if (input.StartsWith("/review "))
            {
                await new ReviewCommand(_chat).ExecuteAsync(input[8..].Trim(), null, ct);
                continue;
            }

            if (input.StartsWith("/explain "))
            {
                await new ExplainCommand(_chat).ExecuteAsync(input[9..].Trim(), null, ct);
                continue;
            }

            if (input.StartsWith("/refactor "))
            {
                var parts = input[10..].Split(" --goal ", 2, StringSplitOptions.TrimEntries);
                var path  = parts[0];
                var goal  = parts.Length > 1 ? parts[1] : "improve readability and apply SOLID principles";
                await new RefactorCommand(_chat).ExecuteAsync(path, goal, null, ct);
                continue;
            }

            if (input.StartsWith("/test "))
            {
                await new TestCommand(_chat).ExecuteAsync(input[6..].Trim(), null, null, ct);
                continue;
            }

            if (input.StartsWith("/analyse ") || input.StartsWith("/analyze "))
            {
                var path = input.StartsWith("/analyse ") ? input[9..].Trim() : input[9..].Trim();
                await new AnalyseCommand(_chat).ExecuteAsync(path, null, null, ct);
                continue;
            }

            if (input.StartsWith("/model "))
            {
                _chat.Model = input[7..].Trim();
                ConsoleUI.Success($"Model switched to: {_chat.Model}");
                continue;
            }

            if (input is "/models")
            {
                ConsoleUI.Info("Run 'code-cli models' in a new terminal to list models.");
                continue;
            }

            if (input is "/help" or "help")
            {
                PrintChatHelp();
                continue;
            }

            // Regular chat
            Console.WriteLine();
            try
            {
                var stream          = _chat.ChatAsync(input, history, ct);
                var responseBuilder = new System.Text.StringBuilder();

                ConsoleUI.AssistantPrefix();
                ConsoleUI.ResetStreamState();

                await foreach (var token in stream.WithCancellation(ct))
                {
                    ConsoleUI.StreamToken(token);
                    responseBuilder.Append(token);
                }
                Console.WriteLine();

                history.Add(("user",      input));
                history.Add(("assistant", responseBuilder.ToString()));

                if (history.Count > 20)
                    history = history.TakeLast(20).ToList();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine();
                ConsoleUI.Warning("Response interrupted.");
            }
        }
    }

    private static void PrintChatHelp()
    {
        Console.WriteLine();
        var cmds = new (string cmd, string desc)[]
        {
            ("/fix <file>",                   "Fix bugs in a file"),
            ("/review <file>",                "Full code review"),
            ("/explain <file>",               "Explain a file"),
            ("/refactor <file> [--goal <g>]", "Refactor with a goal"),
            ("/test <file>",                  "Generate unit tests"),
            ("/analyse <path>",               "Analyse file or directory"),
            ("/model <name>",                 "Switch AI model"),
            ("clear",                         "Clear chat history"),
            ("exit",                          "Quit"),
        };
        foreach (var (cmd, desc) in cmds)
            ConsoleUI.Info($"{cmd,-40} {desc}");
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

internal static class StringExtensions
{
    public static string Truncate(this string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
