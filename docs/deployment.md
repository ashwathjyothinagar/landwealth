# Deployment runbook

Local development uses the passwords in `docker-compose.yml` and `src/LandWealth.Api/appsettings.json`. Replace the MySQL password and `JwtSettings:SecretKey` before any shared deployment. The JWT secret must be at least 32 characters.

## Prerequisites

- .NET 10 SDK
- Node.js 20
- Docker, when running MySQL or the full stack

## Local development

1. Start MySQL 8.4 and wait until it is healthy:

```powershell
docker compose up -d db
```

2. Apply the schema from the repository root. The API does not migrate on startup.

```powershell
dotnet ef database update --project src/LandWealth.Infrastructure --startup-project src/LandWealth.Api
```

3. Start the API on http://localhost:5000 and the web app on http://localhost:3000:

```powershell
dotnet run --project src/LandWealth.Api --launch-profile http
npm --prefix src/landwealth-web run dev
```

The Vite dev server proxies `/api` to the API. Register the first user at http://localhost:3000/register.

## Containers

`docker compose up -d db` must be healthy, and the EF migration above must have been applied to `localhost:3306`, before the API container is used. Then:

```powershell
docker compose up -d --build api web
```

The web container serves the built app on http://localhost:3000 and proxies `/api` to the API. Document files stay in the `document_storage` volume. Do not publish that directory over HTTP.

## Configuration

- `ConnectionStrings:DefaultConnection` — MySQL connection. The compose file passes `ConnectionStrings__DefaultConnection`.
- `JwtSettings:SecretKey` — signing key. Expired tokens and tokens that are not HMAC-SHA256 are rejected.
- `JwtSettings:ExpiryMinutes` — access token lifetime.
- `StorageSettings:DocumentRoot` — directory for GUID-named document files.
- `RateLimiting:AuthPermitLimit` — login and registration attempts per minute per address. The default is 20.

## Launch check

1. `GET http://localhost:5000/api/health` returns status `Healthy`.
2. Register and sign in.
3. Amounts on the dashboard and reports use Indian grouping, for example `₹12,34,567.00`. The API returns the unformatted decimal.
4. A second user cannot open the first user's property.

## Verification

```powershell
dotnet test
npm --prefix src/landwealth-web test
```

`LaunchSmokeTests` walks registration, a bank account, a property purchase of ₹12,34,567.50, a valuation, the dashboard, and the audit log.
