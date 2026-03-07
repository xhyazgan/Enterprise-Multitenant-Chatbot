using ChatBot.Api.Data;
using ChatBot.Api.Endpoints;
using ChatBot.Api.Middleware;
using ChatBot.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ChatDbContext>("chatbotdb");

builder.Services.AddScoped<TenantContextAccessor>();
builder.Services.AddSingleton<TenantResolver>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<ClaudeAiService>();
builder.Services.AddSingleton<AiServiceFactory>();
builder.Services.AddScoped<ChatSessionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Upsert tenant configs to ensure they match seed data
    var seedConfigs = new[]
    {
        new { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), TenantId = "basiccorp", DisplayName = "BasicCorp", AiProvider = "Claude", AiModel = "claude-haiku-4-5", SystemPrompt = "You are a professional assistant for BasicCorp employees. Be formal, precise, and business-oriented." },
        new { Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"), TenantId = "ssohub", DisplayName = "SSOHub", AiProvider = "Claude", AiModel = "claude-haiku-4-5", SystemPrompt = "You are a technical assistant for SSOHub developers. Be concise, use code examples when relevant, and focus on technical accuracy." },
        new { Id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"), TenantId = "startupxyz", DisplayName = "StartupXYZ", AiProvider = "Claude", AiModel = "claude-haiku-4-5", SystemPrompt = "You are a friendly assistant for StartupXYZ team. Be casual, creative, and encouraging. Use simple language." },
    };

    foreach (var seed in seedConfigs)
    {
        var existing = await db.TenantConfigs.FindAsync(seed.Id);
        if (existing is null)
            continue;
        existing.AiProvider = seed.AiProvider;
        existing.AiModel = seed.AiModel;
        existing.SystemPrompt = seed.SystemPrompt;
    }
    await db.SaveChangesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<TenantContextMiddleware>();

app.MapGet("/api/debug/tenants", async (ChatDbContext db) =>
{
    var tenants = await db.TenantConfigs.ToListAsync();
    return Results.Ok(tenants.Select(t => new { t.TenantId, t.AiProvider, t.AiModel }));
});

app.MapDefaultEndpoints();
app.MapChatEndpoints();

app.Run();
