namespace ChatBot.Api.Data.Entities;

public class TenantConfig
{
    public Guid Id { get; set; }
    public required string TenantId { get; set; }
    public required string DisplayName { get; set; }
    public required string AiProvider { get; set; }
    public required string AiModel { get; set; }
    public string? SystemPrompt { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
