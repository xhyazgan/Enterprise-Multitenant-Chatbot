using OpenAI;
using OpenAI.Chat;
using AiChatMessage = OpenAI.Chat.ChatMessage;
using ChatMessage = ChatBot.Api.Data.Entities.ChatMessage;

namespace ChatBot.Api.Services;

public class OpenAiService : IAiService
{
    private readonly OpenAIClient? _client;

    public OpenAiService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
            _client = new OpenAIClient(apiKey);
    }

    public async Task<string> SendMessageAsync(string model, string? systemPrompt, IList<ChatMessage> history, string userMessage)
    {
        if (_client is null)
            throw new InvalidOperationException("OpenAI API key is not configured. Set OpenAI:ApiKey in configuration.");

        var chatClient = _client.GetChatClient(model);

        var messages = new List<AiChatMessage>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(AiChatMessage.CreateSystemMessage(systemPrompt));

        foreach (var msg in history)
        {
            messages.Add(msg.Role switch
            {
                "user" => AiChatMessage.CreateUserMessage(msg.Content),
                "assistant" => AiChatMessage.CreateAssistantMessage(msg.Content),
                _ => AiChatMessage.CreateUserMessage(msg.Content)
            });
        }

        messages.Add(AiChatMessage.CreateUserMessage(userMessage));

        var completion = await chatClient.CompleteChatAsync(messages);
        return completion.Value.Content[0].Text;
    }
}
