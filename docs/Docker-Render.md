# Deploying Ehgiz API to Render (Docker)

Backend-only container deployment using an **external hosted SQL Server**. The Angular frontend is deployed separately.

## Prerequisites

- [Render](https://render.com) account
- External SQL Server connection string (reachable from Render's network)
- API keys: Stripe, SendGrid, Cloudinary, GitHub Models (`AI__ApiKey`)

## Files

| File | Purpose |
|------|---------|
| [`Dockerfile`](../Dockerfile) | Multi-stage .NET 10 build + runtime image |
| [`.dockerignore`](../.dockerignore) | Keeps secrets and build artifacts out of the image |
| [`render.yaml`](../render.yaml) | Optional Render Blueprint |

## Local Docker test

From the `Ehgiz` solution root (`d:\GP\backend\Ehgiz`):

```bash
docker build -t ehgiz-api .
docker run -p 8080:8080 --env-file Ehgiz.API/.env -e ASPNETCORE_ENVIRONMENT=Production ehgiz-api
```

Verify:

```bash
curl http://localhost:8080/health
```

On first run against an empty database, migrations apply automatically and demo seed data is inserted (skipped if users already exist).

## Render setup (manual)

1. **New → Web Service** → Connect your Git repo.
2. **Root Directory:** path containing `Dockerfile` (e.g. `Ehgiz` if repo root is `backend`).
3. **Runtime:** Docker.
4. **Dockerfile path:** `./Dockerfile`.
5. **Health Check Path:** `/health`.
6. Add environment variables (see table below).
7. Deploy.

Alternatively, use **Blueprint** with [`render.yaml`](../render.yaml) and fill `sync: false` secrets in the Render dashboard.

## Required environment variables

| Variable | Example / notes |
|----------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `8080` (must match container port in Render service settings) |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__Key` | Random string, at least 32 characters |
| `SeedUsers__DefaultPassword` | Password for seeded demo users (first deploy only) |
| `Stripe__SecretKey` | `sk_test_...` or live key |
| `Stripe__PublishableKey` | `pk_test_...` |
| `Stripe__WebhookSecret` | `whsec_...` |
| `SendGrid__ApiKey` | SendGrid API key |
| `CloudinarySettings__CloudName` | Cloudinary cloud name |
| `CloudinarySettings__ApiKey` | Cloudinary API key |
| `CloudinarySettings__ApiSecret` | Cloudinary API secret |
| `AI__ApiKey` | GitHub PAT with `models:read` |
| `Frontend__BaseUrl` | Deployed Angular URL, e.g. `https://ehgiz-frontend.onrender.com` |
| `Frontend__AllowedOrigins__0` | Same origin as `Frontend__BaseUrl` (CORS) |

Optional overrides:

| Variable | Default in appsettings |
|----------|------------------------|
| `Jwt__AccessTokenMins` | `60` |
| `Jwt__RefreshTokenDays` | `7` |
| `Platform__FeePercent` | `10` |
| `AI__Endpoint` | `https://models.github.ai/inference` |
| `AI__Model` | `openai/gpt-4o-mini` |

**Do not** commit `.env` or bake secrets into the Docker image. Render env vars override `appsettings.json`.

## Startup behavior

On every container start:

1. **EF Core migrations** run (`Database.MigrateAsync()`).
2. **Seed data** runs (`DatabaseSeeder.SeedAsync()`), idempotent — skips if any user exists.

Swagger UI remains **Development only**.

## Post-deploy checklist

- [ ] `GET https://<service>.onrender.com/health` returns `Healthy`
- [ ] CORS: `Frontend__AllowedOrigins__0` matches your Angular origin exactly
- [ ] Stripe webhook: `https://<service>.onrender.com/api/payments/webhook`
- [ ] Angular `environment.ts` `apiUrl` points to `https://<service>.onrender.com`
- [ ] SignalR hubs: `wss://<service>.onrender.com/hubs/chat` (token via `?access_token=`)

## Render + reverse proxy

Production uses `UseForwardedHeaders()` so HTTPS termination at Render's edge works with `UseHttpsRedirection()` and secure cookies.

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Container exits on start | Missing `ConnectionStrings__DefaultConnection` or SQL firewall blocks Render IPs |
| `503` on `/api/ai/assistant` | `AI__ApiKey` not set |
| CORS errors from Angular | `Frontend__AllowedOrigins__0` mismatch (include scheme, no trailing slash) |
| Seed fails | `SeedUsers__DefaultPassword` not set in Production |
| Health check unhealthy | DB unreachable; check connection string and SQL Server firewall |
