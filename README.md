# Enterprise Multi-Tenant Chatbot

Enterprise-grade multi-tenant chatbot platform with **zero-code tenant provisioning**, **dynamic Keycloak realm management**, and **intelligent AI routing**. Built with .NET Aspire, YARP Gateway, and Keycloak.

---

## Project Goals

- **Zero-Code Multi-Tenancy**: Add new tenants from the Admin Panel — Keycloak realm, client, and roles are created automatically
- **Complete Tenant Isolation**: Each tenant's data, sessions, and configurations are fully isolated
- **Dynamic Authentication**: Gateway validates JWT tokens from any Keycloak realm without restart or configuration changes
- **Flexible AI Integration**: Support for multiple AI providers (OpenAI, Claude) with per-tenant configuration
- **Enterprise Security**: JWT-based authentication with dynamic realm validation via YARP Gateway

---

## Architecture

```
                    ┌──────────────────────┐
                    │   Admin Panel (UI)   │  Create Tenant
                    └──────────┬───────────┘
                               │ POST /api/admin/tenants
                               ▼
┌─────────────────┐   ┌─────────────────┐   ┌──────────────────┐
│    Frontend     │──▶│  YARP Gateway   │──▶│   ChatBot API    │
│  React 19 +     │   │  Dynamic JWT    │   │  ┌────────────┐  │
│  Keycloak-js    │   │  Validation     │   │  │  Keycloak  │  │
│  Zustand        │   │                 │   │  │  Admin API │  │──▶ Creates Realm
└─────────────────┘   └────────┬────────┘   │  └────────────┘  │   + Client
                               │            │  ┌────────────┐  │   + Mappers
                        ┌──────┴──────┐     │  │ AI Router  │  │   + Admin User
                        │  Keycloak   │     │  │ Claude/GPT │  │
                        │  Realm 1..N │     │  └────────────┘  │
                        │  (Dynamic)  │     └────────┬─────────┘
                        └─────────────┘              │
                                              ┌──────▼──────┐
                                              │ PostgreSQL  │
                                              │ chatbot_db  │
                                              │ keycloak_db │
                                              └─────────────┘
```

### Request Flow

1. **Tenant Selection**: User visits `localhost:5173` → Frontend fetches tenant list from `GET /api/tenants`
2. **Subdomain Redirect**: User selects tenant → Redirected to `{tenantId}.localhost:5173`
3. **Keycloak Login**: Frontend creates Keycloak instance for that realm → `login-required` with PKCE
4. **JWT Validation**: Gateway's `DynamicJwksKeyResolver` fetches signing keys from the realm's JWKS endpoint, caches for 30 min
5. **Header Injection**: Gateway extracts `tenant_id`, `sub`, `email` from JWT → Injects as `X-Tenant-Id`, `X-User-Id`, `X-User-Email`
6. **Tenant Context**: API middleware reads headers → All DB queries filtered by `tenant_id`
7. **AI Routing**: `TenantResolver` loads tenant config → `AiServiceFactory` routes to Claude or OpenAI

### Adding a New Tenant (Zero Code)

```
Admin Panel → POST /api/admin/tenants
  → KeycloakAdminService.CreateRealmAsync()
    → Creates realm with chatbot-frontend client
    → Adds tenant_id claim mapper (hardcoded to realm name)
    → Adds realm-roles mapper
    → Creates admin user (Admin1234, temporary)
  → Insert TenantConfig to DB
  → Invalidate TenantResolver cache
  → Frontend refreshes tenant list
  → New tenant ready for login
```

After creation, configure identity providers (Google, GitHub, SAML, LDAP) directly in the Keycloak Admin Console — **no code changes needed**.

---

## Components

| Component | Technology | Description |
|-----------|-----------|-------------|
| **AppHost** | .NET Aspire 13.1.2 | Orchestrates all services with service discovery |
| **ChatBot.Api** | .NET 10 + EF Core | Tenant CRUD, Keycloak Admin API, AI routing, chat endpoints |
| **ChatBot.Gateway** | .NET 10 + YARP 2.3.0 | Dynamic multi-realm JWT validation, header injection, reverse proxy |
| **ChatBot.Frontend** | React 19 + Vite 7 + Tailwind 4 | Dynamic tenant selector, admin panel, chat UI, Keycloak-js auth |
| **Keycloak** | Keycloak 26 | Multi-realm IAM, OAuth 2.0 / OIDC, identity federation |
| **PostgreSQL** | PostgreSQL 16 | `chatbot_db` (multi-tenant data) + `keycloak_db` |

---

## Getting Started

### Prerequisites

- **.NET 10.0 SDK** with Aspire workload (`dotnet workload install aspire`)
- **Docker Desktop** (required for Aspire containers)
- **Node.js 20+** (for frontend)

### Quick Start

```bash
# Clone
git clone <repository-url>
cd Enterprise-Multitenant-Chatbot

# Set AI API keys
cd Enterprise-Multitenant-Chatbot
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-key"
dotnet user-secrets set "Claude:ApiKey" "your-claude-key"

# Run with Aspire
dotnet run
```

Aspire Dashboard opens automatically with service logs, metrics, and distributed tracing.

### Access Points

| Service | URL | Notes |
|---------|-----|-------|
| **Aspire Dashboard** | Shown in terminal | Logs, metrics, traces |
| **Frontend** | http://localhost:5173 | Tenant selector + chat |
| **Keycloak Admin** | Check Aspire dashboard for port | admin / admin |

### Pre-configured Tenants

Three tenants are seeded on first run:

| Tenant | Realm | Color | Description |
|--------|-------|-------|-------------|
| BasicCorp | `basiccorp` | Blue | Enterprise — Strict security, 4-tier roles |
| SSOHub | `ssohub` | Green | Tech Company — OTP, audit logging |
| StartupXYZ | `startupxyz` | Amber | Startup — Self-registration, relaxed policy |

Each realm has pre-configured test users (alice, bob, etc.) with password `password`.

### Create a New Tenant

1. Go to http://localhost:5173
2. Click **Admin Panel** at the bottom
3. Fill in Tenant ID (e.g., `acmecorp`), Display Name, and optionally customize AI provider/model
4. Click **Create Tenant**
5. The new tenant appears in the selector immediately
6. Navigate to `http://acmecorp.localhost:5173` to login
7. Optionally: configure social login providers in Keycloak Admin Console

---

## API Endpoints

### Public (No Auth)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/tenants` | List active tenants |

### Admin (X-Admin-Key Header)

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/admin/tenants` | Create tenant + Keycloak realm |
| `GET` | `/api/admin/tenants` | List all tenants (including inactive) |
| `PUT` | `/api/admin/tenants/{id}` | Update tenant config |
| `DELETE` | `/api/admin/tenants/{id}` | Deactivate tenant |

### Chat (JWT Auth via Gateway)

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/chat/message` | Send message and get AI response |
| `GET` | `/api/chat/sessions` | List user's chat sessions |
| `GET` | `/api/chat/sessions/{id}/messages` | Get session messages |
| `DELETE` | `/api/chat/sessions/{id}` | Delete session (soft) |

---

## Tenant Isolation Strategy

### 1. Authentication (Keycloak Realms)
Each tenant = separate Keycloak realm with independent users, roles, and identity providers.

### 2. Gateway (Dynamic JWT)
`DynamicJwksKeyResolver` validates tokens from **any** realm under the Keycloak instance. No hardcoded realm list — new realms are accepted automatically.

### 3. Application (Tenant Context Middleware)
API middleware extracts `X-Tenant-Id` and `X-User-Id` headers. All service layer operations scoped to the current tenant.

### 4. Database (Tenant ID Column)
Every table includes `tenant_id`. All queries filtered automatically. Cross-tenant data access is impossible at the application level.

### 5. AI Configuration
Each tenant has its own AI provider (Claude/OpenAI), model, and system prompt.

---

## Project Structure

```
Enterprise-Multitenant-Chatbot/
├── Enterprise-Multitenant-Chatbot/     # Aspire AppHost (orchestrator)
│   ├── AppHost.cs                      # Service composition
│   └── Realms/                         # Seed realm JSON files
│       ├── basiccorp-realm.json
│       ├── ssohub-realm.json
│       └── startupxyz-realm.json
├── src/
│   ├── ChatBot.Api/
│   │   ├── Data/                       # EF Core entities + DbContext
│   │   ├── Endpoints/
│   │   │   ├── ChatEndpoints.cs        # Chat CRUD
│   │   │   └── TenantEndpoints.cs      # Tenant management + Keycloak
│   │   ├── Middleware/                  # TenantContextMiddleware
│   │   └── Services/
│   │       ├── KeycloakAdminService.cs  # Keycloak Admin REST API client
│   │       ├── TenantResolver.cs        # Cached tenant config lookup
│   │       ├── AiServiceFactory.cs      # AI provider routing
│   │       ├── ClaudeAiService.cs
│   │       └── OpenAiService.cs
│   ├── ChatBot.Gateway/
│   │   ├── Auth/
│   │   │   ├── MultiRealmJwtConfiguration.cs  # Dynamic JWT handler setup
│   │   │   └── DynamicJwksKeyResolver.cs      # JWKS fetch + cache
│   │   └── Middleware/
│   │       └── HeaderInjectionMiddleware.cs    # JWT claims → headers
│   ├── ChatBot.Frontend/
│   │   └── src/
│   │       ├── components/
│   │       │   ├── TenantSelector.tsx   # Dynamic tenant list from API
│   │       │   ├── AdminPanel.tsx       # Create tenant form
│   │       │   ├── ChatLayout.tsx
│   │       │   ├── MessagePanel.tsx
│   │       │   ├── MessageInput.tsx
│   │       │   └── SessionList.tsx
│   │       ├── stores/
│   │       │   ├── tenantStore.ts       # Tenant list state (Zustand)
│   │       │   ├── authStore.ts         # Keycloak auth state
│   │       │   └── chatStore.ts         # Chat sessions/messages
│   │       ├── services/
│   │       │   ├── tenantService.ts     # Public + admin tenant API
│   │       │   ├── chatService.ts
│   │       │   └── api.ts              # Axios with auth interceptor
│   │       └── config/
│   │           └── keycloak.ts          # Keycloak helpers (no hardcoded tenants)
│   └── ChatBot.ServiceDefaults/         # Shared Aspire configuration
├── docker-compose.yml
└── README.md
```

---

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Orchestration | .NET Aspire | 13.1.2 |
| Backend | .NET / ASP.NET Core | 10.0 |
| ORM | Entity Framework Core (Npgsql) | 13.1.2 |
| Gateway | YARP | 2.3.0 |
| Auth | JWT Bearer | 10.0.1 |
| Frontend | React + TypeScript | 19.2 + 5.9 |
| Bundler | Vite | 7.3 |
| CSS | Tailwind CSS | 4.2 |
| State | Zustand | 5.0 |
| Auth Client | keycloak-js | 26.2 |
| IAM | Keycloak | 26.0 |
| Database | PostgreSQL | 16 |
| AI | Anthropic SDK / OpenAI SDK | 4.0 / 2.2 |

---

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `OpenAI:ApiKey` | OpenAI API key | (user secrets) |
| `Claude:ApiKey` | Anthropic API key | (user secrets) |
| `Keycloak:AdminUser` | Keycloak master admin | `admin` |
| `Keycloak:AdminPassword` | Keycloak master password | `admin` |
| `AdminApiKey` | Admin endpoint auth key | `dev-admin-key-change-in-production` |

### YARP Routes

| Route | Path | Auth | Order |
|-------|------|------|-------|
| tenants-public | `/api/tenants` | None | 1 |
| admin-route | `/api/admin/{**catch-all}` | X-Admin-Key | 2 |
| api-route | `/api/{**catch-all}` | JWT (dynamic) | 10 |

---

## License

MIT License
