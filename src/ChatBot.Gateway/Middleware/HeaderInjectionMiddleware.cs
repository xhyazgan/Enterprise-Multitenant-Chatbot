using System.Security.Claims;

namespace ChatBot.Gateway.Middleware;

public class HeaderInjectionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<HeaderInjectionMiddleware>();

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Headers.Remove("X-Tenant-Id");
        context.Request.Headers.Remove("X-User-Id");
        context.Request.Headers.Remove("X-User-Email");

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = context.User.FindFirstValue("tenant_id");
            if (string.IsNullOrEmpty(tenantId))
            {
                var issuer = context.User.FindFirstValue("iss") ?? "";
                tenantId = ExtractRealmFromIssuer(issuer);
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.User.FindFirstValue("sub");
            var email = context.User.FindFirstValue(ClaimTypes.Email)
                        ?? context.User.FindFirstValue("email");

            _logger.LogDebug("Injecting headers — tenant: {Tenant}, user: {User}",
                tenantId, userId);

            if (!string.IsNullOrEmpty(tenantId))
                context.Request.Headers["X-Tenant-Id"] = tenantId;

            if (!string.IsNullOrEmpty(userId))
                context.Request.Headers["X-User-Id"] = userId;

            if (!string.IsNullOrEmpty(email))
                context.Request.Headers["X-User-Email"] = email;
        }
        else
        {
            _logger.LogDebug("Request is not authenticated — path: {Path}", context.Request.Path);
        }

        await next(context);
    }

    private static string ExtractRealmFromIssuer(string issuer)
    {
        const string marker = "/realms/";
        var idx = issuer.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? issuer[(idx + marker.Length)..] : "";
    }
}
