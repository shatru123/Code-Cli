using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCli.Providers;

public class OpenAiCompatibleModelProvider : IModelProvider
{
    private readonly HttpClient _http;

    public OpenAiCompatibleModelProvider(string endpoint, string? apiKey = null, string name = "openai-compatible")
    {
        Name = name;
        Endpoint = endpoint.TrimEnd('/');
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public string Name { get; }

    public string Endpoint { get; }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Endpoint}/models", ct);
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
            var response = await _http.GetFromJsonAsync<OpenAiModelListResponse>($"{Endpoint}/models", ct);
            return response?.Data.Select(x => x.Id).ToList() ?? [];
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
        var body = new OpenAiChatRequest(
            request.Model,
            [new OpenAiMessage("system", request.SystemPrompt), new OpenAiMessage("user", request.UserPrompt)],
            request.Stream,
            request.Temperature);

        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/chat/completions")
        {
            Content = content
        };

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        if (!request.Stream)
        {
            var payload = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: ct);
            var text = payload?.Choices.FirstOrDefault()?.Message?.Content;
            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") break;

            string? contentToken = null;
            try
            {
                var chunk = JsonSerializer.Deserialize<OpenAiChatStreamChunk>(data);
                contentToken = chunk?.Choices.FirstOrDefault()?.Delta?.Content;
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(contentToken))
                yield return contentToken;
        }
    }

    public async Task<string> CompleteAsync(ModelRequest request, CancellationToken ct = default)
    {
        var payload = new OpenAiChatRequest(
            request.Model,
            [new OpenAiMessage("system", request.SystemPrompt), new OpenAiMessage("user", request.UserPrompt)],
            false,
            request.Temperature);

        var response = await _http.PostAsJsonAsync($"{Endpoint}/chat/completions", payload, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: ct);
        return body?.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }
}

internal sealed record OpenAiChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiMessage> Messages,
    [property: JsonPropertyName("stream")] bool Stream,
    [property: JsonPropertyName("temperature")] float Temperature
);

internal sealed record OpenAiMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

internal sealed record OpenAiModelListResponse(
    [property: JsonPropertyName("data")] List<OpenAiModelItem> Data
);

internal sealed record OpenAiModelItem(
    [property: JsonPropertyName("id")] string Id
);

internal sealed record OpenAiChatResponse(
    [property: JsonPropertyName("choices")] List<OpenAiChoice> Choices
);

internal sealed record OpenAiChoice(
    [property: JsonPropertyName("message")] OpenAiMessage? Message
);

internal sealed record OpenAiChatStreamChunk(
    [property: JsonPropertyName("choices")] List<OpenAiStreamChoice> Choices
);

internal sealed record OpenAiStreamChoice(
    [property: JsonPropertyName("delta")] OpenAiStreamDelta? Delta
);

internal sealed record OpenAiStreamDelta(
    [property: JsonPropertyName("content")] string? Content
);
