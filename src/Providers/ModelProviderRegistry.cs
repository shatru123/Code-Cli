using CodeCli.Services;

namespace CodeCli.Providers;

public sealed class ModelProviderRegistry(AppConfig config)
{
    private readonly AppConfig _config = config;

    public IModelProvider CreateActiveProvider()
    {
        var provider = _config.Provider.ToLowerInvariant();

        return provider switch
        {
            "ollama" => new OllamaModelProvider(_config.Endpoint),
            "openai-compatible" => new OpenAiCompatibleModelProvider(_config.Endpoint, _config.ApiKey),
            "llama.cpp" => new LlamaCppModelProvider(_config.Endpoint),
            _ => new OllamaModelProvider(_config.Endpoint)
        };
    }

    public IReadOnlyList<string> GetAvailableProviders() =>
        ["ollama", "openai-compatible", "llama.cpp"];
}
