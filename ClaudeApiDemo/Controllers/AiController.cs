using ClaudeApiDemo.Models;
using ClaudeApiDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeApiDemo.Controllers;

[ApiController]
[Route("api/ai")]
[Produces("application/json")]
public class AiController : ControllerBase
{
    private readonly ClaudeService _claude;
    private readonly ILogger<AiController> _logger;

    public AiController(ClaudeService claude, ILogger<AiController> logger)
    {
        _claude = claude;
        _logger = logger;
    }

    // ── POST /api/ai/chat ────────────────────────────────────────────────────

    /// <summary>
    /// Multi-turn chat. Pass the full conversation history each request.
    /// </summary>
    /// <remarks>
    /// Example body:
    /// <code>
    /// {
    ///   "systemPrompt": "You are a helpful assistant.",
    ///   "messages": [
    ///     { "role": "user", "content": "What is the capital of France?" }
    ///   ]
    /// }
    /// </code>
    /// </remarks>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        if (request.Messages is not { Count: > 0 })
            return BadRequest("At least one message is required.");

        _logger.LogInformation("Chat request: {Count} messages", request.Messages.Count);
        var result = await _claude.ChatAsync(request, ct);
        return Ok(result);
    }

    // ── POST /api/ai/summarize ───────────────────────────────────────────────

    /// <summary>
    /// Summarize a block of text. Style can be "Brief", "Detailed", or "Bullet points".
    /// </summary>
    [HttpPost("summarize")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Summarize(
        [FromBody] SummarizeRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text is required.");

        var summary = await _claude.SummarizeAsync(request, ct);
        return Ok(new { summary });
    }

    // ── POST /api/ai/classify ────────────────────────────────────────────────

    /// <summary>
    /// Zero-shot text classification. Provide the text and a list of candidate labels.
    /// </summary>
    /// <remarks>
    /// Example body:
    /// <code>
    /// {
    ///   "text": "My order arrived broken and customer service was rude.",
    ///   "labels": ["complaint", "refund request", "general inquiry", "compliment"]
    /// }
    /// </code>
    /// </remarks>
    [HttpPost("classify")]
    [ProducesResponseType(typeof(ClassifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Classify(
        [FromBody] ClassifyRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text is required.");

        if (request.Labels is not { Count: > 1 })
            return BadRequest("At least two labels are required.");

        var result = await _claude.ClassifyAsync(request, ct);
        return Ok(result);
    }

    // ── GET /api/ai/health ───────────────────────────────────────────────────

    /// <summary>Quick health check — confirms the API key is configured.</summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health([FromServices] IConfiguration config)
    {
        var hasKey = !string.IsNullOrEmpty(config["Anthropic:ApiKey"]);
        return Ok(new
        {
            status  = "ok",
            apiKey  = hasKey ? "configured" : "MISSING",
            model   = "claude-sonnet-4-20250514",
            version = "1.0.0",
        });
    }
}
