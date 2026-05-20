using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCli.Services;

public class AppConfig
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "ollama";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "qwen2.5-coder:7b";

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "http://localhost:11434";

    [JsonPropertyName("host")]
    public string Host { get; set; } = "http://localhost:11434";

    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = true;

    [JsonPropertyName("history_size")]
    public int HistorySize { get; set; } = 10;

    [JsonPropertyName("preferred_language")]
    public string PreferredLanguage { get; set; } = "auto";

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

    [JsonPropertyName("max_context_files")]
    public int MaxContextFiles { get; set; } = 8;
}

public static class ConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".code-cli");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

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
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFile, json);
        }
        catch { /* silently fail */ }
    }

    public static void Print(AppConfig config)
    {
        Console.WriteLine($"  Config file : {ConfigFile}");
        Console.WriteLine($"  Provider    : {config.Provider}");
        Console.WriteLine($"  Model       : {config.Model}");
        Console.WriteLine($"  Runtime     : {config.Runtime}");
        Console.WriteLine($"  Endpoint    : {config.Endpoint}");
        if (config.Runtime.Equals("docker", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  Docker image: {config.DockerImage}");
            Console.WriteLine($"  Container   : {config.DockerContainerName}");
            Console.WriteLine($"  Volume      : {config.DockerVolume}");
            Console.WriteLine($"  Auto start  : {config.DockerAutoStart}");
        }
        Console.WriteLine($"  Streaming   : {config.Stream}");
        Console.WriteLine($"  History     : {config.HistorySize} messages");
        Console.WriteLine($"  Language    : {config.PreferredLanguage}");
        Console.WriteLine($"  Context     : {config.MaxContextFiles} files");
    }

    private static AppConfig Normalize(AppConfig config)
    {
        config.Provider = NormalizeProvider(config.Provider);
        config.Model = string.IsNullOrWhiteSpace(config.Model) ? "qwen2.5-coder:7b" : config.Model.Trim();
        config.Endpoint = string.IsNullOrWhiteSpace(config.Endpoint)
            ? (string.IsNullOrWhiteSpace(config.Host) ? "http://localhost:11434" : config.Host.Trim())
            : config.Endpoint.Trim();
        config.Host = string.IsNullOrWhiteSpace(config.Host) ? config.Endpoint : config.Host.Trim();
        config.PreferredLanguage = string.IsNullOrWhiteSpace(config.PreferredLanguage) ? "auto" : config.PreferredLanguage.Trim();
        config.Runtime = NormalizeRuntime(config.Runtime);
        config.DockerImage = string.IsNullOrWhiteSpace(config.DockerImage) ? "ollama/ollama:latest" : config.DockerImage.Trim();
        config.DockerContainerName = string.IsNullOrWhiteSpace(config.DockerContainerName) ? "code-cli-ollama" : config.DockerContainerName.Trim();
        config.DockerVolume = string.IsNullOrWhiteSpace(config.DockerVolume) ? "code-cli-ollama" : config.DockerVolume.Trim();

        if (config.HistorySize <= 0) config.HistorySize = 10;
        if (config.MaxContextFiles <= 0) config.MaxContextFiles = 8;

        return config;
    }

    public static string NormalizeProvider(string? provider)
    {
        var normalized = provider?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "ollama" => "ollama",
            "openai-compatible" => "openai-compatible",
            "llama.cpp" => "llama.cpp",
            _ => "ollama"
        };
    }

    public static string NormalizeRuntime(string? runtime)
    {
        if (string.IsNullOrWhiteSpace(runtime)) return "local";
        return runtime.Trim().Equals("docker", StringComparison.OrdinalIgnoreCase) ? "docker" : "local";
    }
}
