using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeApiDemo.Models;

namespace ClaudeApiDemo.Services;

// ── Anthropic API shapes ────────────────────────────────────────────────────

file record AnthropicMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

file class AnthropicRequest
{
    [JsonPropertyName("model")]    public string Model     { get; set; } = "claude-sonnet-4-20250514";
    [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; } = 1024;
    [JsonPropertyName("system")]   public string? System  { get; set; }
    [JsonPropertyName("messages")] public List<AnthropicMessage> Messages { get; set; } = new();
}

file class AnthropicResponse
{
    [JsonPropertyName("content")]    public List<ContentBlock> Content    { get; set; } = new();
    [JsonPropertyName("usage")]      public Usage Usage                   { get; set; } = new();
    [JsonPropertyName("stop_reason")] public string StopReason            { get; set; } = string.Empty;
}

file class ContentBlock
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
}

file class Usage
{
    [JsonPropertyName("input_tokens")]  public int InputTokens  { get; set; }
    [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
}

// ── Service ─────────────────────────────────────────────────────────────────

public class ClaudeService
{
    private readonly HttpClient _http;
    private readonly ILogger<ClaudeService> _logger;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    public ClaudeService(HttpClient http, IConfiguration config, ILogger<ClaudeService> logger)
    {
        _http   = http;
        _logger = logger;

        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");

        _http.BaseAddress = new Uri("https://api.anthropic.com/");
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ── Core helper ──────────────────────────────────────────────────────────

    private async Task<AnthropicResponse> SendAsync(
        AnthropicRequest request,
        CancellationToken ct = default)
    {
        var body    = JsonSerializer.Serialize(request, _json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        _logger.LogDebug("→ Anthropic: {Body}", body);

        var response = await _http.PostAsync("v1/messages", content, ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("← Anthropic ({Status}): {Raw}", response.StatusCode, raw);

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<AnthropicResponse>(raw, _json)
               ?? throw new InvalidOperationException("Empty response from Anthropic API.");
    }

    // ── Public methods ───────────────────────────────────────────────────────

    /// <summary>Multi-turn chat with optional system prompt.</summary>
    public async Task<ChatResponse> ChatAsync(ChatRequest req, CancellationToken ct = default)
    {
        var request = new AnthropicRequest
        {
            MaxTokens = req.MaxTokens,
            System    = req.SystemPrompt,
            Messages  = req.Messages
                .Select(m => new AnthropicMessage(m.Role, m.Content))
                .ToList(),
        };

        var result = await SendAsync(request, ct);
        var text   = result.Content.FirstOrDefault(b => b.Type == "text")?.Text ?? string.Empty;

        return new ChatResponse
        {
            Reply        = text,
            InputTokens  = result.Usage.InputTokens,
            OutputTokens = result.Usage.OutputTokens,
            StopReason   = result.StopReason,
        };
    }

    /// <summary>Summarize arbitrary text with a chosen style.</summary>
    public async Task<string> SummarizeAsync(SummarizeRequest req, CancellationToken ct = default)
    {
        var styleGuide = req.Style.ToLower() switch
        {
            "detailed"      => "Write a detailed, multi-paragraph summary.",
            "bullet points" => "Summarize using concise bullet points.",
            _               => "Write a brief 2-3 sentence summary.",
        };

        var request = new AnthropicRequest
        {
            System   = $"You are a summarization assistant. {styleGuide} Return only the summary, no preamble.",
            Messages = [new("user", req.Text)],
        };

        var result = await SendAsync(request, ct);
        return result.Content.FirstOrDefault(b => b.Type == "text")?.Text ?? string.Empty;
    }

    /// <summary>Zero-shot text classification.</summary>
    public async Task<ClassifyResponse> ClassifyAsync(ClassifyRequest req, CancellationToken ct = default)
    {
        var labels = string.Join(", ", req.Labels);
        var prompt =
            $"""
            Classify the following text into exactly ONE of these labels: {labels}.

            Text:
            {req.Text}

            Respond with valid JSON only — no markdown, no explanation outside JSON:
            {{"label": "<chosen label>", "reasoning": "<one sentence why>"}}
            """;

        var request = new AnthropicRequest
        {
            System   = "You are a text classification assistant. Output only valid JSON.",
            Messages = [new("user", prompt)],
        };

        var result = await SendAsync(request, ct);
        var raw    = result.Content.FirstOrDefault(b => b.Type == "text")?.Text ?? "{}";

        // Strip accidental markdown fences
        raw = raw.Trim().TrimStart('`').TrimEnd('`');
        if (raw.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            raw = raw[4..].TrimStart();

        return JsonSerializer.Deserialize<ClassifyResponse>(raw, _json)
               ?? new ClassifyResponse { Label = "unknown" };
    }
}
