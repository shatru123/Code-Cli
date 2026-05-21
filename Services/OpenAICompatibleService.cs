using System.Runtime.CompilerServices;

namespace CodeCli.Services;

/// <summary>
/// OpenAI-compatible provider scaffold.
/// Supports LM Studio, llama.cpp server, vLLM and OpenAI-compatible APIs.
/// </summary>
public sealed class OpenAICompatibleService : IAIService
{
    private readonly string _endpoint;

    public string ProviderName => "openai-compatible";

    public string Model { get; set; }

    public OpenAICompatibleService(string endpoint, string model)
    {
        _endpoint = endpoint;
        Model = model;
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<List<ModelInfo>> GetAvailableModelsAsync()
        => Task.FromResult(new List<ModelInfo>
        {
            new(Model, "Configured OpenAI-compatible endpoint", true)
        });

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return "[OpenAI-compatible provider connected] ";
        yield return $"Endpoint: {_endpoint}\n";

        await Task.CompletedTask;
    }
}
