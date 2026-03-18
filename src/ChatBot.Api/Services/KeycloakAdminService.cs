using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChatBot.Api.Services;

public class KeycloakAdminService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _keycloakUrl;
    private readonly string _adminUser;
    private readonly string _adminPassword;
    private readonly ILogger<KeycloakAdminService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KeycloakAdminService(IConfiguration configuration, ILogger<KeycloakAdminService> logger)
    {
        _logger = logger;
        _keycloakUrl = (configuration.GetConnectionString("keycloak")
            ?? configuration["Keycloak:Url"]
            ?? "http://localhost:8080").TrimEnd('/');
        _adminUser = configuration["Keycloak:AdminUser"] ?? "admin";
        _adminPassword = configuration["Keycloak:AdminPassword"] ?? "admin";

        // Own HttpClient - bypasses Aspire's resilience handlers that cause hanging
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _logger.LogInformation("KeycloakAdminService initialized — URL: {Url}, User: {User}", _keycloakUrl, _adminUser);
    }

    public async Task<string> GetAdminTokenAsync()
    {
        var tokenUrl = $"{_keycloakUrl}/realms/master/protocol/openid-connect/token";
        _logger.LogDebug("Getting admin token from {Url}", tokenUrl);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", _adminUser),
            new KeyValuePair<string, string>("password", _adminPassword)
        });

        var response = await _httpClient.PostAsync(tokenUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get admin token: {Status} — {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Keycloak token error {response.StatusCode}: {body}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
        return tokenResponse.GetProperty("access_token").GetString()!;
    }

    public async Task<bool> RealmExistsAsync(string realmName)
    {
        var token = await GetAdminTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakUrl}/admin/realms/{realmName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.SendAsync(request);
        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task CreateRealmAsync(string realmName, string displayName)
    {
        var token = await GetAdminTokenAsync();

        // Check if realm already exists
        using (var checkReq = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakUrl}/admin/realms/{realmName}"))
        {
            checkReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var checkResp = await _httpClient.SendAsync(checkReq);
            if (checkResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Realm {RealmName} already exists, skipping creation", realmName);
                return;
            }
        }

        // Step 1: Create realm with client + protocol mappers
        var realmRepresentation = new
        {
            realm = realmName,
            enabled = true,
            displayName,
            sslRequired = "none",
            registrationAllowed = false,
            loginWithEmailAllowed = true,
            duplicateEmailsAllowed = false,
            resetPasswordAllowed = true,
            passwordPolicy = "length(8) and digits(1)",
            ssoSessionIdleTimeout = 1800,
            ssoSessionMaxLifespan = 7200,
            accessTokenLifespan = 300,
            roles = new
            {
                realm = new object[]
                {
                    new { name = "admin", description = "Administrator role" },
                    new { name = "user", description = "Standard user role" }
                }
            },
            clients = new object[]
            {
                new
                {
                    clientId = "chatbot-frontend",
                    enabled = true,
                    publicClient = true,
                    directAccessGrantsEnabled = true,
                    standardFlowEnabled = true,
                    implicitFlowEnabled = false,
                    redirectUris = new[] { "*" },
                    webOrigins = new[] { "*" },
                    attributes = new Dictionary<string, string>
                    {
                        ["pkce.code.challenge.method"] = "S256",
                        ["access.token.lifespan"] = "300"
                    },
                    protocolMappers = new object[]
                    {
                        new
                        {
                            name = "tenant_id",
                            protocol = "openid-connect",
                            protocolMapper = "oidc-hardcoded-claim-mapper",
                            consentRequired = false,
                            config = new Dictionary<string, string>
                            {
                                ["claim.name"] = "tenant_id",
                                ["claim.value"] = realmName,
                                ["jsonType.label"] = "String",
                                ["id.token.claim"] = "true",
                                ["access.token.claim"] = "true",
                                ["userinfo.token.claim"] = "true"
                            }
                        },
                        new
                        {
                            name = "realm-roles",
                            protocol = "openid-connect",
                            protocolMapper = "oidc-usermodel-realm-role-mapper",
                            consentRequired = false,
                            config = new Dictionary<string, string>
                            {
                                ["multivalued"] = "true",
                                ["claim.name"] = "roles",
                                ["jsonType.label"] = "String",
                                ["id.token.claim"] = "true",
                                ["access.token.claim"] = "true",
                                ["userinfo.token.claim"] = "true"
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(realmRepresentation, JsonOptions);
        _logger.LogInformation("Creating Keycloak realm: {RealmName} at {Url}", realmName, _keycloakUrl);

        await SendRequest(HttpMethod.Post, $"{_keycloakUrl}/admin/realms", token, json);

        // Step 2: Create admin user
        var userRepresentation = new
        {
            username = "admin",
            enabled = true,
            email = $"{realmName}-admin@example.com",
            emailVerified = true,
            credentials = new object[]
            {
                new { type = "password", value = "Admin1234", temporary = true }
            }
        };

        var userJson = JsonSerializer.Serialize(userRepresentation, JsonOptions);
        await SendRequest(HttpMethod.Post, $"{_keycloakUrl}/admin/realms/{realmName}/users", token, userJson);

        // Step 3: Assign admin role to the user (best-effort)
        try
        {
            using var getUsersReq = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakUrl}/admin/realms/{realmName}/users?username=admin&exact=true");
            getUsersReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getUsersResp = await _httpClient.SendAsync(getUsersReq);
            var usersJson = await getUsersResp.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<JsonElement>(usersJson);
            var userId = users[0].GetProperty("id").GetString()!;

            using var getRoleReq = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakUrl}/admin/realms/{realmName}/roles/admin");
            getRoleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getRoleResp = await _httpClient.SendAsync(getRoleReq);
            var roleJson = await getRoleResp.Content.ReadAsStringAsync();

            await SendRequest(HttpMethod.Post,
                $"{_keycloakUrl}/admin/realms/{realmName}/users/{userId}/role-mappings/realm",
                token, $"[{roleJson}]");

            _logger.LogInformation("Realm {RealmName} created with admin user and roles", realmName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Realm {RealmName} created but role assignment failed (non-critical)", realmName);
        }
    }

    public async Task DeleteRealmAsync(string realmName)
    {
        var token = await GetAdminTokenAsync();
        await SendRequest(HttpMethod.Delete, $"{_keycloakUrl}/admin/realms/{realmName}", token);
    }

    private async Task SendRequest(HttpMethod method, string url, string token, string? jsonBody = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (jsonBody is not null)
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Keycloak API error: {Status} {Url} — {Body}", response.StatusCode, url, errorBody);
            throw new HttpRequestException($"Keycloak {response.StatusCode}: {errorBody}");
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
