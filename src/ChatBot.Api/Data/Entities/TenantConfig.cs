namespace ChatBot.Api.Data.Entities;

public class TenantConfig
{
    public Guid Id { get; set; }
    public required string TenantId { get; set; }
    public required string DisplayName { get; set; }
    public required string AiProvider { get; set; }
    public required string AiModel { get; set; }
    public string? SystemPrompt { get; set; }
}
