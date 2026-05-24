namespace CodeCli.Providers;

public interface IModelProvider
{
    string Name { get; }

    string Endpoint { get; }

    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default);

    IAsyncEnumerable<string> StreamCompletionAsync(ModelRequest request, CancellationToken ct = default);

    Task<string> CompleteAsync(ModelRequest request, CancellationToken ct = default);
}

public sealed record ModelRequest(
    string Model,
    string SystemPrompt,
    string UserPrompt,
    bool Stream = true,
    float Temperature = 0.15f,
    int ContextWindow = 8192
);
