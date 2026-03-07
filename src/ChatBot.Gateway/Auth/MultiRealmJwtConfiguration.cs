using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ChatBot.Gateway.Auth;

public static class MultiRealmJwtConfiguration
{
    private static readonly string[] Realms = ["basiccorp", "ssohub", "startupxyz"];

    public static AuthenticationBuilder AddMultiRealmJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakUrl = configuration.GetConnectionString("keycloak")
                          ?? configuration["Keycloak:Url"]
                          ?? "http://localhost:8080";
        var schemes = Realms.Select(r => $"Bearer_{r}").ToArray();

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = "MultiRealm";
            options.DefaultChallengeScheme = "MultiRealm";
        })
        .AddPolicyScheme("MultiRealm", "Multi-Realm JWT", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                    return schemes[0]; // Default fallback

                var token = authHeader["Bearer ".Length..];

                // Decode JWT payload to find issuer without validation
                try
                {
                    var parts = token.Split('.');
                    if (parts.Length >= 2)
                    {
                        var payload = parts[1].Replace('-', '+').Replace('_', '/');
                        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                        var doc = System.Text.Json.JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("iss", out var issuer))
                        {
                            var iss = issuer.GetString() ?? "";
                            foreach (var realm in Realms)
                            {
                                if (iss.Contains($"/realms/{realm}"))
                                    return $"Bearer_{realm}";
                            }
                        }
                    }
                }
                catch
                {
                    // Fall through to default
                }

                return schemes[0];
            };
        });

        foreach (var realm in Realms)
        {
            builder.AddJwtBearer($"Bearer_{realm}", options =>
            {
                options.Authority = $"{keycloakUrl}/realms/{realm}";
                options.RequireHttpsMetadata = false;
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{keycloakUrl}/realms/{realm}",
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var log = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");
                        log.LogError(context.Exception, "JWT auth failed for realm {Realm}", realm);
                        return Task.CompletedTask;
                    }
                };
            });
        }

        return builder;
    }
}
