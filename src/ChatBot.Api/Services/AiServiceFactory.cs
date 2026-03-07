namespace ChatBot.Api.Services;

public class AiServiceFactory(OpenAiService openAiService, ClaudeAiService claudeAiService)
{
    public IAiService GetService(string provider) => provider.ToLowerInvariant() switch
    {
        "openai" => openAiService,
        "claude" => claudeAiService,
        _ => throw new ArgumentException($"Unknown AI provider: {provider}")
    };
}
