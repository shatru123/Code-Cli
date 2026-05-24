namespace CodeCli.Providers;

public sealed class LlamaCppModelProvider : OpenAiCompatibleModelProvider
{
    public LlamaCppModelProvider(string endpoint)
        : base(endpoint, apiKey: null, name: "llama.cpp")
    {
    }
}
