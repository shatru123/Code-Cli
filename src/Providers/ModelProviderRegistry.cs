using CodeCli.Services;

namespace CodeCli.Providers;

public sealed class ModelProviderRegistry(AppConfig config)
{
    private readonly AppConfig _config = config;

    public IModelProvider CreateActiveProvider()
    {
        return _config.Provider.ToLowerInvariant() switch
        {
            "claude"             => new ClaudeModelProvider(_config.AnthropicApiKey ?? _config.ApiKey ?? string.Empty),
            "openai-compatible"  => new OpenAiCompatibleModelProvider(_config.Endpoint, _config.ApiKey),
            "llama.cpp"          => new LlamaCppModelProvider(_config.Endpoint),
            _                    => new OllamaModelProvider(_config.Endpoint)   // "ollama" + default
        };
    }

    public IReadOnlyList<string> GetAvailableProviders() =>
        ["ollama", "claude", "openai-compatible", "llama.cpp"];
}
