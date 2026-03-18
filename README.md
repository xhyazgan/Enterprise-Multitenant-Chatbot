<div align="center">

# Enterprise Multi-Tenant AI Chatbot

**One click. New tenant. Zero code.**

Create fully isolated AI chatbot tenants with their own authentication, users, and AI configuration — all without writing a single line of code.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://react.dev/)
[![Keycloak](https://img.shields.io/badge/Keycloak-26-4D4D4D?style=for-the-badge&logo=keycloak&logoColor=white)](https://www.keycloak.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

---

**[Quick Start](#-quick-start)** | **[How It Works](#-how-it-works)** | **[Architecture](#-architecture)** | **[API Reference](#-api-reference)**

</div>

---

## The Problem

Building multi-tenant SaaS is hard. For every new customer you need to:
- Set up authentication and user management
- Configure identity providers (Google, GitHub, LDAP...)
- Isolate their data from other tenants
- Route AI requests to the right model with the right prompt
- Do it all **without deploying new code**

## The Solution

This platform turns all of that into a **single API call**. Behind the scenes:

```
POST /api/admin/tenants { "tenantId": "acmecorp", "displayName": "Acme Corp" }
```

> **What happens in ~2 seconds:**
>
> 1. Keycloak realm created with OAuth 2.0 / OIDC client (PKCE)
> 2. JWT claim mappers configured (`tenant_id`, `roles`)
> 3. Admin user provisioned with temporary password
> 4. Tenant config stored in PostgreSQL
> 5. Gateway immediately accepts tokens from the new realm
> 6. Frontend shows the new tenant in the selector
>
> **Done.** Navigate to `acmecorp.localhost:5173` and login.

Want Google login for that tenant? Go to Keycloak Admin Console, add Google as an identity provider. **Zero code.**

---

## Demo

```
http://localhost:5173                    # Tenant selector (dynamic from API)
http://basiccorp.localhost:5173          # BasicCorp tenant login
http://acmecorp.localhost:5173           # Your new tenant login
http://localhost:8080/admin              # Keycloak Admin Console
```

### Pre-configured Tenants

| Tenant | Security Profile | AI Model | Color |
|--------|-----------------|----------|-------|
| **BasicCorp** | Enterprise (strict passwords, short sessions, 4-tier RBAC) | Claude Haiku 4.5 | Blue |
| **SSOHub** | Tech Company (TOTP for admins, audit logging, 12hr sessions) | Claude Haiku 4.5 | Green |
| **StartupXYZ** | Startup (self-registration, relaxed policy, 7-day sessions) | Claude Haiku 4.5 | Amber |

Each demonstrates a different security posture — all managed through Keycloak, not code.

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) + Aspire workload
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js 20+](https://nodejs.org/)

```bash
# Install Aspire workload
dotnet workload install aspire

# Clone and enter
git clone <repository-url>
cd Enterprise-Multitenant-Chatbot

# Set your AI keys (pick one or both)
cd Enterprise-Multitenant-Chatbot
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."

# Launch everything
dotnet run
```

Aspire Dashboard opens automatically — all services, logs, metrics, and distributed traces in one place.

### Create Your First Tenant

1. Open http://localhost:5173
2. Click **Admin Panel** at the bottom
3. Enter a Tenant ID (e.g. `mycompany`) and Display Name
4. Hit **Create Tenant**
5. Click on the new tenant card to login at `mycompany.localhost:5173`

---

## How It Works

### The Magic: Dynamic Everything

Most multi-tenant systems require configuration changes and restarts when adding tenants. This one doesn't.

| Layer | Traditional Approach | This Project |
|-------|---------------------|--------------|
| **Auth** | Hardcoded realm list, restart needed | `DynamicJwksKeyResolver` — accepts any realm, fetches keys on demand |
| **Gateway** | One JWT handler per tenant | Single handler with `IssuerSigningKeyResolver` delegate |
| **Frontend** | Hardcoded tenant config | `GET /api/tenants` — fetched from DB at runtime |
| **Realm Setup** | Manual Keycloak admin work | `KeycloakAdminService` — automated via Admin REST API |

### Request Lifecycle

```
User clicks "BasicCorp"
  │
  ├─ Redirect → basiccorp.localhost:5173
  ├─ Keycloak init (PKCE + login-required)
  ├─ User authenticates → JWT issued by basiccorp realm
  │
  ├─ POST /api/chat/message (Bearer token)
  │    │
  │    ├─ YARP Gateway
  │    │   ├─ Extract issuer from JWT (no validation yet)
  │    │   ├─ Fetch JWKS from {issuer}/protocol/openid-connect/certs (cached 30min)
  │    │   ├─ Validate signature, lifetime, issuer pattern
  │    │   └─ Inject headers: X-Tenant-Id, X-User-Id, X-User-Email
  │    │
  │    ├─ ChatBot API
  │    │   ├─ TenantContextMiddleware reads headers
  │    │   ├─ TenantResolver loads config (cached in ConcurrentDictionary)
  │    │   ├─ AiServiceFactory → ClaudeAiService (tenant uses Claude)
  │    │   ├─ AI responds with tenant's system prompt context
  │    │   └─ Message persisted with tenant_id isolation
  │    │
  │    └─ Response returned to frontend
  │
  └─ Message appears in chat UI
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        .NET Aspire AppHost                          │
│                   (Service Discovery + Orchestration)                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌───────────┐    ┌──────────────┐    ┌──────────────────────────┐ │
│  │ Frontend  │───▶│ YARP Gateway │───▶│      ChatBot API         │ │
│  │           │    │              │    │                          │ │
│  │ React 19  │    │ Dynamic JWT  │    │ Tenant CRUD + Keycloak   │ │
│  │ Vite 7    │    │ JWKS Cache   │    │ AI Routing (Claude/GPT)  │ │
│  │ Tailwind 4│    │ Header Inject│    │ Chat Sessions            │ │
│  │ Zustand   │    │              │    │                          │ │
│  └───────────┘    └──────┬───────┘    └────────┬─────────────────┘ │
│                          │                     │                    │
│                   ┌──────┴───────┐      ┌──────┴───────┐           │
│                   │   Keycloak   │      │  PostgreSQL   │           │
│                   │   26.0       │      │  16-alpine    │           │
│                   │              │      │               │           │
│                   │ Realm 1..N   │      │ chatbot_db    │           │
│                   │ (Dynamic)    │      │ keycloak_db   │           │
│                   └──────────────┘      └───────────────┘           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 5-Layer Tenant Isolation

| # | Layer | How |
|---|-------|-----|
| 1 | **Authentication** | Each tenant = separate Keycloak realm (independent users, passwords, MFA, identity providers) |
| 2 | **Gateway** | Dynamic JWT validation — tokens validated against issuing realm's JWKS. Cross-realm tokens rejected. |
| 3 | **Application** | `TenantContextMiddleware` enforces tenant context. Services scoped to `X-Tenant-Id`. |
| 4 | **Database** | Every row tagged with `tenant_id`. All queries filtered. Cross-tenant reads impossible. |
| 5 | **AI** | Per-tenant AI provider, model, and system prompt. BasicCorp gets formal Claude, StartupXYZ gets casual. |

---

## Tech Stack

<table>
<tr><td>

### Backend
| | |
|-|-|
| .NET 10 | ASP.NET Core Minimal APIs |
| Aspire 13.1.2 | Orchestration + OpenTelemetry |
| EF Core | PostgreSQL via Npgsql |
| YARP 2.3.0 | Reverse proxy + JWT auth |

</td><td>

### Frontend
| | |
|-|-|
| React 19.2 | TypeScript 5.9 |
| Vite 7.3 | Tailwind CSS 4.2 |
| Zustand 5.0 | State management |
| keycloak-js 26.2 | OIDC/OAuth client |

</td><td>

### Infrastructure
| | |
|-|-|
| Keycloak 26 | Multi-realm IAM |
| PostgreSQL 16 | Primary database |
| Docker | Container runtime |
| Anthropic SDK 4.0 | Claude AI |
| OpenAI SDK 2.2 | GPT models |

</td></tr>
</table>

---

## API Reference

### Public

```http
GET /api/tenants              # List active tenants (no auth required)
```

### Admin

```http
POST   /api/admin/tenants              # Create tenant + Keycloak realm
GET    /api/admin/tenants              # List all tenants
PUT    /api/admin/tenants/{tenantId}   # Update tenant config
DELETE /api/admin/tenants/{tenantId}   # Deactivate tenant
```

> All admin endpoints require `X-Admin-Key` header.

### Chat (JWT required)

```http
POST   /api/chat/message                      # Send message, get AI response
GET    /api/chat/sessions                     # List user's sessions
GET    /api/chat/sessions/{id}/messages       # Get session history
DELETE /api/chat/sessions/{id}                # Soft delete session
```

### Example: Create Tenant

```bash
curl -X POST http://localhost:5173/api/admin/tenants \
  -H "Content-Type: application/json" \
  -H "X-Admin-Key: dev-admin-key-change-in-production" \
  -d '{
    "tenantId": "acmecorp",
    "displayName": "Acme Corporation",
    "aiProvider": "Claude",
    "aiModel": "claude-haiku-4-5",
    "color": "#8B5CF6",
    "description": "Purple-themed Acme workspace"
  }'
```

---

## Project Structure

```
Enterprise-Multitenant-Chatbot/
│
├── Enterprise-Multitenant-Chatbot/        # .NET Aspire AppHost
│   ├── AppHost.cs                         # Service composition & orchestration
│   └── Realms/                            # Keycloak seed realm JSONs
│
├── src/
│   ├── ChatBot.Api/                       # Backend API
│   │   ├── Endpoints/
│   │   │   ├── ChatEndpoints.cs           #   Chat session & message CRUD
│   │   │   └── TenantEndpoints.cs         #   Tenant management + Keycloak provisioning
│   │   ├── Services/
│   │   │   ├── KeycloakAdminService.cs    #   Keycloak Admin REST API client
│   │   │   ├── TenantResolver.cs          #   Cached tenant config lookup
│   │   │   ├── AiServiceFactory.cs        #   Routes to Claude or OpenAI
│   │   │   ├── ClaudeAiService.cs         #   Anthropic SDK integration
│   │   │   └── OpenAiService.cs           #   OpenAI SDK integration
│   │   ├── Data/                          #   EF Core entities + DbContext + seed data
│   │   └── Middleware/                    #   Tenant context extraction
│   │
│   ├── ChatBot.Gateway/                   # YARP API Gateway
│   │   ├── Auth/
│   │   │   ├── DynamicJwtConfiguration.cs #   Single dynamic JWT handler
│   │   │   └── DynamicJwksKeyResolver.cs  #   Runtime JWKS fetching + caching
│   │   └── Middleware/
│   │       └── HeaderInjectionMiddleware.cs#  JWT claims → X-Tenant-Id headers
│   │
│   ├── ChatBot.Frontend/                  # React SPA
│   │   └── src/
│   │       ├── components/                #   TenantSelector, AdminPanel, Chat UI
│   │       ├── stores/                    #   tenantStore, authStore, chatStore
│   │       ├── services/                  #   API clients (tenant, chat)
│   │       └── config/keycloak.ts         #   Dynamic Keycloak helpers
│   │
│   └── ChatBot.ServiceDefaults/           # Shared Aspire config (OTel, health, resilience)
│
├── docker-compose.yml                     # Alternative to Aspire
└── README.md
```

---

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `Claude:ApiKey` | Anthropic API key | *(user secrets)* |
| `OpenAI:ApiKey` | OpenAI API key | *(user secrets)* |
| `Keycloak:AdminUser` | Master realm admin | `admin` |
| `Keycloak:AdminPassword` | Master realm password | `admin` |
| `AdminApiKey` | Tenant admin endpoint key | `dev-admin-key-change-in-production` |

### Gateway Routes (YARP)

| Route | Pattern | Auth | Priority |
|-------|---------|------|----------|
| Public tenants | `/api/tenants` | None | 1 |
| Admin | `/api/admin/**` | X-Admin-Key | 2 |
| Chat API | `/api/**` | JWT (dynamic) | 10 |

---

## Why This Stack?

**YARP** — Not just a proxy. It's the security boundary. One place for JWT validation, header injection, and routing. Built on Kestrel — blazing fast.

**Keycloak** — Enterprise IAM that scales. Each realm is a complete identity silo: users, roles, MFA, social login, LDAP federation. The Admin REST API makes it automatable.

**Aspire** — Replaces docker-compose for local dev with automatic service discovery, health checks, OpenTelemetry, and a dashboard that actually helps you debug.

**The combination** — YARP validates tokens from Keycloak, injects tenant context, and the API never needs to know about authentication. Add a tenant, add a realm, add users, add Google login — the Gateway and API just work.

---

<div align="center">

**Built with .NET Aspire, Keycloak, React, and a belief that multi-tenancy shouldn't require a deploy.**

MIT License

</div>
