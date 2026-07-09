# Ehgiz — Backend API

Ehgiz is a peer-to-peer **tool rental marketplace**: owners list their tools, renters browse, book, pay, and chat with owners in real time. This repository contains the backend — a RESTful API built with **ASP.NET Core (.NET 10)**, layered with clean architecture, and backed by **SQL Server** via Entity Framework Core.

The Angular frontend lives in a separate repository and consumes this API.

---

## Features

- **Authentication & Accounts**
  - JWT access tokens (60 min) + rotating refresh tokens delivered as an HTTP-only cookie scoped to `/api/auth`
  - Email verification and password reset via one-time codes (SendGrid), with per-email attempt limits and per-IP rate limiting
  - ASP.NET Core Identity with role-based authorization (Admin / User)
- **Tool Listings**
  - CRUD with multi-image upload (Cloudinary), primary image selection, categories, and condition filters
  - Geo search by latitude/longitude, plus text, category, and price filtering
- **Bookings & Handover**
  - Full booking lifecycle (request → approve → active → complete/cancel)
  - Handover records with photo evidence and issue reporting
- **Payments & Wallet**
  - Stripe integration (payment intents + webhooks), USD top-ups
  - Internal wallet with transaction history, owner earnings, and a platform revenue ledger (configurable platform fee, default 10%)
- **Real-Time Messaging & Notifications**
  - SignalR hubs for chat (`/hubs/chat`) and notifications (`/hubs/notifications`)
  - Persistent conversations, unread counts, and typed notification events
- **Saved Searches**
  - Users save search criteria and get notified when a matching tool is listed
- **Reviews** — ratings and comments tied to completed bookings
- **AI-Powered Assistance** (via GitHub Models / OpenAI)
  - Image validation for tool photos
  - Photo-based tool search ("find tools that look like this")
  - Tool listing suggestions and a conversational tool assistant agent
- **Admin Panel API** — user, tool, and platform management endpoints
- **Operational** — Serilog structured logging, global exception-handling middleware, health checks (`/health`), Swagger/OpenAPI with JWT auth support

---

## Architecture

The solution follows a clean, three-layer architecture with strict inward-pointing dependencies:

```
Ehgiz.slnx
├── Ehgiz.API/            # Presentation layer
│   ├── Controllers/      #   REST endpoints (incl. Admin/)
│   ├── Hubs/             #   SignalR hubs (Chat, Notifications)
│   ├── Middleware/       #   Global exception handling
│   └── Extensions/       #   Composable startup configuration (Auth, CORS, Swagger, AI, SignalR, DB)
├── Ehgiz.Application/    # Business logic layer
│   ├── Services/         #   Domain services (Auth, Booking, Payment, Wallet, AI, ...)
│   ├── DTOs/             #   Request/response contracts
│   ├── Interfaces/       #   Service abstractions
│   ├── Mappings/         #   Mapster configuration
│   ├── AI/               #   AI validators + embedded prompt templates
│   └── Seed/             #   Database seeding (roles, demo users, categories)
├── Ehgiz.DAL/            # Data access layer
│   ├── Entities/         #   EF Core entities
│   ├── Data/             #   DbContext + configuration
│   ├── Repositories/     #   Generic + specialized repositories
│   ├── UnitOfWork/       #   Transactional coordination
│   └── Migrations/       #   EF Core migrations
└── Ehgiz.Tests/          # xUnit test suite (service-level, SQLite-backed)
```

**Key patterns:** Repository + Unit of Work, DTO mapping via Mapster, uniform `ApiResponse<T>` envelope, extension-method composition in `Program.cs`.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core Web API (.NET 10) |
| Database | SQL Server + Entity Framework Core 10 |
| Auth | ASP.NET Core Identity, JWT Bearer, refresh-token rotation |
| Real-time | SignalR |
| Payments | Stripe.net |
| Email | SendGrid |
| Media storage | Cloudinary |
| AI | Microsoft.Extensions.AI + GitHub Models (GPT-4o / GPT-4o-mini) |
| Mapping | Mapster |
| Logging | Serilog |
| API docs | Swashbuckle (Swagger) |
| Testing | xUnit + SQLite in-memory fixtures |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full instance)
- Accounts/keys for: Stripe, SendGrid, Cloudinary, and a GitHub Models (or OpenAI-compatible) API key — only needed for the features you intend to exercise

### 1. Clone

```bash
git clone https://github.com/<your-org>/Ehgiz_back_end.git
cd Ehgiz_back_end
```

### 2. Configure secrets

Secrets are intentionally blank in `appsettings.json`. The app loads a `.env` file at startup (via DotNetEnv), so create one in `Ehgiz.API/` (or export equivalent environment variables):

```env
ConnectionStrings__DefaultConnection=Server=localhost;Database=EhgizDb;Trusted_Connection=True;TrustServerCertificate=True
Jwt__Key=<random-string-at-least-32-chars>
Stripe__SecretKey=sk_test_...
Stripe__PublishableKey=pk_test_...
Stripe__WebhookSecret=whsec_...
SendGrid__ApiKey=SG....
CloudinarySettings__CloudName=...
CloudinarySettings__ApiKey=...
CloudinarySettings__ApiSecret=...
AI__ApiKey=<github-models-or-openai-key>
SeedUsers__DefaultPassword=<password-for-seeded-demo-users>
```

> Never commit `.env` or real secrets. `appsettings.json` should keep only non-sensitive defaults.

Other notable settings in `appsettings.json`:

| Key | Default | Purpose |
|---|---|---|
| `Jwt:AccessTokenMins` | `60` | Access token lifetime |
| `Jwt:RefreshTokenDays` | `7` | Refresh token lifetime |
| `Platform:FeePercent` | `10` | Platform commission on bookings |
| `Frontend:AllowedOrigins` | `http://localhost:4200` | CORS whitelist |
| `SendGrid:VerificationCodeMins` | `15` | One-time code validity |

### 3. Create the database

```bash
dotnet ef database update --project Ehgiz.DAL --startup-project Ehgiz.API
```

### 4. Run

```bash
dotnet run --project Ehgiz.API
```

- Swagger UI: `https://localhost:<port>/swagger` (Development only)
- Health check: `GET /health`

The database seeder creates roles, demo users, and categories on first run (empty database only).

---

## API Surface

| Area | Base route | Highlights |
|---|---|---|
| Auth | `/api/auth` | Register (multipart w/ profile image), login, refresh, email verification, password reset |
| Tools | `/api/tools` | Search/filter/geo-search, CRUD, images, primary image |
| Bookings | `/api/bookings` | Lifecycle management, handover, issue reports |
| Payments | `/api/payments` | Stripe intents + webhook |
| Wallet | `/api/wallet` | Balance, top-ups (USD), transactions, earnings |
| Messages | `/api/messages` | Conversations and chat history |
| Notifications | `/api/notifications` | List, mark read |
| Reviews | `/api/reviews` | Create/list reviews |
| Saved Searches | `/api/saved-searches` | CRUD + match notifications |
| AI | `/api/ai` | Image validation, photo search, tool assistant |
| Settings | `/api/settings` | Public frontend settings (e.g. platform fee) |
| Admin | `/api/admin` | Platform administration (Admin role) |
| SignalR | `/hubs/chat`, `/hubs/notifications` | Real-time messaging and push notifications |

All endpoints return a consistent `ApiResponse<T>` envelope. Full request/response schemas are available in Swagger.

---

## Testing

```bash
dotnet test
```

The test suite (`Ehgiz.Tests`) covers the application services (auth, bookings, wallet, reviews, saved searches, messaging, notifications, tokens, tools, profiles) using SQLite-backed EF Core fixtures for fast, isolated runs.

---

## Security Notes

- Refresh tokens are HTTP-only cookies scoped to `/api/auth` and rotated on use
- One-time email code endpoints are rate-limited per IP (6 requests / 15 min) **and** per email, with neutral error responses that never reveal whether an account exists
- Request body size capped at 10 MB; upload size/type validated (AI image checks included)
- Role-based authorization on admin endpoints; global exception middleware avoids leaking internals

---

## Docker / Render

Deploy the API as a Docker container on [Render](https://render.com) with an external SQL Server.

```bash
docker build -t ehgiz-api .
docker run -p 8080:8080 --env-file Ehgiz.API/.env -e ASPNETCORE_ENVIRONMENT=Production ehgiz-api
```

Full setup, environment variables, and troubleshooting: [`docs/Docker-Render.md`](docs/Docker-Render.md).

---

## License

This project is proprietary. All rights reserved.
