using System.Text.RegularExpressions;
using ChatBot.Api.Data;
using ChatBot.Api.Data.Entities;
using ChatBot.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Endpoints;

public static partial class TenantEndpoints
{
    [GeneratedRegex("^[a-z][a-z0-9-]{1,30}$")]
    private static partial Regex TenantIdPattern();

    public static void MapTenantEndpoints(this WebApplication app)
    {
        // Public endpoint - list active tenants (no auth required)
        app.MapGet("/api/tenants", async (ChatDbContext db) =>
        {
            var tenants = await db.TenantConfigs
                .Where(t => t.IsActive)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new
                {
                    tenantId = t.TenantId,
                    displayName = t.DisplayName,
                    color = t.Color,
                    description = t.Description,
                    aiModel = t.AiModel
                })
                .ToListAsync();

            return Results.Ok(tenants);
        });

        // Admin group with shared auth filter
        var adminGroup = app.MapGroup("/api/admin")
            .AddEndpointFilter(async (context, next) =>
            {
                var httpContext = context.HttpContext;
                var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                var adminKey = config["AdminApiKey"];

                if (string.IsNullOrEmpty(adminKey))
                    return Results.Json(new { error = "Admin API key not configured." }, statusCode: 500);

                var requestKey = httpContext.Request.Headers["X-Admin-Key"].FirstOrDefault();
                if (requestKey != adminKey)
                    return Results.Unauthorized();

                return await next(context);
            });

        // Create tenant
        adminGroup.MapPost("/tenants", async (
            CreateTenantRequest request,
            ChatDbContext db,
            KeycloakAdminService keycloakService,
            TenantResolver tenantResolver) =>
        {
            if (!TenantIdPattern().IsMatch(request.TenantId))
                return Results.BadRequest(new { error = "Invalid tenant ID. Must start with lowercase letter, contain only lowercase letters, numbers, and hyphens (2-31 chars)." });

            var exists = await db.TenantConfigs.AnyAsync(t => t.TenantId == request.TenantId);
            if (exists)
                return Results.Conflict(new { error = $"Tenant '{request.TenantId}' already exists." });

            try
            {
                await keycloakService.CreateRealmAsync(request.TenantId, request.DisplayName);

                var tenant = new TenantConfig
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    DisplayName = request.DisplayName,
                    AiProvider = request.AiProvider,
                    AiModel = request.AiModel,
                    SystemPrompt = request.SystemPrompt,
                    Color = request.Color ?? "#3B82F6",
                    Description = request.Description ?? "",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.TenantConfigs.Add(tenant);
                await db.SaveChangesAsync();
                tenantResolver.InvalidateAll();

                return Results.Created($"/api/admin/tenants/{tenant.TenantId}", new
                {
                    tenantId = tenant.TenantId,
                    displayName = tenant.DisplayName,
                    aiProvider = tenant.AiProvider,
                    aiModel = tenant.AiModel,
                    color = tenant.Color,
                    description = tenant.Description,
                    isActive = tenant.IsActive,
                    createdAt = tenant.CreatedAt
                });
            }
            catch (HttpRequestException ex)
            {
                return Results.Json(new { error = $"Failed to create Keycloak realm: {ex.Message}" }, statusCode: 502);
            }
        });

        // List all tenants (including inactive)
        adminGroup.MapGet("/tenants", async (ChatDbContext db) =>
        {
            var tenants = await db.TenantConfigs
                .OrderBy(t => t.CreatedAt)
                .Select(t => new
                {
                    tenantId = t.TenantId,
                    displayName = t.DisplayName,
                    aiProvider = t.AiProvider,
                    aiModel = t.AiModel,
                    color = t.Color,
                    description = t.Description,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(tenants);
        });

        // Update tenant
        adminGroup.MapPut("/tenants/{tenantId}", async (
            string tenantId,
            UpdateTenantRequest request,
            ChatDbContext db,
            TenantResolver tenantResolver) =>
        {
            var tenant = await db.TenantConfigs.FirstOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant is null)
                return Results.NotFound(new { error = $"Tenant '{tenantId}' not found." });

            if (request.DisplayName is not null) tenant.DisplayName = request.DisplayName;
            if (request.AiProvider is not null) tenant.AiProvider = request.AiProvider;
            if (request.AiModel is not null) tenant.AiModel = request.AiModel;
            if (request.SystemPrompt is not null) tenant.SystemPrompt = request.SystemPrompt;
            if (request.Color is not null) tenant.Color = request.Color;
            if (request.Description is not null) tenant.Description = request.Description;
            if (request.IsActive.HasValue) tenant.IsActive = request.IsActive.Value;

            await db.SaveChangesAsync();
            tenantResolver.InvalidateCache(tenantId);

            return Results.Ok(new
            {
                tenantId = tenant.TenantId,
                displayName = tenant.DisplayName,
                aiProvider = tenant.AiProvider,
                aiModel = tenant.AiModel,
                color = tenant.Color,
                description = tenant.Description,
                isActive = tenant.IsActive,
                createdAt = tenant.CreatedAt
            });
        });

        // Deactivate tenant (soft delete)
        adminGroup.MapDelete("/tenants/{tenantId}", async (
            string tenantId,
            ChatDbContext db,
            TenantResolver tenantResolver) =>
        {
            var tenant = await db.TenantConfigs.FirstOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant is null)
                return Results.NotFound(new { error = $"Tenant '{tenantId}' not found." });

            tenant.IsActive = false;
            await db.SaveChangesAsync();
            tenantResolver.InvalidateCache(tenantId);

            return Results.NoContent();
        });
    }
}

public record CreateTenantRequest(
    string TenantId,
    string DisplayName,
    string AiProvider,
    string AiModel,
    string? SystemPrompt,
    string? Color,
    string? Description
);

public record UpdateTenantRequest(
    string? DisplayName,
    string? AiProvider,
    string? AiModel,
    string? SystemPrompt,
    string? Color,
    string? Description,
    bool? IsActive
);
