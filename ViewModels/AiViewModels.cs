using DepiLms.Models;

namespace DepiLms.ViewModels;

public class AiAssistantViewModel
{
    public int? ConversationId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public IReadOnlyCollection<AiMessage> Messages { get; set; } = [];
    public string Model { get; set; } = "deepseek/deepseek-chat";
}
