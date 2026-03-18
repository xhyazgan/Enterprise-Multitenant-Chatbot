<div align="center">

<img src="https://img.shields.io/badge/STATUS-ACTIVE_DEVELOPMENT-brightgreen?style=flat-square" alt="Status" />

# Enterprise Multi-Tenant AI Chatbot

### One click. New tenant. Zero code.

> **A reference architecture** for building multi-tenant SaaS applications with .NET Aspire.
>
> This project demonstrates how to combine **Keycloak** (identity), **YARP** (gateway), and **React** (frontend) to create a platform where new tenants — complete with their own authentication realm, users, roles, and AI chatbot — are provisioned with a single API call. No restarts. No config files. No deploys.
>
> Built as a showcase for **enterprise-grade multi-tenancy patterns** in the .NET ecosystem.

<br/>

[![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/Aspire_13.1-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![React](https://img.shields.io/badge/React_19-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://react.dev/)
[![Keycloak](https://img.shields.io/badge/Keycloak_26-4D4D4D?style=for-the-badge&logo=keycloak&logoColor=white)](https://www.keycloak.org/)
[![YARP](https://img.shields.io/badge/YARP_2.3-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://microsoft.github.io/reverse-proxy/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL_16-336791?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript_5.9-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Tailwind](https://img.shields.io/badge/Tailwind_4-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![License](https://img.shields.io/badge/MIT-green?style=for-the-badge)](LICENSE)

</div>

<br/>

## What Does It Solve?

Building multi-tenant SaaS means solving the same problems over and over:

| Challenge | How This Project Handles It |
|:----------|:---------------------------|
| **"How do I onboard a new customer?"** | Admin Panel creates Keycloak realm + OAuth client + roles + user automatically |
| **"How do I isolate tenant data?"** | 5-layer isolation: Auth realm → JWT gateway → middleware → DB filter → AI config |
| **"How do I add Google/GitHub login?"** | Configure in Keycloak Admin Console. No code, no deploy. |
| **"How do I support different AI models per tenant?"** | Each tenant has its own provider (Claude/GPT), model, and system prompt |
| **"How do I validate JWTs from new tenants?"** | `DynamicJwksKeyResolver` fetches signing keys at runtime. No restart needed. |

<br/>

## Tenant Provisioning Flow

```mermaid
flowchart LR
    A["<b>Admin Panel</b><br/>React UI"] -->|"POST /api/admin/tenants"| B["<b>ChatBot API</b><br/>.NET 10"]
    B -->|"Admin REST API"| C["<b>Keycloak 26</b><br/>Creates Realm +<br/>Client + Mappers"]
    B -->|"INSERT"| D[("<b>PostgreSQL</b><br/>TenantConfig")]

    E["<b>New User</b>"] -->|"acmecorp.localhost"| F["<b>Frontend</b><br/>React 19"]
    F -->|"OAuth 2.0 PKCE"| C
    F -->|"Bearer JWT"| G["<b>YARP Gateway</b><br/>Dynamic JWT"]
    G -->|"X-Tenant-Id"| B
    B -->|"Per-tenant config"| H["<b>Claude / GPT</b>"]

    style A fill:#8B5CF6,color:#fff,stroke:#7C3AED
    style B fill:#512BD4,color:#fff,stroke:#4318D1
    style C fill:#4D4D4D,color:#fff,stroke:#333
    style D fill:#336791,color:#fff,stroke:#2A5478
    style E fill:#10B981,color:#fff,stroke:#059669
    style F fill:#61DAFB,color:#000,stroke:#4BB8D9
    style G fill:#512BD4,color:#fff,stroke:#4318D1
    style H fill:#D97706,color:#fff,stroke:#B45309
```

<br/>

## System Architecture

```mermaid
graph TB
    subgraph Aspire["<b>.NET Aspire AppHost</b> &nbsp; Service Discovery + Orchestration + OpenTelemetry"]
        direction TB
        FE["<b>Frontend</b><br/>React 19 &bull; Vite 7 &bull; Tailwind 4<br/>Zustand &bull; keycloak-js"]
        GW["<b>YARP Gateway</b><br/>Dynamic JWT Validation<br/>JWKS Caching &bull; Header Injection"]
        API["<b>ChatBot API</b><br/>.NET 10 &bull; EF Core<br/>Keycloak Admin &bull; AI Router"]
        KC["<b>Keycloak 26</b><br/>Realm 1..N (Dynamic)<br/>OAuth 2.0 &bull; OIDC &bull; SAML"]
        DB[("<b>PostgreSQL 16</b><br/>chatbot_db &bull; keycloak_db")]
    end

    FE -->|"/api/*"| GW
    GW -->|"X-Tenant-Id &bull; X-User-Id"| API
    GW -.->|"JWKS fetch (cached 30min)"| KC
    FE -.->|"OAuth 2.0 PKCE"| KC
    API -->|"Create Realm via Admin REST API"| KC
    API --> DB
    KC --> DB

    style Aspire fill:#0d1117,color:#c9d1d9,stroke:#30363d
    style FE fill:#1a1a2e,color:#61DAFB,stroke:#61DAFB
    style GW fill:#1a1a2e,color:#A78BFA,stroke:#8B5CF6
    style API fill:#1a1a2e,color:#A78BFA,stroke:#8B5CF6
    style KC fill:#1a1a2e,color:#F87171,stroke:#EF4444
    style DB fill:#1a1a2e,color:#60A5FA,stroke:#3B82F6
```

<br/>

## 5-Layer Tenant Isolation

```mermaid
graph LR
    A["<b>1. Auth Realm</b><br/>Separate users<br/>MFA &bull; IdPs"] --> B["<b>2. JWT Gateway</b><br/>Dynamic validation<br/>Cross-realm rejection"]
    B --> C["<b>3. Middleware</b><br/>X-Tenant-Id scope<br/>Every request"]
    C --> D["<b>4. Database</b><br/>All queries filtered<br/>by tenant_id"]
    D --> E["<b>5. AI Config</b><br/>Per-tenant provider<br/>model &bull; prompt"]

    style A fill:#EF4444,color:#fff,stroke:#DC2626
    style B fill:#F59E0B,color:#000,stroke:#D97706
    style C fill:#10B981,color:#fff,stroke:#059669
    style D fill:#3B82F6,color:#fff,stroke:#2563EB
    style E fill:#8B5CF6,color:#fff,stroke:#7C3AED
```

<br/>

## Quick Start

```bash
# Prerequisites: .NET 10 SDK, Docker Desktop, Node.js 20+
dotnet workload install aspire

git clone <repository-url>
cd Enterprise-Multitenant-Chatbot/Enterprise-Multitenant-Chatbot

# Set at least one AI key
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."

# Launch everything
dotnet run
```

> Aspire Dashboard opens automatically — all services, logs, metrics, and distributed traces in one place.
> Frontend starts at **localhost:5173**

### Pre-configured Tenants

3 tenants are seeded on first run, each demonstrating a different security posture:

| Tenant | Security Profile | Color |
|:-------|:----------------|:------|
| **BasicCorp** | Enterprise — strict passwords, short sessions, 4-tier RBAC | ![#3B82F6](https://via.placeholder.com/12/3B82F6/3B82F6.png) Blue |
| **SSOHub** | Tech Company — TOTP for admins, audit logging, 12hr sessions | ![#10B981](https://via.placeholder.com/12/10B981/10B981.png) Green |
| **StartupXYZ** | Startup — self-registration, relaxed policy, 7-day sessions | ![#F59E0B](https://via.placeholder.com/12/F59E0B/F59E0B.png) Amber |

### Create a New Tenant

1. Open **localhost:5173** → Click **Admin Panel**
2. Enter Tenant ID (e.g. `acmecorp`) and Display Name
3. Click **Create Tenant**
4. Navigate to **acmecorp.localhost:5173** → Login with Keycloak

<br/>

## Tech Stack

<table>
<tr>
<td width="33%">

### Backend
| | |
|:--|:--|
| .NET 10 | ASP.NET Core Minimal APIs |
| Aspire 13.1.2 | Orchestration + OTel |
| EF Core | PostgreSQL (Npgsql) |
| YARP 2.3.0 | Reverse Proxy + JWT |
| Anthropic SDK 4.0 | Claude AI |
| OpenAI SDK 2.2 | GPT Models |

</td>
<td width="33%">

### Frontend
| | |
|:--|:--|
| React 19 | TypeScript 5.9 |
| Vite 7.3 | Build + Dev Server |
| Tailwind CSS 4.2 | Utility-first CSS |
| Zustand 5.0 | State Management |
| keycloak-js 26.2 | OIDC Client |
| Axios | HTTP Client |

</td>
<td width="34%">

### Infrastructure
| | |
|:--|:--|
| Keycloak 26 | Multi-realm IAM |
| PostgreSQL 16 | Primary Database |
| Docker | Container Runtime |
| OpenTelemetry | Distributed Tracing |
| PgAdmin | DB Management |

</td>
</tr>
</table>

<br/>

## Why This Stack?

> **YARP** — Not just a proxy. It's the security boundary. JWT validation, header injection, and routing — all in one place. Built on Kestrel for maximum performance.

> **Keycloak** — Enterprise IAM that scales. Each realm is a complete identity silo: users, roles, MFA, social login, LDAP federation. The Admin REST API makes it fully automatable.

> **Aspire** — The modern alternative to docker-compose. Automatic service discovery, health checks, and an OpenTelemetry dashboard that actually helps you debug distributed systems.

> **Together** — YARP validates tokens from Keycloak, injects tenant context, and the API never touches authentication logic. Add a tenant, add a realm, add Google login. No restarts. No deploys. Just works.

---

<div align="center">

<br/>

**Multi-tenancy shouldn't require a deploy.**

<br/>

Made with .NET Aspire, Keycloak, React, and a belief that SaaS onboarding should be instant.

<br/>

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

</div>
