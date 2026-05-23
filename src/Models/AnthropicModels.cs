using System.Text.Json.Serialization;

namespace CodeCli.Models;

// ── Request ───────────────────────────────────────────────────────────────────

public record AnthropicUserMessage(
    [property: JsonPropertyName("role")]    string Role    = "user",
    [property: JsonPropertyName("content")] string Content = ""
);

public record AnthropicRequest(
    [property: JsonPropertyName("model")]      string                     Model,
    [property: JsonPropertyName("max_tokens")] int                        MaxTokens,
    [property: JsonPropertyName("system")]     string                     System,
    [property: JsonPropertyName("messages")]   List<AnthropicUserMessage> Messages,
    [property: JsonPropertyName("stream")]     bool                       Stream = true
);

// ── SSE streaming events ──────────────────────────────────────────────────────

public record SseEvent(
    [property: JsonPropertyName("type")]  string          Type  = "",
    [property: JsonPropertyName("index")] int             Index = 0,
    [property: JsonPropertyName("delta")] SseDelta?       Delta = null,
    [property: JsonPropertyName("error")] SseErrorDetail? Error = null
);

public record SseDelta(
    [property: JsonPropertyName("type")] string Type = "",
    [property: JsonPropertyName("text")] string Text = ""
);

public record SseErrorDetail(
    [property: JsonPropertyName("type")]    string Type    = "",
    [property: JsonPropertyName("message")] string Message = ""
);

// ── Non-streaming response (error envelope) ───────────────────────────────────

public record AnthropicErrorResponse(
    [property: JsonPropertyName("error")] SseErrorDetail? Error = null
);
