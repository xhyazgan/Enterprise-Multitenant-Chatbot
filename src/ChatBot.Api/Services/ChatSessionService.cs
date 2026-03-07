using ChatBot.Api.Data;
using ChatBot.Api.Data.Entities;
using ChatBot.Api.Middleware;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Services;

public class ChatSessionService(
    ChatDbContext db,
    TenantContextAccessor tenantAccessor,
    TenantResolver tenantResolver,
    AiServiceFactory aiServiceFactory)
{
    private TenantContext Tenant => tenantAccessor.TenantContext
        ?? throw new InvalidOperationException("Tenant context not available.");

    public async Task<ChatSession> CreateSessionAsync(string title)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            TenantId = Tenant.TenantId,
            UserId = Tenant.UserId,
            Title = title
        };

        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task<List<ChatSession>> GetSessionsAsync()
    {
        return await db.ChatSessions
            .Where(s => s.TenantId == Tenant.TenantId && s.UserId == Tenant.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId)
    {
        return await db.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId
                && s.TenantId == Tenant.TenantId
                && s.UserId == Tenant.UserId);
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        var session = await db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId
                && s.TenantId == Tenant.TenantId
                && s.UserId == Tenant.UserId);

        if (session is null) return false;

        session.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ChatMessage> SendMessageAsync(Guid? sessionId, string userMessage)
    {
        var tenantConfig = await tenantResolver.ResolveAsync(Tenant.TenantId)
            ?? throw new InvalidOperationException($"Tenant '{Tenant.TenantId}' not found.");

        ChatSession session;
        if (sessionId.HasValue)
        {
            session = await db.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value
                    && s.TenantId == Tenant.TenantId
                    && s.UserId == Tenant.UserId)
                ?? throw new InvalidOperationException("Session not found.");
        }
        else
        {
            var title = userMessage.Length > 50 ? userMessage[..50] + "..." : userMessage;
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                TenantId = Tenant.TenantId,
                UserId = Tenant.UserId,
                Title = title,
                Messages = new List<ChatMessage>()
            };
            db.ChatSessions.Add(session);
        }

        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "user",
            Content = userMessage
        };
        db.ChatMessages.Add(userMsg);

        var aiService = aiServiceFactory.GetService(tenantConfig.AiProvider);
        var aiResponse = await aiService.SendMessageAsync(
            tenantConfig.AiModel,
            tenantConfig.SystemPrompt,
            session.Messages.ToList(),
            userMessage);

        var assistantMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "assistant",
            Content = aiResponse
        };
        db.ChatMessages.Add(assistantMsg);

        await db.SaveChangesAsync();
        return assistantMsg;
    }
}
