using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCli.Services;

public class AppConfig
{
    // ── Shared ────────────────────────────────────────────────────────────────
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "ollama";   // "ollama" | "claude" | "openai-compatible" | "llama.cpp"

    [JsonPropertyName("model")]
    public string Model { get; set; } = "qwen2.5-coder:7b";

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = true;

    [JsonPropertyName("history_size")]
    public int HistorySize { get; set; } = 10;

    [JsonPropertyName("preferred_language")]
    public string PreferredLanguage { get; set; } = "auto";

    [JsonPropertyName("max_context_files")]
    public int MaxContextFiles { get; set; } = 8;

    // ── Anthropic / Claude ────────────────────────────────────────────────────
    [JsonPropertyName("anthropic_api_key")]
    public string? AnthropicApiKey { get; set; }

    [JsonPropertyName("anthropic_model")]
    public string AnthropicModel { get; set; } = "claude-sonnet-4-5";

    // ── Ollama / generic endpoint providers ───────────────────────────────────
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "http://localhost:11434";

    [JsonPropertyName("host")]
    public string Host { get; set; } = "http://localhost:11434";

    /// <summary>Generic API key for openai-compatible and other providers.</summary>
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("runtime")]
    public string Runtime { get; set; } = "local";

    [JsonPropertyName("docker_image")]
    public string DockerImage { get; set; } = "ollama/ollama:latest";

    [JsonPropertyName("docker_container_name")]
    public string DockerContainerName { get; set; } = "code-cli-ollama";

    [JsonPropertyName("docker_volume")]
    public string DockerVolume { get; set; } = "code-cli-ollama";

    [JsonPropertyName("docker_auto_start")]
    public bool DockerAutoStart { get; set; } = true;
}

public static class ConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".code-cli");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── Load / Save ───────────────────────────────────────────────────────────

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigFile)) return Normalize(new AppConfig());
            var json = File.ReadAllText(ConfigFile);
            return Normalize(JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig());
        }
        catch { return Normalize(new AppConfig()); }
    }

    public static void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(config, JsonOptions));
        }
        catch { /* silent */ }
    }

    // ── Mutating helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Persist the Anthropic API key and auto-switch the active provider to Claude.
    /// Usage: code-cli config --set-key sk-ant-...
    /// </summary>
    public static void SetApiKey(string key)
    {
        var cfg = Load();
        cfg.AnthropicApiKey = key.Trim();
        cfg.Provider        = "claude";
        Save(cfg);
    }

    /// <summary>
    /// Switch the active provider.
    /// Usage: code-cli config --set-provider ollama|claude|openai-compatible|llama.cpp
    /// </summary>
    public static void SetProvider(string provider)
    {
        var cfg = Load();
        cfg.Provider = NormalizeProvider(provider);
        Save(cfg);
    }

    // ── Print ─────────────────────────────────────────────────────────────────

    public static void Print(AppConfig config)
    {
        Console.WriteLine($"  Config file     : {ConfigFile}");
        Console.WriteLine($"  Active provider : {config.Provider}");
        Console.WriteLine();

        // Claude section
        var keyStatus = string.IsNullOrWhiteSpace(config.AnthropicApiKey)
            ? "❌ not set  →  run: code-cli config --set-key sk-ant-..."
            : $"✔  {MaskKey(config.AnthropicApiKey)}";
        Console.WriteLine($"  [Claude]");
        Console.WriteLine($"    API key       : {keyStatus}");
        Console.WriteLine($"    Model         : {config.AnthropicModel}");
        Console.WriteLine();

        // Ollama / other providers
        Console.WriteLine($"  [Ollama / other]");
        Console.WriteLine($"    Model         : {config.Model}");
        Console.WriteLine($"    Endpoint      : {config.Endpoint}");
        Console.WriteLine($"    Runtime       : {config.Runtime}");
        if (config.Runtime.Equals("docker", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"    Docker image  : {config.DockerImage}");
            Console.WriteLine($"    Container     : {config.DockerContainerName}");
            Console.WriteLine($"    Volume        : {config.DockerVolume}");
            Console.WriteLine($"    Auto start    : {config.DockerAutoStart}");
        }
        Console.WriteLine();

        Console.WriteLine($"  Streaming       : {config.Stream}");
        Console.WriteLine($"  History size    : {config.HistorySize} messages");
        Console.WriteLine($"  Context files   : {config.MaxContextFiles}");
        Console.WriteLine($"  Language        : {config.PreferredLanguage}");
    }

    // ── Normalization ─────────────────────────────────────────────────────────

    private static AppConfig Normalize(AppConfig c)
    {
        // Auto-activate Claude if env var is set and no key stored yet
        var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey) && string.IsNullOrWhiteSpace(c.AnthropicApiKey))
            c.AnthropicApiKey = envKey;

        c.Provider        = NormalizeProvider(c.Provider);
        c.AnthropicModel  = string.IsNullOrWhiteSpace(c.AnthropicModel) ? "claude-sonnet-4-5" : c.AnthropicModel.Trim();
        c.Model           = string.IsNullOrWhiteSpace(c.Model)    ? "qwen2.5-coder:7b"     : c.Model.Trim();
        c.Endpoint        = string.IsNullOrWhiteSpace(c.Endpoint)
            ? (string.IsNullOrWhiteSpace(c.Host) ? "http://localhost:11434" : c.Host.Trim())
            : c.Endpoint.Trim();
        c.Host            = string.IsNullOrWhiteSpace(c.Host) ? c.Endpoint : c.Host.Trim();
        c.Runtime         = NormalizeRuntime(c.Runtime);
        c.DockerImage     = string.IsNullOrWhiteSpace(c.DockerImage)    ? "ollama/ollama:latest" : c.DockerImage.Trim();
        c.DockerContainerName = string.IsNullOrWhiteSpace(c.DockerContainerName) ? "code-cli-ollama" : c.DockerContainerName.Trim();
        c.DockerVolume    = string.IsNullOrWhiteSpace(c.DockerVolume)   ? "code-cli-ollama"     : c.DockerVolume.Trim();
        c.PreferredLanguage = string.IsNullOrWhiteSpace(c.PreferredLanguage) ? "auto" : c.PreferredLanguage.Trim();

        if (c.HistorySize     <= 0) c.HistorySize     = 10;
        if (c.MaxContextFiles <= 0) c.MaxContextFiles = 8;

        return c;
    }

    public static string NormalizeProvider(string? provider)
    {
        return provider?.Trim().ToLowerInvariant() switch
        {
            "claude"            => "claude",
            "openai-compatible" => "openai-compatible",
            "llama.cpp"         => "llama.cpp",
            _                   => "ollama"
        };
    }

    public static string NormalizeRuntime(string? runtime) =>
        runtime?.Trim().Equals("docker", StringComparison.OrdinalIgnoreCase) == true ? "docker" : "local";

    private static string MaskKey(string key) =>
        key.Length > 12 ? key[..8] + "..." + key[^4..] : "****";
}
