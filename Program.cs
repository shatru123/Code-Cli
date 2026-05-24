using CodeCli.Commands;
using CodeCli.Providers;
using CodeCli.Services;
using CodeCli.UI;

// ── Ctrl+C ────────────────────────────────────────────────────────────────────

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine();
    ConsoleUI.Warning("Cancelled.");
};

// ── Parse global flags ────────────────────────────────────────────────────────

string? modelOverride    = GetFlag(args, "--model");
string? hostOverride     = GetFlag(args, "--host");
string? endpointOverride = GetFlag(args, "--endpoint");
string? providerOverride = GetFlag(args, "--provider");
string? outputFile       = GetFlag(args, "--output");
string? runtimeOverride  = GetFlag(args, "--runtime");
string? errorMsg         = GetFlag(args, "--error");
string? framework        = GetFlag(args, "--framework");
string? goal             = GetFlag(args, "--goal");
string? focus            = GetFlag(args, "--focus");
bool    noStream         = args.Contains("--no-stream");
bool    verbose          = args.Contains("--verbose");

var cleanArgs = StripFlags(args);

// ── Load configuration ────────────────────────────────────────────────────────

var config = ConfigManager.Load();
if (modelOverride    is not null) config.Model    = modelOverride;
if (hostOverride     is not null) { config.Host = hostOverride; config.Endpoint = hostOverride; }
if (endpointOverride is not null) { config.Endpoint = endpointOverride; config.Host = endpointOverride; }
if (providerOverride is not null) config.Provider = ConfigManager.NormalizeProvider(providerOverride);
if (runtimeOverride  is not null) config.Runtime  = ConfigManager.NormalizeRuntime(runtimeOverride);
if (noStream)                     config.Stream   = false;

// When Claude is the active provider, use AnthropicModel as the model name
if (config.Provider == "claude" && modelOverride is null)
    config.Model = config.AnthropicModel;

// ── No args → help ────────────────────────────────────────────────────────────

if (cleanArgs.Length == 0)
{
    ConsoleUI.PrintHelp();
    return 0;
}

var command          = cleanArgs[0].ToLowerInvariant();
var runtime          = new OllamaRuntimeManager(config);
var providerRegistry = new ModelProviderRegistry(config);

// ── Provider-independent commands ─────────────────────────────────────────────

if (command is "--help" or "-h" or "help")
{
    ConsoleUI.PrintHelp();
    return 0;
}

if (command is "--version" or "-v" or "version")
{
    Console.WriteLine("Code-Cli v2.0.0");
    return 0;
}

if (command is "config")
{
    // Mutation sub-commands
    var setKey      = GetFlag(args, "--set-key");
    var setProvider = GetFlag(args, "--set-provider");

    if (setKey is not null)
    {
        ConfigManager.SetApiKey(setKey);
        ConsoleUI.Success("Anthropic API key saved. Provider switched to claude.");
        ConsoleUI.Info("Run: code-cli chat   to start using Claude.");
        return 0;
    }

    if (setProvider is not null)
    {
        ConfigManager.SetProvider(setProvider);
        ConsoleUI.Success($"Provider set to: {ConfigManager.NormalizeProvider(setProvider)}");
        return 0;
    }

    // Print
    ConsoleUI.SectionHeader("CONFIGURATION");
    ConfigManager.Print(config);
    Console.WriteLine();
    ConsoleUI.Info($"Config file: {System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".code-cli", "config.json")}");
    ConsoleUI.Info("To set Claude key:  code-cli config --set-key sk-ant-...");
    ConsoleUI.Info("To switch provider: code-cli config --set-provider ollama|claude");
    return 0;
}

if (command is "explain-project")
{
    var epProvider  = providerRegistry.CreateActiveProvider();
    var epAssistant = new CodeAssistantService(epProvider, config.Model);
    var epAgent     = new AutonomousCodingAgent(
        epAssistant,
        new CodeContextBuilder(new RepositoryScanner(), new CSharpProjectAnalyzer()));
    await new ExplainProjectCommand(epAssistant, epAgent).ExecuteAsync(cts.Token);
    return 0;
}

// ── Prepare Ollama Docker runtime if needed ───────────────────────────────────

if (config.Provider == "ollama" && runtime.UsesDocker)
{
    // PrepareAsync writes live progress to console (image pull, model pull),
    // so we do NOT wrap it in a spinner.
    ConsoleUI.SectionHeader("DOCKER RUNTIME — AUTO SETUP");
    var preparation = await runtime.PrepareAsync(cts.Token);

    if (!preparation.Success)
    {
        Console.WriteLine();
        ConsoleUI.Error(preparation.Message);
        Console.WriteLine();
        Console.WriteLine("  How to fix:");
        foreach (var step in runtime.GetStartupHelp())
            Console.WriteLine($"  {step}");
        Console.WriteLine();
        return 1;
    }

    Console.WriteLine();
}

// ── Create provider + services ────────────────────────────────────────────────

var provider  = providerRegistry.CreateActiveProvider();
var assistant = new CodeAssistantService(provider, config.Model);
var agent     = new AutonomousCodingAgent(
    assistant,
    new CodeContextBuilder(new RepositoryScanner(), new CSharpProjectAnalyzer()));

// ── models ────────────────────────────────────────────────────────────────────

if (command is "models")
{
    ConsoleUI.SectionHeader($"MODELS — {config.Provider.ToUpper()}");
    var models = await ConsoleUI.WithSpinnerAsync(
        "Fetching models",
        () => provider.GetAvailableModelsAsync(cts.Token),
        cts.Token);

    if (models.Count == 0)
        ConsoleUI.Warning("No models found.");
    else
        ConsoleUI.PrintModelTable(models, config.Model);

    return 0;
}

// ── provider ──────────────────────────────────────────────────────────────────

if (command is "provider")
{
    new ProviderCommand().Execute(
        assistant.ProviderName,
        assistant.Endpoint,
        assistant.Model,
        providerRegistry.GetAvailableProviders());
    return 0;
}

// ── Verify provider is reachable ──────────────────────────────────────────────

bool available = await ConsoleUI.WithSpinnerAsync(
    $"Connecting to {assistant.ProviderName}",
    () => provider.IsAvailableAsync(cts.Token),
    cts.Token);

if (!available)
{
    ConsoleUI.Error($"Cannot connect to {assistant.ProviderName} at {assistant.Endpoint}");
    Console.WriteLine();

    if (config.Provider == "claude")
    {
        ConsoleUI.Info("Set your API key:   code-cli config --set-key sk-ant-...");
        ConsoleUI.Info("Or set via env:     export ANTHROPIC_API_KEY=sk-ant-...");
        ConsoleUI.Info("Switch to Ollama:   code-cli config --set-provider ollama");
    }
    else
    {
        Console.WriteLine("  To fix this:");
        foreach (var step in runtime.GetStartupHelp())
            Console.WriteLine($"  {step}");
    }

    Console.WriteLine();
    return 1;
}

if (verbose)
    ConsoleUI.Success($"Connected to {assistant.ProviderName} at {assistant.Endpoint} | Model: {config.Model}");

// ── Route commands ────────────────────────────────────────────────────────────

int exitCode = 0;

try
{
    switch (command)
    {
        case "chat":
            await new ChatCommand(assistant).ExecuteAsync(cts.Token);
            break;

        case "ask":
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli ask <question>");
                exitCode = 1; break;
            }
            await new AskCommand(assistant).ExecuteAsync(
                string.Join(" ", cleanArgs[1..]), outputFile, cts.Token);
            break;

        case "write":
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli write <description>");
                exitCode = 1; break;
            }
            await new WriteCommand(assistant).ExecuteAsync(
                string.Join(" ", cleanArgs[1..]), outputFile, cts.Token);
            break;

        case "fix":
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli fix <file> [--error <message>]");
                exitCode = 1; break;
            }
            await new FixCommand(assistant).ExecuteAsync(cleanArgs[1], errorMsg, outputFile, cts.Token);
            break;

        case "review":
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli review <file>");
                exitCode = 1; break;
            }
            await new ReviewCommand(assistant).ExecuteAsync(cleanArgs[1], outputFile, cts.Token);
            break;

        case "explain":
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli explain <file>");
                exitCode = 1; break;
            }
            await new ExplainCommand(assistant).ExecuteAsync(cleanArgs[1], outputFile, cts.Token);
            break;

        case "refactor":
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli refactor <file> [--goal \"improve readability\"]");
                ConsoleUI.Error("Example: code-cli refactor Services/OrderService.cs --goal \"extract CQRS handlers\"");
                exitCode = 1; break;
            }
            await new RefactorCommand(assistant).ExecuteAsync(
                cleanArgs[1],
                goal ?? "improve readability, maintainability, and apply SOLID principles",
                outputFile,
                cts.Token);
            break;

        case "test":
            if (cleanArgs.Length < 2)
            {
                ConsoleUI.Error("Usage: code-cli test <file> [--framework xunit|pytest|jest]");
                ConsoleUI.Error("Example: code-cli test Services/PaymentService.cs --framework xunit");
                exitCode = 1; break;
            }
            await new TestCommand(assistant).ExecuteAsync(
                cleanArgs[1], framework, outputFile, cts.Token);
            break;

        case "analyse":
        case "analyze":
        {
            var analysePath = cleanArgs.Length > 1 ? cleanArgs[1] : ".";
            await new AnalyseCommand(assistant).ExecuteAsync(analysePath, focus, outputFile, cts.Token);
            break;
        }

        case "diagnose":
        {
            var targetPath = cleanArgs.Length >= 2 ? cleanArgs[1] : null;
            await new DiagnoseCommand(assistant, agent).ExecuteAsync(targetPath, outputFile, cts.Token);
            break;
        }

        case "optimize":
        case "optimise":
        {
            var targetPath = cleanArgs.Length >= 2 ? cleanArgs[1] : null;
            await new OptimizeCommand(assistant, agent).ExecuteAsync(targetPath, outputFile, cts.Token);
            break;
        }

        case "architecture":
            await new ArchitectureCommand(assistant, agent).ExecuteAsync(outputFile, cts.Token);
            break;

        default:
            ConsoleUI.Error($"Unknown command: '{command}'");
            Console.WriteLine("Run 'code-cli --help' for usage.");
            exitCode = 1;
            break;
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
    var flagsWithValues = new HashSet<string>
    {
        "--model", "--host", "--endpoint", "--provider", "--output",
        "--error", "--runtime", "--framework", "--goal", "--focus",
        "--set-key", "--set-provider"
    };
    var result = new List<string>();
    for (var i = 0; i < args.Length; i++)
    {
        if (flagsWithValues.Contains(args[i])) { i++; continue; }
        if (args[i].StartsWith("--")) continue;
        result.Add(args[i]);
    }
    return result.ToArray();
}
