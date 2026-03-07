# Enterprise Multi-Tenant Chatbot

Enterprise-grade multi-tenant chatbot platform featuring **complete tenant isolation**, **multi-realm authentication**, and **intelligent AI routing**. Built with modern cloud-native architecture using YARP Gateway and Keycloak.

---

## 🎯 Project Goals

- **Complete Tenant Isolation**: Each tenant's data, sessions, and configurations are fully isolated at the database and application level
- **Multi-Realm Authentication**: Each tenant has its own Keycloak realm with independent user management and SSO capabilities
- **Flexible AI Integration**: Support for multiple AI providers (OpenAI, Claude) with per-tenant configuration
- **Enterprise Security**: JWT-based authentication with realm-specific token validation
- **Scalable Architecture**: Microservices-based design with API Gateway pattern using YARP
- **Developer Experience**: Full-stack solution with React frontend, .NET backend, and Docker orchestration

---

## 🏗️ Architecture

### High-Level Architecture

```
┌─────────────────┐
│   SSO Portal    │  (Tenant Selection & Login)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Frontend     │  (React + Keycloak JS)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  YARP Gateway   │◄─────┐
│  + Multi-Realm  │      │ JWT Validation
│  JWT Auth       │      │ (Per Realm)
└────────┬────────┘      │
         │               │
         │        ┌──────┴──────┐
         │        │  Keycloak   │
         │        │  ├─ Realm 1 │
         │        │  ├─ Realm 2 │
         │        │  └─ Realm N │
         │        └─────────────┘
         │
         ▼
┌─────────────────┐
│   ChatBot API   │
│  ┌───────────┐  │
│  │  Tenant   │  │ (Tenant Context Middleware)
│  │ Resolver  │  │
│  └─────┬─────┘  │
│        │        │
│  ┌─────▼─────┐  │
│  │ AI Router │  │ (OpenAI / Claude)
│  └───────────┘  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   PostgreSQL    │
│  ├─ chatbot_db  │ (Multi-tenant data with tenant_id)
│  └─ keycloak_db │ (Keycloak realms & users)
└─────────────────┘
```

### Request Flow

1. **User Login**: User selects tenant → SSO Portal redirects to tenant-specific Keycloak realm
2. **Authentication**: Keycloak authenticates user → Issues JWT token for that realm
3. **API Request**: Frontend sends request with JWT → YARP Gateway validates token against correct realm
4. **Header Injection**: Gateway extracts `tenant_id`, `user_id`, `email` from JWT → Injects as headers
5. **Tenant Resolution**: ChatBot API middleware reads headers → Sets tenant context
6. **Data Isolation**: All database queries automatically filtered by `tenant_id`
7. **AI Routing**: API selects appropriate AI provider based on tenant configuration

---

## 🔐 Why YARP + Keycloak?

### YARP (Yet Another Reverse Proxy)

**Purpose**: Acts as an intelligent API Gateway with authentication layer

**Benefits**:
- **Security Boundary**: Single point of authentication/authorization before reaching backend services
- **Header Injection**: Enriches requests with tenant context from validated JWT claims
- **Routing Flexibility**: Easy to add new microservices and route based on paths
- **Performance**: Built on ASP.NET Core Kestrel (high-performance, low-latency)
- **Multi-Realm JWT**: Custom implementation validates tokens against tenant-specific Keycloak realms

### Keycloak

**Purpose**: Enterprise Identity and Access Management (IAM)

**Benefits**:
- **Multi-Realm Support**: Each tenant gets isolated authentication realm
- **Standards-Based**: OAuth 2.0, OpenID Connect, SAML 2.0
- **User Federation**: Can integrate with LDAP, Active Directory, or custom providers
- **SSO Capabilities**: Single Sign-On across multiple applications per tenant
- **Fine-Grained Authorization**: Role-based access control (RBAC)
- **Customizable**: Theme per realm, custom authentication flows

### Why This Combination?

1. **Complete Tenant Isolation**
   - Keycloak: Separate realm = separate user database, roles, and authentication
   - YARP: Validates JWT against correct realm, prevents cross-tenant token usage
   - API: Enforces tenant context in all operations

2. **Scalability**
   - Add new tenants by creating new Keycloak realm (no code changes)
   - YARP can scale horizontally and route to multiple backend instances
   - Stateless architecture with JWT tokens

3. **Enterprise Security**
   - Industry-standard authentication protocols
   - Token-based authentication (no session management)
   - Automatic token expiration and refresh

4. **Developer Experience**
   - YARP configuration via appsettings.json
   - Keycloak admin UI for user management
   - Easy local development with Docker Compose

---

## 🔒 Tenant Isolation Strategy

### 1. Authentication Level (Keycloak Realms)
Each tenant has its own Keycloak realm (e.g., `ssohub-realm`, `basiccorp-realm`). This ensures:
- Tenant A users cannot authenticate to Tenant B
- JWT tokens are realm-specific and validated accordingly
- Each realm has independent user database, roles, and permissions

### 2. Gateway Level (YARP Multi-Realm JWT)
API Gateway validates each incoming JWT token against the appropriate realm. After successful validation:
- `X-Tenant-Id`: Tenant identifier
- `X-User-Id`: User identifier  
- `X-User-Email`: User email

Headers are injected to the backend, eliminating the need for backend services to handle authentication.

### 3. Application Level (Tenant Context)
Backend API reads the injected headers and maintains tenant context throughout the request. All database queries are automatically filtered by tenant.

### 4. Database Level (Tenant ID Column)
Every table includes a `tenant_id` column, and all queries are filtered by this column. Cross-tenant data access is completely prevented at the application level.

### 5. AI Configuration Level
Each tenant can have different AI provider (OpenAI/Claude), model, and system prompt configurations.

---

## 🧩 Components

### 1. **ChatBot.Gateway** (YARP API Gateway)
- **Technology**: ASP.NET Core 10.0 + YARP
- **Port**: 5100
- Multi-realm JWT authentication, request routing, header injection

### 2. **ChatBot.Api** (Backend API)
- **Technology**: ASP.NET Core 10.0 + Entity Framework Core
- **Port**: 5200
- Chat session management, AI provider integration, tenant resolution

### 3. **ChatBot.Frontend** (User Interface)
- **Technology**: React 18 + TypeScript + Vite + Tailwind CSS
- **Port**: 3000
- Chat interface, Keycloak integration, session management

### 4. **ChatBot.SsoPortal** (Tenant Selection)
- **Technology**: React 18 + TypeScript + Vite
- **Port**: 3001
- Tenant selection and login redirect

### 5. **Keycloak** (Identity Provider)
- **Technology**: Keycloak 26.0
- **Port**: 8080
- Multi-realm authentication, user management, JWT issuance

### 6. **PostgreSQL** (Database)
- **Technology**: PostgreSQL 16
- **Port**: 5432
- `keycloak_db` and `chatbot_db` (multi-tenant data)

---

## 🚀 Getting Started

### Prerequisites

- **.NET 10.0 SDK** (with Aspire workload)
- **Docker Desktop** (required for Aspire)
- **Node.js 20+** (for frontend development)

### Install .NET Aspire Workload

```bash
dotnet workload install aspire
```

### Environment Setup

1. **Clone the repository**:
```bash
git clone <repository-url>
cd Enterprise-Multitenant-Chatbot
```

2. **Set API Keys** (recommended: user secrets):
```bash
cd Enterprise-Multitenant-Chatbot
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-key"
dotnet user-secrets set "Claude:ApiKey" "your-claude-key"
```

3. **Run with .NET Aspire** (recommended):
```bash
cd Enterprise-Multitenant-Chatbot
dotnet run
```

Aspire Dashboard will open automatically. It provides unified view of all services, logs, and distributed tracing.

4. **Alternative: Docker Compose**:
```bash
# Create .env file first (copy from .env.example)
docker-compose up -d
docker-compose ps  # Check service health
```

### Access the Application

**With .NET Aspire (recommended):**
- **Aspire Dashboard**: http://localhost:15000 or URL shown in terminal
  - View all services, logs, metrics, and distributed traces
  - Click on service endpoints to access them
- **Keycloak Admin**: Check dashboard for port (default credentials: admin / admin)

**With Docker Compose:**
| Service | URL | Credentials |
|---------|-----|-------------|
| **SSO Portal** | http://localhost:3001 | Select tenant → Login |
| **Chatbot App** | http://localhost:3000 | After login from portal |
| **API Gateway** | http://localhost:5100 | N/A (JWT required) |
| **Keycloak Admin** | http://localhost:8080 | admin / admin |

### Test Users (Pre-configured in Realms)

**SsoHub Realm** (`http://localhost:8080/realms/ssohub`):
- User: `user@ssohub.com` / Password: `password`

**BasicCorp Realm** (`http://localhost:8080/realms/basiccorp`):
- User: `user@basiccorp.com` / Password: `password`

**StartupXYZ Realm** (`http://localhost:8080/realms/startupxyz`):
- User: `user@startupxyz.com` / Password: `password`

---

## 🛠️ Development

### Run with Aspire (Recommended)

```bash
cd Enterprise-Multitenant-Chatbot
dotnet run
```

Aspire automatically orchestrates all services with hot reload, logging, and distributed tracing.

### Run Individual Services

**Backend API:**
```bash
cd src/ChatBot.Api
dotnet run
```

**Gateway:**
```bash
cd src/ChatBot.Gateway
dotnet run
```

**Frontend:**
```bash
cd src/ChatBot.Frontend
npm install && npm run dev
```

### Database Migrations

```bash
cd src/ChatBot.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## 📦 Technology Stack

### Backend
- **.NET 10.0** + ASP.NET Core
- **.NET Aspire** (Orchestration & Observability)
- **Entity Framework Core** (PostgreSQL)
- **YARP** (API Gateway)

### Frontend
- **React 18** + TypeScript + Vite
- **Tailwind CSS**
- **Keycloak JS** + Zustand

### Infrastructure
- **.NET Aspire** (Local Development)
- **Docker** + Docker Compose
- **PostgreSQL 16**
- **Keycloak 26.0**

### AI Integrations
- OpenAI API (GPT models)
- Anthropic Claude API

---

## 📂 Project Structure

```
Enterprise-Multitenant-Chatbot/
├── src/
│   ├── ChatBot.Api/              # Backend API
│   │   ├── Data/                 # EF Core DbContext and entities
│   │   ├── Middleware/           # Tenant context middleware
│   │   ├── Services/             # AI services, tenant resolver
│   │   └── Endpoints/            # Minimal API endpoints
│   ├── ChatBot.Gateway/          # YARP API Gateway
│   │   ├── Auth/                 # Multi-realm JWT configuration
│   │   └── Middleware/           # Header injection
│   ├── ChatBot.Frontend/         # React chatbot UI
│   │   └── src/
│   │       ├── components/       # React components
│   │       ├── services/         # API client
│   │       ├── stores/           # Zustand stores
│   │       └── config/           # Keycloak config
│   ├── ChatBot.SsoPortal/        # Tenant selection portal
│   └── ChatBot.ServiceDefaults/  # Shared Aspire configuration
├── Enterprise-Multitenant-Chatbot/
│   └── Realms/                   # Keycloak realm configurations
│       ├── ssohub-realm.json
│       ├── basiccorp-realm.json
│       └── startupxyz-realm.json
├── docs/                         # Documentation
├── docker-compose.yml            # Docker orchestration
├── init-databases.sql            # PostgreSQL initialization
└── README.md                     # This file
```

---

## 🔧 Configuration

### Adding a New Tenant

1. **Create Keycloak Realm**: Login to Keycloak Admin Console and create new realm
2. **Update Gateway Configuration**: Add realm configuration to `appsettings.json`
3. **Add Tenant to Database**: Insert tenant configuration record
4. **Update SSO Portal**: Add tenant to selector dropdown

---

## 🔍 Key Features

### Multi-Tenant Data Isolation
- Every database query automatically filtered by `tenant_id`
- Cross-tenant data access impossible at application level
- Tenant context enforced throughout entire request pipeline

### Flexible AI Provider Configuration
Customizable per tenant:
- AI Provider selection (OpenAI or Claude)
- Model selection (gpt-4, claude-3-opus, etc.)
- Custom system prompt
- Provider-specific settings

### Secure Header Injection
Gateway automatically injects trusted headers after JWT validation:
- `X-Tenant-Id`: Tenant identifier (from realm)
- `X-User-Id`: User identifier
- `X-User-Email`: User email

### Session Management
- Multi-session support per user
- Tenant and user-based isolation
- Automatic session title generation
- Message history persistence

---

## 🧪 Testing

### Manual Testing
1. Start services: `docker-compose up -d`
2. Access SSO Portal: http://localhost:3001
3. Select a tenant and login with test credentials
4. Create chat session and send messages
5. Verify isolation by logging in as different tenant

### API Testing
Use Postman or cURL to test API endpoints. Get JWT token from Keycloak and use it in Authorization header.

---

## 🚨 Security Considerations

- ✅ JWT-based multi-realm authentication
- ✅ Complete tenant isolation (database + application level)
- ✅ Secure header injection via Gateway
- ✅ Environment variables for secrets

## 📊 Monitoring

**Health Checks**: `/gateway/health` and `/health` endpoints

**Production**: Application Insights, CloudWatch, ELK Stack, Datadog

**Key Metrics**: Request rate, auth rates, AI costs, system resources

---

## 🗺️ Roadmap

- Real-time chat (SignalR/WebSockets)
- File upload support
- Multi-AI provider support (Azure OpenAI, Google PaLM)
- Usage analytics dashboard
- Tenant-specific branding
- RBAC within tenants
- Rate limiting per tenant
- Mobile app (React Native)

---

## 📄 License

MIT License

---
