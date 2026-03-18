var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with two databases
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var chatbotDb = postgres.AddDatabase("chatbotdb");

// Keycloak with realm imports
var keycloak = builder.AddKeycloakContainer("keycloak")
    .WithImport("./Realms/basiccorp-realm.json")
    .WithImport("./Realms/ssohub-realm.json")
    .WithImport("./Realms/startupxyz-realm.json");

// ChatBot API
var api = builder.AddProject<Projects.ChatBot_Api>("chatbot-api")
    .WithReference(chatbotDb)
    .WithReference(keycloak)
    .WaitFor(chatbotDb)
    .WaitFor(keycloak);

// ChatBot Gateway
var gateway = builder.AddProject<Projects.ChatBot_Gateway>("chatbot-gateway")
    .WithReference(api)
    .WithReference(keycloak)
    .WaitFor(api)
    .WaitFor(keycloak);

// Frontend (Vite/React)
builder.AddViteApp("chatbot-frontend", "../src/ChatBot.Frontend")
    .WithNpm()
    .WithReference(gateway)
    .WithReference(keycloak)
    .WaitFor(gateway)
    .WithHttpEndpoint(port: 5173, env: "PORT", name: "vite")
    .WithEnvironment("GATEWAY_URL", gateway.GetEndpoint("http"))
    .WithEnvironment("KEYCLOAK_URL", keycloak.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

// SSO Demo Portal (Vite/React) - demonstrates SSO across apps in same realm
builder.AddViteApp("sso-portal", "../src/ChatBot.SsoPortal")
    .WithNpm()
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5174, env: "PORT", name: "vite")
    .WithEnvironment("KEYCLOAK_URL", keycloak.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
