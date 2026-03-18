using ChatBot.Gateway.Auth;
using ChatBot.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDynamicJwt(builder.Configuration);
builder.Services.AddAuthorization();

// Resolve chatbot-api URL from Aspire service discovery env vars
var apiUrl = builder.Configuration["services:chatbot-api:http:0"]
             ?? builder.Configuration["services:chatbot-api:https:0"]
             ?? "http://localhost:5000";

// Override YARP destination with the resolved URL so YARP doesn't see "https+http://" scheme
builder.Configuration["ReverseProxy:Clusters:chatbot-api:Destinations:primary:Address"] = apiUrl;

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway");
var keycloakUrl = builder.Configuration.GetConnectionString("keycloak")
                  ?? builder.Configuration["Keycloak:Url"]
                  ?? "http://localhost:8080";
logger.LogInformation("Gateway starting — Keycloak: {KeycloakUrl}, API: {ApiUrl}", keycloakUrl, apiUrl);

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in gateway — path: {Path}", context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = $"Gateway error: {ex.GetType().Name}: {ex.Message}",
                    path = context.Request.Path.Value
                });
            }
        }
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<HeaderInjectionMiddleware>();

app.MapGet("/gateway/health", () => Results.Ok(new { status = "ok", keycloakUrl, apiUrl }));

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.Run();
