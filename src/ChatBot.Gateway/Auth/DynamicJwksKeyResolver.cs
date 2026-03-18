using System.Collections.Concurrent;
using Microsoft.IdentityModel.Tokens;

namespace ChatBot.Gateway.Auth;

public class DynamicJwksKeyResolver
{
    private readonly string _keycloakBaseUrl;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CachedKeySet> _cache = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);
    private readonly ILogger<DynamicJwksKeyResolver> _logger;

    private record CachedKeySet(JsonWebKeySet KeySet, DateTime FetchedAt);

    public DynamicJwksKeyResolver(string keycloakBaseUrl, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _keycloakBaseUrl = keycloakBaseUrl.TrimEnd('/');
        _httpClient = httpClientFactory.CreateClient("jwks");
        _logger = loggerFactory.CreateLogger<DynamicJwksKeyResolver>();
    }

    public IEnumerable<SecurityKey> ResolveSigningKeys(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
    {
        var issuer = securityToken.Issuer;

        // Validate issuer is from our Keycloak instance
        if (!issuer.StartsWith($"{_keycloakBaseUrl}/realms/"))
        {
            _logger.LogWarning("Unknown issuer: {Issuer}", issuer);
            return [];
        }

        var cached = _cache.GetOrAdd(issuer, _ => FetchKeySet(issuer));

        // Check if cache expired
        if (DateTime.UtcNow - cached.FetchedAt > _cacheDuration)
        {
            cached = FetchKeySet(issuer);
            _cache[issuer] = cached;
        }

        var keys = cached.KeySet.GetSigningKeys();

        // If kid doesn't match any cached key, try refreshing (key rotation)
        if (!string.IsNullOrEmpty(kid) && !keys.Any(k => k.KeyId == kid))
        {
            _logger.LogInformation("Key {Kid} not found in cache for {Issuer}, refreshing", kid, issuer);
            cached = FetchKeySet(issuer);
            _cache[issuer] = cached;
            keys = cached.KeySet.GetSigningKeys();
        }

        return keys;
    }

    private CachedKeySet FetchKeySet(string issuer)
    {
        var jwksUrl = $"{issuer}/protocol/openid-connect/certs";
        _logger.LogInformation("Fetching JWKS from {Url}", jwksUrl);

        // Synchronous fetch (required by IssuerSigningKeyResolver delegate signature)
        var response = _httpClient.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
        var keySet = new JsonWebKeySet(response);
        return new CachedKeySet(keySet, DateTime.UtcNow);
    }
}
