using ChatBot.Api.Services;

namespace ChatBot.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat");

        group.MapPost("/message", async (SendMessageRequest request, ChatSessionService chatService, ILogger<ChatSessionService> logger, IHostEnvironment env) =>
        {
            try
            {
                var response = await chatService.SendMessageAsync(request.SessionId, request.Message);
                return Results.Ok(new
                {
                    sessionId = response.SessionId,
                    message = new
                    {
                        id = response.Id,
                        role = response.Role,
                        content = response.Content,
                        createdAt = response.CreatedAt
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process message");
                var errorDetail = env.IsDevelopment() ? $"{ex.GetType().Name}: {ex.Message}" : "Failed to generate response.";
                return Results.Json(new { error = errorDetail }, statusCode: 502);
            }
        });

        group.MapGet("/sessions", async (ChatSessionService chatService) =>
        {
            var sessions = await chatService.GetSessionsAsync();
            return Results.Ok(sessions.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                createdAt = s.CreatedAt
            }));
        });

        group.MapGet("/sessions/{id:guid}/messages", async (Guid id, ChatSessionService chatService) =>
        {
            var session = await chatService.GetSessionWithMessagesAsync(id);
            if (session is null)
                return Results.NotFound(new { error = "Session not found." });

            return Results.Ok(new
            {
                id = session.Id,
                title = session.Title,
                messages = session.Messages.Select(m => new
                {
                    id = m.Id,
                    role = m.Role,
                    content = m.Content,
                    createdAt = m.CreatedAt
                })
            });
        });

        group.MapDelete("/sessions/{id:guid}", async (Guid id, ChatSessionService chatService) =>
        {
            var deleted = await chatService.DeleteSessionAsync(id);
            return deleted ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Session not found." });
        });
    }
}

public record SendMessageRequest(Guid? SessionId, string Message);
