using CodeCli.Commands;
using CodeCli.Services;
using CodeCli.UI;

// ── Handle Ctrl+C gracefully ──────────────────────────────────────────────────

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine();
    ConsoleUI.Warning("Cancelled.");
};

// ── Parse global flags BEFORE System.CommandLine routing ─────────────────────

string? modelOverride   = GetFlag(args, "--model");
string? hostOverride    = GetFlag(args, "--host");
string? outputFile      = GetFlag(args, "--output");
string? runtimeOverride = GetFlag(args, "--runtime");
bool    noStream        = args.Contains("--no-stream");
bool    verbose         = args.Contains("--verbose");

// Strip flags from args for command routing
var cleanArgs = StripFlags(args);

// ── Load configuration ────────────────────────────────────────────────────────

var config = ConfigManager.Load();
if (modelOverride  is not null) config.Model  = modelOverride;
if (hostOverride   is not null) config.Host   = hostOverride;
if (runtimeOverride is not null) config.Runtime = ConfigManager.NormalizeRuntime(runtimeOverride);
if (noStream)                   config.Stream = false;

// ── No args → show help ───────────────────────────────────────────────────────

if (cleanArgs.Length == 0)
{
    ConsoleUI.PrintHelp();
    return 0;
}

var command = cleanArgs[0].ToLowerInvariant();
var runtime = new OllamaRuntimeManager(config);

// ── Special commands (no Ollama needed) ──────────────────────────────────────

if (command is "--help" or "-h" or "help")
{
    ConsoleUI.PrintHelp();
    return 0;
}

if (command is "--version" or "-v" or "version")
{
    Console.WriteLine("Code-Cli v1.0.0");
    return 0;
}

if (command is "config")
{
    ConsoleUI.SectionHeader("CONFIGURATION");
    ConfigManager.Print(config);
    Console.WriteLine();
    ConsoleUI.Info("To change: edit ~/.code-cli/config.json");
    return 0;
}

// ── Verify Ollama is running ──────────────────────────────────────────────────

var ollama    = new OllamaService(config.Host);
var assistant = new CodeAssistantService(ollama, config.Model);

if (runtime.UsesDocker)
{
    var preparation = await ConsoleUI.WithSpinnerAsync(
        "Preparing Ollama Docker runtime",
        () => runtime.PrepareAsync(cts.Token),
        cts.Token);

    if (!preparation.Success)
    {
        ConsoleUI.Error(preparation.Message);
        Console.WriteLine();
        Console.WriteLine("  To fix this:");
        foreach (var step in runtime.GetStartupHelp())
            Console.WriteLine($"  {step}");
        Console.WriteLine();
        return 1;
    }

    if (verbose)
        ConsoleUI.Info(preparation.Message);
}

if (command is "models")
{
    ConsoleUI.SectionHeader("INSTALLED MODELS");
    var models = await ConsoleUI.WithSpinnerAsync(
        "Fetching models",
        () => ollama.GetAvailableModelsAsync(),
        cts.Token);

    if (models.Count == 0)
    {
        var installCommand = runtime.UsesDocker
            ? $"docker exec -it {config.DockerContainerName} ollama pull qwen2.5-coder:7b"
            : "ollama pull qwen2.5-coder:7b";
        ConsoleUI.Warning($"No models found. Install one with: {installCommand}");
    }
    else
    {
        ConsoleUI.PrintModelTable(models);
        ConsoleUI.Info($"Active model: {config.Model}");
    }

    return 0;
}

// For all AI commands, ensure Ollama is available
bool ollamaRunning = await ConsoleUI.WithSpinnerAsync(
    "Connecting to Ollama",
    () => ollama.IsRunningAsync(),
    cts.Token);

if (!ollamaRunning)
{
    ConsoleUI.Error($"Cannot connect to Ollama at {config.Host}");
    Console.WriteLine();
    Console.WriteLine("  To fix this:");
    foreach (var step in runtime.GetStartupHelp())
        Console.WriteLine($"  {step}");
    Console.WriteLine();
    return 1;
}

if (verbose)
    ConsoleUI.Success($"Connected to Ollama at {config.Host} | Model: {config.Model}");

// ── Route to commands ─────────────────────────────────────────────────────────

int exitCode = 0;

try
{
    switch (command)
    {
        // ── chat ─────────────────────────────────────────────────────────────
        case "chat":
        {
            var cmd = new ChatCommand(assistant);
            await cmd.ExecuteAsync(cts.Token);
            break;
        }

        // ── ask ──────────────────────────────────────────────────────────────
        case "ask":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli ask <question>");
                ConsoleUI.Error("Example: code-cli ask \"How do I implement dependency injection in .NET?\"");
                exitCode = 1; break;
            }

            // Join remaining args as question (no need for quotes)
            var question = string.Join(" ", cleanArgs[1..]);
            var cmd = new AskCommand(assistant);
            await cmd.ExecuteAsync(question, outputFile, cts.Token);
            break;
        }

        // ── write ────────────────────────────────────────────────────────────
        case "write":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli write <description>");
                ConsoleUI.Error("Example: code-cli write \"JWT authentication middleware in ASP.NET Core\"");
                exitCode = 1; break;
            }

            var description = string.Join(" ", cleanArgs[1..]);
            var cmd = new WriteCommand(assistant);
            await cmd.ExecuteAsync(description, outputFile, cts.Token);
            break;
        }

        // ── fix ──────────────────────────────────────────────────────────────
        case "fix":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli fix <file> [--error <message>]");
                ConsoleUI.Error("Example: code-cli fix MyService.cs --error \"NullReferenceException at line 42\"");
                exitCode = 1; break;
            }

            var filePath   = cleanArgs[1];
            var errMsg     = GetFlag(args, "--error");
            var cmd        = new FixCommand(assistant);
            await cmd.ExecuteAsync(filePath, errMsg, outputFile, cts.Token);
            break;
        }

        // ── review ───────────────────────────────────────────────────────────
        case "review":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli review <file>");
                ConsoleUI.Error("Example: code-cli review Controllers/UserController.cs");
                exitCode = 1; break;
            }

            var cmd = new ReviewCommand(assistant);
            await cmd.ExecuteAsync(cleanArgs[1], outputFile, cts.Token);
            break;
        }

        // ── explain ──────────────────────────────────────────────────────────
        case "explain":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli explain <file>");
                ConsoleUI.Error("Example: code-cli explain Program.cs");
                exitCode = 1; break;
            }

            var cmd = new ExplainCommand(assistant);
            await cmd.ExecuteAsync(cleanArgs[1], outputFile, cts.Token);
            break;
        }

        // ── unknown ───────────────────────────────────────────────────────────
        default:
        {
            ConsoleUI.Error($"Unknown command: '{command}'");
            Console.WriteLine("Run 'code-cli --help' for usage.");
            exitCode = 1;
            break;
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    ConsoleUI.Warning("Operation cancelled.");
    exitCode = 130;
}
catch (Exception ex)
{
    ConsoleUI.Error($"Unexpected error: {ex.Message}");
    if (verbose) Console.WriteLine(ex.StackTrace);
    exitCode = 1;
}

Console.WriteLine();
return exitCode;

// ── Helpers ───────────────────────────────────────────────────────────────────

static string? GetFlag(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static string[] StripFlags(string[] args)
{
    var result = new List<string>();
    var flagsWithValues = new HashSet<string> { "--model", "--host", "--output", "--error", "--runtime" };

    for (int i = 0; i < args.Length; i++)
    {
        if (flagsWithValues.Contains(args[i]))
        {
            i++; // skip flag value
            continue;
        }
        if (args[i].StartsWith("--")) continue;
        result.Add(args[i]);
    }
    return result.ToArray();
}
