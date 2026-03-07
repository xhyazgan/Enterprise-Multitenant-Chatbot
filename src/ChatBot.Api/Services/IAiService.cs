using ChatBot.Api.Data.Entities;

namespace ChatBot.Api.Services;

public interface IAiService
{
    Task<string> SendMessageAsync(string model, string? systemPrompt, IList<ChatMessage> history, string userMessage);
}
