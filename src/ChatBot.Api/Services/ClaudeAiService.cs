using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using ChatMessage = ChatBot.Api.Data.Entities.ChatMessage;

namespace ChatBot.Api.Services;

public class ClaudeAiService : IAiService
{
    private readonly AnthropicClient? _client;

    public ClaudeAiService(IConfiguration configuration)
    {
        var apiKey = configuration["Claude:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
            _client = new AnthropicClient(apiKey);
    }

    public async Task<string> SendMessageAsync(string model, string? systemPrompt, IList<ChatMessage> history, string userMessage)
    {
        if (_client is null)
            throw new InvalidOperationException("Claude API key is not configured. Set Claude:ApiKey in configuration.");

        var messages = new List<Message>();

        foreach (var msg in history)
        {
            messages.Add(new Message
            {
                Role = msg.Role == "assistant" ? RoleType.Assistant : RoleType.User,
                Content = [new TextContent { Text = msg.Content }]
            });
        }

        messages.Add(new Message
        {
            Role = RoleType.User,
            Content = [new TextContent { Text = userMessage }]
        });

        var parameters = new MessageParameters
        {
            Model = model,
            MaxTokens = 1024,
            Messages = messages
        };

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            parameters.SystemMessage = systemPrompt;
        }

        var response = await _client.Messages.GetClaudeMessageAsync(parameters);
        return response.Content.OfType<TextContent>().FirstOrDefault()?.Text
               ?? "No response generated.";
    }
}
