using CodeCli.Commands;
using CodeCli.Providers;
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
string? endpointOverride = GetFlag(args, "--endpoint");
string? providerOverride = GetFlag(args, "--provider");
string? outputFile      = GetFlag(args, "--output");
string? runtimeOverride = GetFlag(args, "--runtime");
bool    noStream        = args.Contains("--no-stream");
bool    verbose         = args.Contains("--verbose");

// Strip flags from args for command routing
var cleanArgs = StripFlags(args);

// ── Load configuration ────────────────────────────────────────────────────────

var config = ConfigManager.Load();
if (modelOverride  is not null) config.Model  = modelOverride;
if (hostOverride   is not null) { config.Host = hostOverride; config.Endpoint = hostOverride; }
if (endpointOverride is not null) { config.Endpoint = endpointOverride; config.Host = endpointOverride; }
if (providerOverride is not null) config.Provider = ConfigManager.NormalizeProvider(providerOverride);
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
var providerRegistry = new ModelProviderRegistry(config);

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

if (command is "explain-project")
{
    var explainProjectProvider = providerRegistry.CreateActiveProvider();
    var explainProjectAssistant = new CodeAssistantService(explainProjectProvider, config.Model);
    var explainProjectAgent = new AutonomousCodingAgent(
        explainProjectAssistant,
        new CodeContextBuilder(new RepositoryScanner(), new CSharpProjectAnalyzer()));
    var projectCommand = new ExplainProjectCommand(explainProjectAssistant, explainProjectAgent);
    await projectCommand.ExecuteAsync(cts.Token);
    return 0;
}

// ── Verify provider is running ────────────────────────────────────────────────

if (config.Provider == "ollama" && runtime.UsesDocker)
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

var provider = providerRegistry.CreateActiveProvider();
var assistant = new CodeAssistantService(provider, config.Model);
var agent = new AutonomousCodingAgent(
    assistant,
    new CodeContextBuilder(new RepositoryScanner(), new CSharpProjectAnalyzer()));

if (command is "models")
{
    ConsoleUI.SectionHeader("INSTALLED MODELS");
    var models = await ConsoleUI.WithSpinnerAsync(
        "Fetching models",
        () => provider.GetAvailableModelsAsync(cts.Token),
        cts.Token);

    if (models.Count == 0)
    {
        var installCommand = config.Provider == "ollama" && runtime.UsesDocker
            ? $"docker exec -it {config.DockerContainerName} ollama pull qwen2.5-coder:7b"
            : config.Provider == "ollama"
                ? "ollama pull qwen2.5-coder:7b"
                : "Use your provider's model management workflow to install a model.";
        ConsoleUI.Warning($"No models found. Install one with: {installCommand}");
    }
    else
    {
        ConsoleUI.PrintModelTable(models);
        ConsoleUI.Info($"Active model: {config.Model}");
    }

    return 0;
}

if (command is "provider")
{
    var providerCommand = new ProviderCommand();
    providerCommand.Execute(assistant.ProviderName, assistant.Endpoint, assistant.Model, providerRegistry.GetAvailableProviders());
    return 0;
}

// For all AI commands, ensure provider is available
bool providerAvailable = await ConsoleUI.WithSpinnerAsync(
    $"Connecting to {assistant.ProviderName}",
    () => provider.IsAvailableAsync(cts.Token),
    cts.Token);

if (!providerAvailable)
{
    ConsoleUI.Error($"Cannot connect to {assistant.ProviderName} at {assistant.Endpoint}");
    Console.WriteLine();
    Console.WriteLine("  To fix this:");
    if (config.Provider == "ollama")
    {
        foreach (var step in runtime.GetStartupHelp())
            Console.WriteLine($"  {step}");
    }
    else
    {
        Console.WriteLine("  1. Verify the endpoint is correct");
        Console.WriteLine("  2. Verify the provider server is running");
        Console.WriteLine("  3. Verify the selected model exists on the target endpoint");
    }
    Console.WriteLine();
    return 1;
}

if (verbose)
    ConsoleUI.Success($"Connected to {assistant.ProviderName} at {assistant.Endpoint} | Model: {config.Model}");

// ── Route to commands ─────────────────────────────────────────────────────────

int exitCode = 0;

try
{
    switch (command)
    {
        case "chat":
        {
            var cmd = new ChatCommand(assistant);
            await cmd.ExecuteAsync(cts.Token);
            break;
        }

        case "ask":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli ask <question>");
                exitCode = 1; break;
            }

            var question = string.Join(" ", cleanArgs[1..]);
            var cmd = new AskCommand(assistant);
            await cmd.ExecuteAsync(question, outputFile, cts.Token);
            break;
        }

        case "write":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli write <description>");
                exitCode = 1; break;
            }

            var description = string.Join(" ", cleanArgs[1..]);
            var cmd = new WriteCommand(assistant);
            await cmd.ExecuteAsync(description, outputFile, cts.Token);
            break;
        }

        case "fix":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli fix <file> [--error <message>]");
                exitCode = 1; break;
            }

            var filePath   = cleanArgs[1];
            var errMsg     = GetFlag(args, "--error");
            var cmd        = new FixCommand(assistant);
            await cmd.ExecuteAsync(filePath, errMsg, outputFile, cts.Token);
            break;
        }

        case "diagnose":
        {
            var targetPath = cleanArgs.Length >= 2 ? cleanArgs[1] : null;
            var cmd = new DiagnoseCommand(assistant, agent);
            await cmd.ExecuteAsync(targetPath, outputFile, cts.Token);
            break;
        }

        case "optimize":
        {
            var targetPath = cleanArgs.Length >= 2 ? cleanArgs[1] : null;
            var cmd = new OptimizeCommand(assistant, agent);
            await cmd.ExecuteAsync(targetPath, outputFile, cts.Token);
            break;
        }

        case "architecture":
        {
            var cmd = new ArchitectureCommand(assistant, agent);
            await cmd.ExecuteAsync(outputFile, cts.Token);
            break;
        }

        case "review":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli review <file>");
                exitCode = 1; break;
            }

            var cmd = new ReviewCommand(assistant);
            await cmd.ExecuteAsync(cleanArgs[1], outputFile, cts.Token);
            break;
        }

        case "explain":
        {
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli explain <file>");
                exitCode = 1; break;
            }

            var cmd = new ExplainCommand(assistant);
            await cmd.ExecuteAsync(cleanArgs[1], outputFile, cts.Token);
            break;
        }

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

static string? GetFlag(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static string[] StripFlags(string[] args)
{
    var result = new List<string>();
    var flagsWithValues = new HashSet<string> { "--model", "--host", "--endpoint", "--provider", "--output", "--error", "--runtime" };

    for (int i = 0; i < args.Length; i++)
    {
        if (flagsWithValues.Contains(args[i]))
        {
            i++;
            continue;
        }

        if (args[i].StartsWith("--")) continue;

        result.Add(args[i]);
    }

    return result.ToArray();
}
