# Gym Manager

An enterprise gym management system: a .NET 10 Clean/Onion-architecture backend (CQRS, EF Core/SQL Server,
JWT auth with dynamic permission-based authorization) and a vanilla HTML/CSS/JS admin frontend.

## Architecture

```
src/
  BuildingBlocks/GymManager.SharedKernel   Cross-cutting primitives: Result pattern, CQRS interfaces,
                                            pagination, aggregate/entity base types, domain event contracts.
  Core/GymManager.Domain                   Pure domain layer (no external dependencies). Aggregates,
                                            value objects, domain events, and error catalogs per module.
  Core/GymManager.Application              CQRS handlers, one vertical-slice folder per feature
                                            (command/query + handler + validator + DTOs).
  Infrastructure/GymManager.Infrastructure  EF Core persistence (SQL Server), repositories, JWT issuance,
                                            email/SMS senders, QR/barcode generation, caching, report export.
  Presentation/GymManager.Api              ASP.NET Core Web API: versioned controllers, Swagger, health
                                            checks, background jobs, Serilog/OpenTelemetry.
frontend/                                  Static HTML/CSS/vanilla-JS admin SPA, served by nginx in Docker
                                            or any static file host. Talks to the API over REST.
tests/
  GymManager.ArchitectureTests              Enforces the layering rules above (NetArchTest-based).
  GymManager.UnitTests                      Domain-level unit tests (aggregates, value objects, services).
  GymManager.IntegrationTests               Full-pipeline HTTP tests via WebApplicationFactory.
```

Every feature area (Members, Memberships, Attendance, Classes, Trainers, Payments, Invoices, Expenses,
Products/POS, Lockers, Notifications, Settings, Audit Logs, Reports) follows the same shape: a Domain
aggregate + errors, an Application CQRS slice, an Infrastructure EF configuration + repository, and an API
controller gated by a specific permission (see `Permissions.cs` in `GymManager.Domain.Identity`).

## Running locally

### Option A — Docker Compose (recommended)

```bash
docker-compose up --build
```

This starts SQL Server, the API (migrations run automatically on startup), and an nginx container serving
the frontend. Once healthy:

- API: http://localhost:8080/swagger
- Frontend: http://localhost:5500

### Option B — dotnet + your own SQL Server

1. Point `ConnectionStrings:GymManagerDatabase` in `src/Presentation/GymManager.Api/appsettings.json` (or an
   environment variable, see below) at a reachable SQL Server instance.

   If you already have a local SQL Server instance (e.g. SQL Server Developer/Express installed directly on
   Windows) rather than Docker, its TCP/IP protocol is often disabled by default — check with
   `Test-NetConnection localhost -Port 1433`. Rather than enabling TCP/IP (a system-level change), the
   simplest fix is to connect via Windows Authentication instead, which works over Shared Memory/Named Pipes
   without any SQL Server configuration change:
   ```json
   "ConnectionStrings": {
     "GymManagerDatabase": "Server=localhost;Database=GymManagerDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
   }
   ```
   `src/Presentation/GymManager.Api/appsettings.Development.json` is a good place for this override.
2. Run the API:
   ```bash
   dotnet run --project src/Presentation/GymManager.Api
   ```
   Migrations and the default seed data (see below) run automatically on startup outside the `Testing`
   environment.
3. Serve `frontend/` with any static file server (or open `frontend/index.html` directly) and set
   `window.__GYM_API_BASE_URL__` if the API isn't at `http://localhost:8080/api/v1`.

### Default credentials

A first-run seed creates:

- **Owner login:** `admin@gymmanager.local` / `Admin@12345` (Development/Testing only — see below)
- System roles: Owner, Manager, Front Desk, Trainer (see `DataSeeder.cs` for exact permission grants)
- A default "Main Branch"

Outside `Development`/`Testing`, `SecretsValidator` refuses to start unless `Seed__AdminPassword` (and
optionally `Seed__AdminEmail`) has been set to something other than the well-known default above — this
guarantees a real deployment can never silently ship with a publicly-known, fully-privileged Owner account.

## Configuration & secrets

`appsettings.json` ships with dev-time placeholder values (JWT signing key, SQL password) intended only for
local/Docker Compose use. Outside `Development`/`Testing`, the API refuses to start if those placeholders
are still in effect (see `SecretsValidator` in `GymManager.Api/Configuration`) — supply real values via
environment variables using the standard ASP.NET Core double-underscore convention, e.g.:

```bash
export Jwt__SecretKey="<a real random secret, at least 32 characters>"
export ConnectionStrings__GymManagerDatabase="Server=...;Database=...;User Id=...;Password=...;..."
```

or wire a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.) into `IConfiguration` before
`AddJwtAuthentication`/`AddInfrastructure` run.

Other environment variables of note:

| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Swagger UI and relaxes the secrets check; anything else enforces it and enables HSTS. |
| `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience` | JWT signing configuration. |
| `ConnectionStrings__GymManagerDatabase` | SQL Server connection string. |
| `Email__SmtpHost`, `Email__SmtpPort`, `Email__Username`, `Email__Password` | Outbound email (welcome messages, receipts, reminders). |
| `FeatureManagement__OnlineClassBooking`, `FeatureManagement__PosModule` | Feature flags gating class booking and the POS/sales endpoints. |
| `Stripe__SecretKey`, `Stripe__WebhookSecret` | Stripe integration (see below); `Stripe__PublishableKey` is public by design and not secrets-checked. |
| `Seed__AdminEmail`, `Seed__AdminPassword` | Overrides the seeded first-run Owner account's credentials. `Seed__AdminPassword` is required outside Development/Testing. |

## Stripe payment gateway

`POST /api/v1/payments/gateway-intent` starts a card payment via Stripe and `POST /api/v1/webhooks/stripe`
receives its outcome asynchronously — the `Payment` stays `Pending` until the webhook confirms it, unlike
`POST /api/v1/payments` (cash/manually-recorded payments, which are created already-settled).

To use it:

1. Create a free Stripe account (no business verification needed for test mode) and, from the dashboard in
   **test mode**, copy the **Secret key** (`sk_test_...`) and **Publishable key** (`pk_test_...`) from
   Developers → API keys.
2. Set up a webhook endpoint (Developers → Webhooks) pointing at
   `https://<your-host>/api/v1/webhooks/stripe`, subscribed to at least `payment_intent.succeeded` and
   `payment_intent.payment_failed`. Copy its **Signing secret** (`whsec_...`).
3. Set all three via environment variables. `appsettings.json` ships with placeholder values for all three;
   `Stripe:SecretKey` and `Stripe:WebhookSecret` fail `SecretsValidator`'s startup check outside
   Development/Testing (the same as the JWT key and DB password) if left as placeholders — `Stripe:PublishableKey`
   is not secret and isn't checked, but must still be set for Stripe.js on the frontend to work:

   ```bash
   export Stripe__SecretKey="sk_test_..."
   export Stripe__PublishableKey="pk_test_..."
   export Stripe__WebhookSecret="whsec_..."
   ```

Test-mode keys work identically to live keys for every code path this integration exercises — only the key
values differ, so there is no separate "sandbox mode" toggle. Use Stripe's
[test card numbers](https://stripe.com/docs/testing) (e.g. `4242 4242 4242 4242`, any future expiry, any CVC)
against the `clientSecret` returned by `gateway-intent` to exercise the flow end-to-end without moving real
money. Only Stripe is wired up today; Paymob/Fawry are unimplemented — the `IPaymentGatewayService`
abstraction in `GymManager.Application.Abstractions` is provider-agnostic, so adding one means implementing
that interface, not changing any command/controller code.

## Running tests

```bash
dotnet test
```

Runs architecture, unit, and integration tests. Integration tests substitute EF Core's InMemory provider for
SQL Server (see `CustomWebApplicationFactory`), since no database is required to run them. All tests pass
against InMemory and have also been verified against a real SQL Server instance — see `PROJECT_STATUS.md`
for a real concurrency bug this project once hit and fixed, as a reminder that "should work the same against
the real database" is worth actually verifying, not assuming.

## API surface

Full endpoint documentation is generated by Swashbuckle and served at `/swagger` when running in
Development. All endpoints are versioned (`/api/v1/...`) and require a bearer token except the
`/auth/*` endpoints (`register`, `login`, `refresh`, `logout`, `password-reset/request`,
`password-reset/confirm`).
