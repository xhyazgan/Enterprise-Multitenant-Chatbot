namespace ChatBot.Api.Data.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public required string TenantId { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
