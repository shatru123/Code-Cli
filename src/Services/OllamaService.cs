using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CodeCli.Models;

namespace CodeCli.Services;

public class OllamaService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public OllamaService(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    // ── Health check ──────────────────────────────────────────────────────────

    public async Task<bool> IsRunningAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{_baseUrl}/api/tags");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Model discovery ───────────────────────────────────────────────────────

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<OllamaModelsResponse>($"{_baseUrl}/api/tags");
            return resp?.Models.Select(m => m.Name).ToList() ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    // ── Streaming completion ──────────────────────────────────────────────────

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new OllamaRequest(
            Model: model,
            Prompt: userPrompt,
            System: systemPrompt,
            Stream: true,
            Options: new OllamaOptions(Temperature: 0.15f, NumCtx: 8192)
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        string? connectError = null;
        try
        {
            response = await _http.PostAsync($"{_baseUrl}/api/generate", content, ct);
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

    // ── Non-streaming (returns full response) ─────────────────────────────────

    public async Task<string> CompleteAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await foreach (var token in StreamCompletionAsync(model, systemPrompt, userPrompt, ct))
            sb.Append(token);
        return sb.ToString();
    }
}
