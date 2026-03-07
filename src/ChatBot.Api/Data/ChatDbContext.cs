using ChatBot.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<TenantConfig> TenantConfigs => Set<TenantConfig>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId).IsUnique();
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.UserId });
            entity.HasQueryFilter(e => e.DeletedAt == null);
            entity.HasMany(e => e.Messages)
                  .WithOne(m => m.Session)
                  .HasForeignKey(m => m.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantConfig>().HasData(
            new TenantConfig
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                TenantId = "basiccorp",
                DisplayName = "BasicCorp",
                AiProvider = "Claude",
                AiModel = "claude-haiku-4-5",
                SystemPrompt = "You are a professional assistant for BasicCorp employees. Be formal, precise, and business-oriented."
            },
            new TenantConfig
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                TenantId = "ssohub",
                DisplayName = "SSOHub",
                AiProvider = "Claude",
                AiModel = "claude-haiku-4-5",
                SystemPrompt = "You are a technical assistant for SSOHub developers. Be concise, use code examples when relevant, and focus on technical accuracy."
            },
            new TenantConfig
            {
                Id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                TenantId = "startupxyz",
                DisplayName = "StartupXYZ",
                AiProvider = "Claude",
                AiModel = "claude-haiku-4-5",
                SystemPrompt = "You are a friendly assistant for StartupXYZ team. Be casual, creative, and encouraging. Use simple language."
            }
        );
    }
}
