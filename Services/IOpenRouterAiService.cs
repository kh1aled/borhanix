namespace DepiLms.Services;

public record AiPromptMessage(string Role, string Content);
public record AiAssistantResult(string Content, string Model, int? PromptTokens, int? CompletionTokens, int? TotalTokens);

public interface IOpenRouterAiService
{
    Task<AiAssistantResult> AskAsync(
        IReadOnlyCollection<AiPromptMessage> history,
        string userPrompt,
        string platformContext,
        CancellationToken cancellationToken);
}
