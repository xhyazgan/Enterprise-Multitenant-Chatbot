using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ChatBot.Gateway.Auth;

public static class DynamicJwtConfiguration
{
    public static AuthenticationBuilder AddDynamicJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakUrl = (configuration.GetConnectionString("keycloak")
                          ?? configuration["Keycloak:Url"]
                          ?? "http://localhost:8080").TrimEnd('/');

        // Register JWKS HttpClient
        services.AddHttpClient("jwks")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        // Register the key resolver
        services.AddSingleton(sp => new DynamicJwksKeyResolver(
            keycloakUrl,
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<ILoggerFactory>()));

        // Add authentication with a basic setup
        var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Post-configure to inject the resolver (has access to DI)
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<DynamicJwksKeyResolver>((options, resolver) =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    IssuerValidator = (issuer, securityToken, validationParameters) =>
                    {
                        if (issuer.StartsWith($"{keycloakUrl}/realms/"))
                            return issuer;
                        throw new SecurityTokenInvalidIssuerException($"Invalid issuer: {issuer}");
                    },
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    IssuerSigningKeyResolver = resolver.ResolveSigningKeys
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var log = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");
                        log.LogError(context.Exception, "JWT auth failed");
                        return Task.CompletedTask;
                    }
                };
            });

        return builder;
    }
}
