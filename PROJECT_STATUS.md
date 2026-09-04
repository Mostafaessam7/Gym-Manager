# Gym Manager — Project Status Report

_Last updated: 2026-08-27, after re-verifying the repository from scratch: full solution build, full test
run, and a spot-check of the claims below against the actual source tree (controllers, migrations, frontend
modules, CI, Docker files). Everything in "Current, Verified State" was independently confirmed, not just
carried over from prior notes._

## Current, verified state

- **Build:** `dotnet build GymManager.slnx -c Release` — succeeds, 0 warnings, 0 errors.
- **Tests:** `dotnet test GymManager.slnx -c Release` —
  **Architecture 11/11, Unit 233/233, Integration 175/175 — all passing, zero skips.**
  (Architecture went 9 → 11 on 2026-08-28 with the branch-isolation convention test and its self-test.)
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
  aggregate must remember to call the guard explicitly.

  **Update (2026-08-28): the specific shape that caused both regressions is now caught automatically.**
  `tests/GymManager.ArchitectureTests/BranchIsolationConventionTests.cs` scans the Application layer's
  source for an authorization-only member lookup that runs *through* the global branch filter and then
  guards on `if (member is not null)`. That combination is the hole: the filter hides a cross-branch
  member, the lookup returns `null`, and the null-check skips `EnsureCanAccess` entirely instead of
  denying access. It requires `.IgnoreQueryFilters()` (or `GetBranchIdForAuthorizationAsync`) on such
  lookups, and reports the offending file and line.

  Deliberately narrow: a filtered lookup whose `null` path *returns* (`if (member is null) return
  Failure(NotFound)`) is not flagged — access is still denied there, the filter just turns a 403 into a
  404. It also can't prove a brand-new handler remembered the guard at all; it closes the specific,
  twice-shipped hole rather than the whole convention. Verified by reintroducing the bug into a real
  handler and confirming the test fails naming that file and line, and by a self-test that pins the
  detector against the vulnerable, fixed, and safe-early-return shapes so it can't silently stop matching.
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


## Decisions adopted (workspace-level)

| Decision | What it means here |
|---|---|
| **Azure** is the primary deployment target | Not wired yet |
| **Azure Key Vault** for production secrets | Not wired yet. Today: env vars + placeholder detection that refuses to start outside Development |
| **Redis** belongs here | One of the three products scoped for it (with PosFlow and RealEstateCRM). **Not yet added** |
| **App Insights (backend) + Sentry (frontend)** | Not installed yet |
| **Slate Professional theme** | This product's identity on the shared `MeCodex/design-system` token architecture |
| **Angular Material not applicable** | The frontend is vanilla ES modules, not Angular. It consumes the shared token CSS directly |

## Recent work (2026-08-29 pass)

Undocumented anywhere until this cleanup, despite being the five most recent commits:

- **Refresh token moved into an HttpOnly cookie, with CSRF double-submit protection.** The access
  token stays in memory behind an `Authorization` header. See the README's "How the two tokens are
  held" for why the two differ.
- **`AllowCredentials()` added to the CORS policy.** This is the part worth remembering: the cookie
  migration made the browser send credentials, but CORS never allowed them, so the browser
  discarded every auth response before the app saw it and **login was broken**. All 425 server-side
  tests passed throughout — the rule is enforced by the browser, not the server, so no server-side
  test could catch it. It surfaced only because a sibling project's Playwright suite drove a real
  browser.
- **Shared design system, Slate Professional theme.**
- **An architecture test that catches the branch-isolation bypass which had shipped twice.** It
  catches that one bypass *shape*; it does not catch a handler that simply forgets to call
  `IBranchAccessGuard`. That limitation is real and stated in "Known gaps".
- **Dependabot** configured.

## Deliberately deferred (and why)

| Item | Why |
|---|---|
| **Global branch-isolation filter for every aggregate** | A `Member` global query filter already exists. Extending it to every branch-scoped aggregate caused a **second** branch-isolation regression across 17 handlers last time it was attempted. It is the right end state, but it needs its own focused piece of work with tests written first — not a cleanup-pass change |
| **Redis** | Agreed for this product, but adding a cache is a behavioural change needing its own verification, not documentation tidying |
| **Verifying Paymob/Fawry and Twilio against live accounts** | Needs real merchant and Twilio credentials. Implemented and self-tested; explicitly unverified |
| **Load/performance testing** | None has been done. Recorded rather than guessed at |
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

## Redis (2026-08-29)

`ICacheService` now has two implementations, chosen by configuration:

| `ConnectionStrings:Redis` | Implementation | Where |
|---|---|---|
| set | `RedisCacheService` | production / multi-instance |
| unset | `MemoryCacheService` (unchanged) | local dev, CI, tests |

**Why it mattered.** `MemoryCacheService` is per-process. On a single instance that is correct and
cheaper. On more than one, invalidation stops crossing instances: a branch or plan edit clears the
cache on the instance that handled the write, and every other instance keeps serving its own stale
copy until expiry. Nothing errors — the same request just returns different answers depending on
which instance replies.

**Why `StackExchange.Redis` directly and not `IDistributedCache`.** `RemoveByPrefix` has to
enumerate keys, and `IDistributedCache` has no such API. The in-memory implementation works around
that with a local dictionary of keys, which would put us straight back to per-process behaviour for
exactly the invalidation that matters most — plan and trainer caches fan out over branch ids and are
cleared by prefix. The Redis implementation uses `SCAN` (via `server.Keys`), not `KEYS`, because
`KEYS` blocks the server for the whole scan.

**Covered by tests against a real Redis** (`tests/GymManager.IntegrationTests/Caching/`). They skip
themselves when nothing is listening on `localhost:6379`, so CI stays green — an honest trade,
since it means the Redis path is only truly covered on a machine running Redis. Verified locally
against a live server (3 ran, 0 skipped), and `RemoveByPrefix` was confirmed to fail the suite when
its deletion is removed.

**Still needed from you:** point `ConnectionStrings:Redis` at a real server before running more than
one instance. Until then the memory implementation is used and behaviour is unchanged.

## Key Vault, App Insights and Sentry (2026-08-30)

All three are wired and **inert until configured** — each registers only when its value is present,
so nothing changes for a deployment that supplies none of them.

| Feature | Enabled by |
|---|---|
| Azure Key Vault | `KeyVault__Uri` (registered above `SecretsValidator`, so vault values count as configured) |
| Application Insights | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| Sentry (frontend) | `window.__GYM_SENTRY_DSN__` |

The frontend needed a different approach from the other products, and it is worth knowing why
before changing it. It has no build step and no npm dependencies, so it currently loads **zero**
third-party code. Vendoring Sentry's 148 kB bundle would put it outside any dependency management;
loading it from a CDN unpinned would hand whoever controls that CDN script execution inside an
authenticated admin session. It is therefore loaded from the official CDN at a **pinned version
with Subresource Integrity** — verified in a real browser both ways: the correct hash loads, and a
deliberately wrong hash is rejected.

Changing the pinned Sentry version means recomputing the integrity hash. The command is in
`frontend/js/errorReporting.js`, and a mismatch fails closed — Sentry does not load and the app
carries on without it.

**Still open:** the branch-isolation global filter for every aggregate, live-account verification
for Paymob/Fawry and Twilio, and load testing. Branch protection is **not** available on this repo:
it is private, and GitHub requires Pro for protection on private repositories.

---

## Update 2026-09-04 — one branch, protected; routine dependency PRs off

**This repo keeps a single branch: `main`.** Any leftover Dependabot branches were deleted and no
long-lived branches are kept.

**`main` is protected**, and the protection is deliberately the kind that fits a one-branch
workflow:

| Setting | Value | Why |
|---|---|---|
| Force pushes | **blocked** | History cannot be rewritten or silently rolled back. Verified by attempting one and having it rejected |
| Branch deletion | **blocked** | `main` cannot be removed |
| Applies to admins | **yes** | The owner is not exempt; that exemption was the hole fixed on E-Commerce earlier |
| Required status checks | **none** | Deliberate trade-off. Required checks make direct pushes to `main` impossible and force every change through a branch and PR, which is exactly what the one-branch decision rules out. CI still runs on every push — it reports rather than gates |

**Routine dependency PRs are off.** Every `open-pull-requests-limit` in `.github/dependabot.yml` is
`0`, because weekly version bumps meant a continuous stream of branches to merge or close.
**Security updates are unaffected** — Dependabot ignores that limit for security advisories, so a
dependency with a known vulnerability still opens a PR. Set the limits back to a non-zero number to
bring routine updates back.
