using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CodeCli.Models;

namespace CodeCli.Providers;

/// <summary>
/// Anthropic Claude provider implementing IModelProvider.
/// Streams responses via Server-Sent Events (SSE).
/// API key stored in config; set with: code-cli config --set-key sk-ant-...
/// </summary>
public sealed class ClaudeModelProvider : IModelProvider
{
    private readonly HttpClient _http;
    private readonly string     _apiKey;

    private const string BaseUrl    = "https://api.anthropic.com";
    private const string ApiVersion = "2023-06-01";
    private const int    MaxTokens  = 8192;

    public string Name     => "claude";
    public string Endpoint => BaseUrl;

    public ClaudeModelProvider(string apiKey)
    {
        _apiKey = apiKey ?? string.Empty;
        _http   = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _http.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);

        if (!string.IsNullOrWhiteSpace(_apiKey))
            _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
    }

    // ── Health check ──────────────────────────────────────────────────────────

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return false;
        try
        {
            // A lightweight GET — 200 or 4xx both confirm connectivity
            var resp = await _http.GetAsync($"{BaseUrl}/v1/models", ct);
            return (int)resp.StatusCode < 500;
        }
        catch { return false; }
    }

    // ── Model list ────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(
        [
            "claude-sonnet-4-5",
            "claude-opus-4-5",
            "claude-haiku-4-5-20251001",
        ]);

    // ── Streaming completion ──────────────────────────────────────────────────

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            yield return "\n[ERROR] No Anthropic API key configured.\n"
                       + "Run: code-cli config --set-key sk-ant-...\n";
            yield break;
        }

        var body = new AnthropicRequest(
            Model:     request.Model,
            MaxTokens: MaxTokens,
            System:    request.SystemPrompt,
            Messages:  [new AnthropicUserMessage("user", request.UserPrompt)],
            Stream:    true
        );

        var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/messages")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        HttpResponseMessage? resp   = null;
        string?             errMsg = null;

        try
        {
            resp = await _http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                try
                {
                    var errObj = JsonSerializer.Deserialize<AnthropicErrorResponse>(raw);
                    errMsg = errObj?.Error?.Message ?? $"HTTP {(int)resp.StatusCode}";
                }
                catch { errMsg = $"HTTP {(int)resp.StatusCode}: {raw}"; }
            }
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested) { yield break; }
        catch (Exception ex) { errMsg = ex.Message; }

        if (errMsg is not null)
        {
            yield return $"\n[ERROR] Anthropic API: {errMsg}\n";
            yield break;
        }

        // ── Parse SSE stream ──────────────────────────────────────────────────
        await using var stream = await resp!.Content.ReadAsStreamAsync(ct);
        using  var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            string? line;
            try   { line = await reader.ReadLineAsync(ct); }
            catch { break; }

            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..].Trim();
            if (data == "[DONE]") break;

            SseEvent? evt;
            try   { evt = JsonSerializer.Deserialize<SseEvent>(data); }
            catch { continue; }

            if (evt is null) continue;

            if (evt.Type == "error" && evt.Error is not null)
            {
                yield return $"\n[MODEL ERROR] {evt.Error.Message}\n";
                yield break;
            }

            if (evt.Type        == "content_block_delta"
             && evt.Delta?.Type == "text_delta"
             && !string.IsNullOrEmpty(evt.Delta.Text))
            {
                yield return evt.Delta.Text;
            }
        }
    }

    // ── Non-streaming ─────────────────────────────────────────────────────────

    public async Task<string> CompleteAsync(ModelRequest request, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await foreach (var token in StreamCompletionAsync(request with { Stream = true }, ct))
            sb.Append(token);
        return sb.ToString();
    }
}
