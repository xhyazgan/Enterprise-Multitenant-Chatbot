namespace ChatBot.Api.Middleware;

public class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContextAccessor accessor)
    {
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var userEmail = context.Request.Headers["X-User-Email"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        {
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/alive") ||
                context.Request.Path.StartsWithSegments("/api/debug") ||
                context.Request.Path.StartsWithSegments("/api/tenants") ||
                context.Request.Path.StartsWithSegments("/api/admin"))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "X-Tenant-Id and X-User-Id headers are required." });
            return;
        }

        accessor.TenantContext = new TenantContext
        {
            TenantId = tenantId,
            UserId = userId,
            UserEmail = userEmail
        };

        await next(context);
    }
}
