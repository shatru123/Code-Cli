using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CodeCli.Models;

namespace CodeCli.Providers;

public sealed class OllamaModelProvider : IModelProvider
{
    private readonly HttpClient _http;

    public OllamaModelProvider(string endpoint)
    {
        Endpoint = endpoint.TrimEnd('/');
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    public string Name => "ollama";

    public string Endpoint { get; }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Endpoint}/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<OllamaModelsResponse>($"{Endpoint}/api/tags", ct);
            return response?.Models.Select(m => m.Name).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = new OllamaRequest(
            Model: request.Model,
            Prompt: request.UserPrompt,
            System: request.SystemPrompt,
            Stream: request.Stream,
            Options: new OllamaOptions(request.Temperature, request.ContextWindow));

        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        string? connectError = null;
        try
        {
            response = await _http.PostAsync($"{Endpoint}/api/generate", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            connectError = ex.Message;
        }

        if (connectError is not null)
        {
            yield return $"\n[ERROR] Cannot reach Ollama: {connectError}\n";
            yield break;
        }

        await using var stream = await response!.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaStreamChunk? chunk;
            try { chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line); }
            catch { continue; }

            if (chunk is null) continue;
            if (!string.IsNullOrEmpty(chunk.Error))
            {
                yield return $"\n[MODEL ERROR] {chunk.Error}\n";
                yield break;
            }

            if (!string.IsNullOrEmpty(chunk.Response))
                yield return chunk.Response;

            if (chunk.Done) break;
        }
    }

    public async Task<string> CompleteAsync(ModelRequest request, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await foreach (var token in StreamCompletionAsync(request with { Stream = true }, ct))
            sb.Append(token);
        return sb.ToString();
    }
}
