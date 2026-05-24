namespace ClaudeApiDemo.Models;

// --- Chat ---

public record ChatMessage(string Role, string Content);

public class ChatRequest
{
    /// <summary>Conversation history. Last item is the new user message.</summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>Optional system prompt to set the assistant's persona/context.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Max tokens to generate (default 1024).</summary>
    public int MaxTokens { get; set; } = 1024;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string StopReason { get; set; } = string.Empty;
}

// --- Summarize ---

public class SummarizeRequest
{
    /// <summary>The text to summarize.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Brief | Detailed | Bullet points</summary>
    public string Style { get; set; } = "Brief";
}

// --- Classify ---

public class ClassifyRequest
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Candidate labels to classify into.</summary>
    public List<string> Labels { get; set; } = new();
}

public class ClassifyResponse
{
    public string Label { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}
