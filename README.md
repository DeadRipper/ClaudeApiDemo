# Claude AI Demo — ASP.NET Core Web API

A minimal but production-shaped ASP.NET Core 8 REST API that integrates
with the **Anthropic Claude API**. No third-party SDK required — just
`HttpClient` and the standard Anthropic REST endpoint.

---

## Features

| Endpoint | Description |
|---|---|
| `POST /api/ai/chat` | Multi-turn conversation with system prompt support |
| `POST /api/ai/summarize` | Summarize text (Brief / Detailed / Bullet points) |
| `POST /api/ai/classify` | Zero-shot text classification |
| `GET  /api/ai/health` | Confirm service is alive and API key is set |

---

## Quick Start

### 1. Set your API key

**Option A — appsettings.json** (dev only, don't commit):
```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-..."
  }
}
```

**Option B — User Secrets** (recommended for local dev):
```bash
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
```

**Option C — Environment variable** (recommended for production):
```bash
export Anthropic__ApiKey="sk-ant-..."
```

### 2. Run the API

```bash
cd ClaudeApiDemo
dotnet run
```

Swagger UI opens at **http://localhost:5000** (root URL).

---

## Example Requests

### Chat
```bash
curl -X POST http://localhost:5000/api/ai/chat \
  -H "Content-Type: application/json" \
  -d '{
    "systemPrompt": "You are a concise assistant.",
    "messages": [
      { "role": "user", "content": "What is .NET?" }
    ]
  }'
```

### Summarize
```bash
curl -X POST http://localhost:5000/api/ai/summarize \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Your long text here...",
    "style": "Bullet points"
  }'
```

### Classify
```bash
curl -X POST http://localhost:5000/api/ai/classify \
  -H "Content-Type: application/json" \
  -d '{
    "text": "My order never arrived.",
    "labels": ["complaint", "refund request", "general inquiry", "compliment"]
  }'
```

---

## Project Structure

```
ClaudeApiDemo/
├── Controllers/
│   └── AiController.cs     # Route handlers
├── Models/
│   └── Requests.cs         # Request / response DTOs
├── Services/
│   └── ClaudeService.cs    # Anthropic API client
├── Program.cs              # DI + middleware setup
└── appsettings.json
```

---

## Key Design Decisions

- **No SDK** — Uses raw `HttpClient` so you see exactly what's sent to the API.
  Swap in the official `Anthropic.SDK` NuGet package for production if preferred.
- **Typed HttpClient** — `ClaudeService` is registered via `AddHttpClient<T>()` for
  proper lifecycle management and easy mocking in tests.
- **Cancellation tokens** — Threaded through every async call so requests abort
  cleanly when the client disconnects.

---

## Next Steps

- Add **streaming** responses via `text/event-stream` (SSE)
- Add **authentication** (API keys, JWT) to your own endpoints
- Wire up **structured output** / tool use for agentic workflows
- Add integration tests with a mocked `HttpClient`
