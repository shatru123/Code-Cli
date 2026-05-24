namespace CodeCli.Services;

public static class ProviderRegistry
{
    public static readonly IReadOnlyList<string> SupportedProviders =
    [
        "ollama",
        "claude",
        "openai-compatible",
        "llama.cpp"
    ];

    public static bool IsSupported(string provider)
        => SupportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase);
}
