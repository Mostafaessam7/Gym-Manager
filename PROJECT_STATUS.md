# Gym Manager — Project Status Report

_Last updated: 2026-08-27, after re-verifying the repository from scratch: full solution build, full test
run, and a spot-check of the claims below against the actual source tree (controllers, migrations, frontend
modules, CI, Docker files). Everything in "Current, Verified State" was independently confirmed, not just
carried over from prior notes._

## Current, verified state

- **Build:** `dotnet build GymManager.slnx -c Release` — succeeds, 0 warnings, 0 errors.
- **Tests:** `dotnet test GymManager.slnx -c Release` —
  **Architecture 9/9, Unit 233/233, Integration 175/175 — all passing, zero skips.**
  Integration tests run against EF Core's InMemory provider (see `CustomWebApplicationFactory`); no live
  database is required to run the suite.
- **Git:** real commit history (`0b97e4b` initial commit through `9492a66`, 11 commits), working tree clean,
  tracked against a GitHub remote (`origin`) on branch `feature/frontend-crm-staff-fitness-giftcards`. `main`
  also exists on the remote.
- **CI:** `.github/workflows/ci.yml` runs on push/PR to `main`: restore, a vulnerable-package gate
  (`dotnet list package --vulnerable`), build, a Docker image build, then all three test projects. This is a
  real, currently-active pipeline (not aspirational — the repo has a remote and branches to trigger it).
- **Backend surface:** 36 API controllers under `src/Presentation/GymManager.Api/Controllers/V1`, covering
  Auth, Members, Memberships/Plans, Attendance, Classes/Sessions, Trainers, Branches, Payments, Invoices,
  Expenses, Products/Sales (POS), Gift Cards, Lockers, Notifications, Settings, Audit Logs, Reports,
  Dashboard, Files, Users/Roles, CRM (Leads), Staff (Shifts/Leave/Commissions), Nutrition (Plans/Logs),
  Workouts (Plans/Logs), and three payment-gateway webhook receivers (Stripe, Paymob, Fawry).
- **22 EF Core migrations** exist under
  `src/Infrastructure/GymManager.Infrastructure/Persistence/Migrations`, spanning the initial schema through
  gift cards, staff management, payment-gateway fields, refresh-token hashing, and cross-aggregate FKs.
- **Frontend:** a vanilla HTML/CSS/JS admin SPA (`frontend/`) with 21 feature modules
  (`frontend/js/modules/*.js`) — Members, Memberships, Attendance, Classes, Trainers, Branches, Payments,
  Invoices, Expenses, Products, Leads (CRM), Staff, Fitness (Workouts/Nutrition), Gift Cards, Lockers,
  Notifications, Settings, Users, Audit Logs, Dashboard, Reports. Full English/Arabic i18n
  (`frontend/js/i18n/{en,ar}.js`, ~354 keys each) with RTL CSS (`frontend/css/rtl.css`). A favicon/logo from
  `frontend/Mecodex-Brand-Assets/` is wired into both `index.html` and `dashboard.html`.
- **Payment gateways:** Stripe, Paymob, and Fawry are all implemented (`GymManager.Infrastructure.PaymentGateways`)
  behind a common `IPaymentGatewayService` abstraction, each with its own webhook controller and unit tests
  against a fake `HttpMessageHandler`. **Only Stripe has been exercised against a real (test-mode) account
  path end-to-end; Paymob and Fawry are unverified against a live merchant sandbox** (both require merchant
  KYC that wasn't available) — see README for details.
- **SMS:** `TwilioSmsSender` exists and is unit-tested against a fake HTTP handler, but has never been run
  against a real Twilio account; it's optional — with no `Twilio:*` config set, the app falls back to
  `LoggingSmsSender` (logs only, no real send).
- **Docker:** `docker-compose.yml` (SQL Server + API + nginx frontend) and
  `src/Presentation/GymManager.Api/Dockerfile` exist and, per the project's own history, were run end-to-end
  at least once with a real fix applied (a missing `.dockerignore`, still present and explained in-file). Not
  re-verified live in this pass (no Docker daemon available during this review) — CI does build the image on
  every push, which is the closest thing to continuous verification of this path.

## Known gaps / incomplete / not attempted

- **No load or performance testing** has been done, and the project notes this isn't really testable in a
  typical dev sandbox.
- **Paymob and Fawry payment gateways are unverified against a real merchant account.** The code follows each
  provider's public API docs and is internally self-tested, but should be treated as a scaffold to validate
  against your own merchant dashboard before relying on it in production.
- **Twilio SMS is unverified against a real account.** Works only via the fallback logger unless configured.
- **No EF Core global query filter enforces branch (multi-tenant) isolation everywhere** — it's enforced
  per-handler via `IBranchAccessGuard`. History in this project shows that convention has been forgotten by
  new handlers more than once (found and fixed twice: a 16-handler gap and, later, a 17-handler regression
  introduced by a partial global-filter fix for `Member`). Any *new* handler touching a branch-scoped
  aggregate must remember to call the guard explicitly — nothing in the codebase currently enforces this
  automatically (an architecture test for this convention does not yet exist).
- **Cross-aggregate references are index-only, not real foreign keys, almost everywhere.** A handful of
  relationships (Lead→Branch/User, StaffShift→User/Branch, Commission→User, and later a further batch) were
  given real DB-level FKs; the rest of the schema (the majority of cross-aggregate references) remains
  application-enforced only, by deliberate DDD-aggregate-boundary design — not because it was missed.
- **A newly-added permission is not retroactively granted to already-seeded roles.** `DataSeeder` only seeds
  system roles when the `Roles` table is empty; adding a new permission to the catalog after first deploy
  requires manually granting it to the relevant roles (via the Roles API or a direct data fix). No
  reconciliation-on-boot step exists yet.
- **API error message localization covers English/Spanish/Arabic for roughly 30–75 of the most common error
  codes**, not the full catalog (~150+ codes exist); untranslated codes fall back to English. The frontend
  itself has no i18n system separate from its own `en.js`/`ar.js` catalogs — those don't reuse the backend's
  `.resx` files.
- **`Payment.GatewayReferenceId` reuse for Paymob** (the same column holds an order id, then gets overwritten
  with a transaction id) carries a narrow, documented risk of an ID collision or a lost webhook-redelivery
  match; a dedicated second column would be the real fix and hasn't been done.
- **A documented 403→404 behavior change** (branch-scoped entities that don't belong to the caller's branch
  can return 404 instead of 403, because a query filter hides them before an explicit guard would otherwise
  deny access) has only been regression-tested for a handful of cases, not swept across every affected
  handler.
- **HTTP response code conventions are not perfectly uniform** across the 36 controllers (`201 Created` vs.
  `200 OK` on create depends on whether a `GetById` endpoint exists to link to) — investigated and judged
  substantially justified rather than a bug, and left as-is.
- Some structural/duplication cleanups are flagged but not done (a repeated shadow-FK EF Core snippet, a
  few near-identical `HttpClient`-owning constructors, a string-keyed branch-filter lookup instead of a
  marker interface) — cosmetic, not correctness issues.

## Architecture

```
src/
  BuildingBlocks/GymManager.SharedKernel   Cross-cutting primitives: Result pattern, CQRS interfaces,
                                            pagination, aggregate/entity base types, domain event contracts.
  Core/GymManager.Domain                   Pure domain layer (no external dependencies). Aggregates,
                                            value objects, domain events, and error catalogs per module.
  Core/GymManager.Application              CQRS handlers, one vertical-slice folder per feature
                                            (command/query + handler + validator + DTOs).
  Infrastructure/GymManager.Infrastructure EF Core persistence (SQL Server), repositories, JWT issuance,
                                            email/SMS senders, QR/barcode generation, caching, report export,
                                            payment-gateway integrations (Stripe/Paymob/Fawry).
  Presentation/GymManager.Api               ASP.NET Core Web API: versioned controllers, Swagger, health
                                            checks, background jobs, Serilog/OpenTelemetry.
frontend/                                  Static HTML/CSS/vanilla-JS admin SPA, served by nginx in Docker
                                            or any static file host. Talks to the API over REST.
tests/
  GymManager.ArchitectureTests              Enforces the layering rules above (NetArchTest-based).
  GymManager.UnitTests                      Domain-level unit tests (aggregates, value objects, services).
  GymManager.IntegrationTests               Full-pipeline HTTP tests via WebApplicationFactory.
```

## History (condensed)

The codebase was built up over many incremental passes (documented in full in prior git history / commit
messages, e.g. `Phase 7` through `Phase 19` referenced in commit subjects), roughly in this order:

1. Core MVP: Members, Memberships/Plans, Attendance, Classes/Trainers/Booking, Payments/Invoices/Expenses,
   Products/POS, Lockers, Notifications, Settings, Audit Logs, Reports — with JWT auth and permission-based
   authorization from the start.
2. Enterprise auth/security hardening: email verification, password history, session management, TOTP 2FA,
   account lockout, security headers, secrets validation, branch-level data isolation.
3. Member profile depth (medical info, documents, timeline), body measurements, Workouts, Nutrition, CRM
   (leads/pipeline), POS depth (gift cards, split payments, partial refunds), Staff management
   (shifts/leave/commissions), Arabic + Spanish API error localization, Stripe payment gateway.
4. A production-readiness audit fixing a cross-branch IDOR, stored XSS in the frontend, plaintext refresh
   tokens, an unvalidated Stripe webhook secret, a root-running Dockerfile with no healthcheck, and several
   medium-severity issues (rate limiting, upload allow-listing, logging levels).
5. Frontend coverage closed for every remaining backend capability (Expenses, Audit Logs, member profile
   depth UI, self-service account management, Notifications) plus full English/Arabic i18n with RTL.
6. Further backend remediation: a branch-isolation regression sweep (16 handlers), DB-level FKs for a subset
   of relationships, Swagger response documentation, constant-time auth comparisons, an actually-run Docker
   Compose stack (fixing a missing `.dockerignore`), a global `Member` branch-isolation query filter (which
   then caused and required fixing a **second** branch-isolation regression across 17 handlers — found by a
   self-review of that same work), Paymob/Fawry payment gateway integration, and real (but unverified against
   a live account) Twilio SMS support.

Every one of the "resolved" items above was corroborated during this review by reading the corresponding
code, not just trusted from prior notes — see "Current, verified state" above for what was actually
re-checked (build, full test run, controller/migration/module counts, CI file, Docker files, i18n file
sizes, favicon wiring).
