using System.Text.Json.Serialization;

namespace CodeCli.Models;

public record OllamaRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("stream")] bool Stream = true,
    [property: JsonPropertyName("options")] OllamaOptions? Options = null
);

public record OllamaOptions(
    [property: JsonPropertyName("temperature")] float Temperature = 0.2f,
    [property: JsonPropertyName("num_ctx")] int NumCtx = 8192,
    [property: JsonPropertyName("top_p")] float TopP = 0.9f
);

public record OllamaStreamChunk(
    [property: JsonPropertyName("response")] string Response = "",
    [property: JsonPropertyName("done")] bool Done = false,
    [property: JsonPropertyName("error")] string? Error = null
);

public record OllamaModelInfo(
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("size")] long Size = 0,
    [property: JsonPropertyName("modified_at")] string ModifiedAt = ""
);

public record OllamaModelsResponse(
    [property: JsonPropertyName("models")] List<OllamaModelInfo> Models = null!
);

public class ChatMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}
