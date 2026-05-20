using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCli.Services;

public class AppConfig
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "qwen2.5-coder:7b";

    [JsonPropertyName("host")]
    public string Host { get; set; } = "http://localhost:11434";

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = true;

    [JsonPropertyName("history_size")]
    public int HistorySize { get; set; } = 10;

    [JsonPropertyName("preferred_language")]
    public string PreferredLanguage { get; set; } = "auto";
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
            if (!File.Exists(ConfigFile)) return new AppConfig();
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch { return new AppConfig(); }
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
        Console.WriteLine($"  Model       : {config.Model}");
        Console.WriteLine($"  Ollama host : {config.Host}");
        Console.WriteLine($"  Streaming   : {config.Stream}");
        Console.WriteLine($"  History     : {config.HistorySize} messages");
        Console.WriteLine($"  Language    : {config.PreferredLanguage}");
    }
}
