using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DepiLms.Services;

public class OpenRouterAiService(HttpClient httpClient, IConfiguration configuration) : IOpenRouterAiService
{
    public async Task<AiAssistantResult> AskAsync(
        IReadOnlyCollection<AiPromptMessage> history,
        string userPrompt,
        string platformContext,
        CancellationToken cancellationToken)
    {
        var options = configuration.GetSection("OpenRouter");
        var apiKey = options["ApiKey"];
        var endpoint = options["Endpoint"] ?? "https://openrouter.ai/api/v1/chat/completions";
        var model = options["Model"] ?? "deepseek/deepseek-chat";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiAssistantResult(
                "OpenRouter is not configured yet. Add your API key with `dotnet user-secrets set \"OpenRouter:ApiKey\" \"YOUR_KEY\"`, then ask again.",
                model,
                null,
                null,
                null);
        }

        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = """
                You are the DEPI LMS AI assistant. Help students, instructors, and admins with course planning,
                lesson explanations, assignment guidance, quiz practice, grade feedback, attendance questions,
                and platform navigation. Be practical, concise, and educational. Do not fabricate grades,
                attendance records, or approvals. If data is missing, explain what can be checked in the LMS.
                """
            },
            new
            {
                role = "system",
                content = platformContext
            }
        };

        messages.AddRange(history.TakeLast(12).Select(x => new { role = x.Role, content = x.Content }));
        messages.Add(new { role = "user", content = userPrompt });

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.TryAddWithoutValidation("HTTP-Referer", options["HttpReferer"] ?? "http://localhost:5000");
        request.Headers.TryAddWithoutValidation("X-OpenRouter-Title", options["AppTitle"] ?? "DEPI LMS");
        request.Content = JsonContent.Create(new
        {
            model,
            messages,
            temperature = 0.35,
            max_tokens = 1200
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new AiAssistantResult(
                $"OpenRouter returned {(int)response.StatusCode}. Check the API key, model, credits, and network. Details: {raw}",
                model,
                null,
                null,
                null);
        }

        var payload = JsonSerializer.Deserialize<OpenRouterChatResponse>(
            raw,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;

        return new AiAssistantResult(
            string.IsNullOrWhiteSpace(content) ? "The model returned an empty response." : content,
            payload?.Model ?? model,
            payload?.Usage?.PromptTokens,
            payload?.Usage?.CompletionTokens,
            payload?.Usage?.TotalTokens);
    }

    private sealed class OpenRouterChatResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<OpenRouterChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenRouterUsage? Usage { get; set; }
    }

    private sealed class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterMessage? Message { get; set; }
    }

    private sealed class OpenRouterMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class OpenRouterUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }
    }
}
