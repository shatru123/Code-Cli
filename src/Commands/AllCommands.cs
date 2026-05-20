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

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            ConsoleUI.Error($"Cannot read file: {ex.Message}");
            return null;
        }
    }
}

// ── ask command ───────────────────────────────────────────────────────────────

public class AskCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string question,
        string? outputFile,
        CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader($"ASK → {question.Truncate(60)}");
        var stream = Assistant.AskAsync(question, ct);
        await StreamToConsoleAsync(stream, outputFile, ct);
    }
}

// ── write command ─────────────────────────────────────────────────────────────

public class WriteCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string description,
        string? outputFile,
        CancellationToken ct = default)
    {
        ConsoleUI.SectionHeader($"WRITE → {description.Truncate(60)}");
        var stream = Assistant.WriteCodeAsync(description, ct);
        await StreamToConsoleAsync(stream, outputFile, ct);
    }
}

// ── fix command ───────────────────────────────────────────────────────────────

public class FixCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string filePath,
        string? errorMessage,
        string? outputFile,
        CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"FIX → {Path.GetFileName(filePath)}");

        if (!string.IsNullOrWhiteSpace(errorMessage))
            ConsoleUI.Info($"Error context: {errorMessage}");

        var stream = Assistant.FixCodeAsync(code, errorMessage, ct);
        await StreamToConsoleAsync(stream, outputFile, ct);
    }
}

// ── review command ────────────────────────────────────────────────────────────

public class ReviewCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string filePath,
        string? outputFile,
        CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"REVIEW → {Path.GetFileName(filePath)}");
        var stream = Assistant.ReviewCodeAsync(code, ct);
        await StreamToConsoleAsync(stream, outputFile, ct);
    }
}

// ── explain command ───────────────────────────────────────────────────────────

public class ExplainCommand(CodeAssistantService assistant) : CommandBase(assistant)
{
    public async Task ExecuteAsync(
        string filePath,
        string? outputFile,
        CancellationToken ct = default)
    {
        var code = ReadFileOrNull(filePath);
        if (code is null) return;

        ConsoleUI.SectionHeader($"EXPLAIN → {Path.GetFileName(filePath)}");
        var stream = Assistant.ExplainCodeAsync(code, ct);
        await StreamToConsoleAsync(stream, outputFile, ct);
    }
}

// ── chat command (interactive REPL) ──────────────────────────────────────────

public class ChatCommand : CommandBase
{
    private readonly CodeAssistantService _chat;
    public ChatCommand(CodeAssistantService assistant) : base(assistant) { _chat = assistant; }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        ConsoleUI.PrintBanner();
        ConsoleUI.Info($"Model: {_chat.Model}   |   Type 'exit' or 'quit' to leave, 'clear' to reset chat");
        ConsoleUI.Info("Commands: /fix <file>  /review <file>  /explain <file>  /model <name>");
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
                var path = input[5..].Trim();
                var fixCmd = new FixCommand(_chat);
                await fixCmd.ExecuteAsync(path, null, null, ct);
                continue;
            }

            if (input.StartsWith("/review "))
            {
                var path = input[8..].Trim();
                var reviewCmd = new ReviewCommand(_chat);
                await reviewCmd.ExecuteAsync(path, null, ct);
                continue;
            }

            if (input.StartsWith("/explain "))
            {
                var path = input[9..].Trim();
                var explainCmd = new ExplainCommand(_chat);
                await explainCmd.ExecuteAsync(path, null, ct);
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
                ConsoleUI.Info("Commands: /fix <file>  /review <file>  /explain <file>  /model <name>  clear  exit");
                continue;
            }

            // Regular chat
            Console.WriteLine();
            try
            {
                var stream = _chat.ChatAsync(input, history, ct);
                var responseBuilder = new System.Text.StringBuilder();

                ConsoleUI.AssistantPrefix();
                ConsoleUI.ResetStreamState();

                await foreach (var token in stream.WithCancellation(ct))
                {
                    ConsoleUI.StreamToken(token);
                    responseBuilder.Append(token);
                }

                Console.WriteLine();

                var response = responseBuilder.ToString();

                history.Add(("user", input));
                history.Add(("assistant", response));

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
}

// ── Extension helpers ─────────────────────────────────────────────────────────

internal static class StringExtensions
{
    public static string Truncate(this string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
