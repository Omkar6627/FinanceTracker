# FinanceTracker

A cross-platform personal & organisational finance tracker built from a single codebase. It runs as a Progressive Web App today and is architected to extend to native mobile and desktop without duplicating code.

FinanceTracker ships in two product modes that share the same backend, database, and frontend:

- **Individual Mode** — personal expense tracking, budgets, and reports for a single user.
- **Enterprise Mode** — organisation-level finance management with departments, role-based access control, an approval workflow, and an audit trail.

Mode is a property of the organisation, not a separate deployment. Every account starts as a single-user Individual organisation and can switch to Enterprise in place, without touching existing data.

---

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Angular 17 + Ionic 7 (NgModule-based), Chart.js, PWA (service worker + manifest) |
| Backend | .NET 6 Minimal API, Clean Architecture |
| ORM | EF Core 6 (code-first, global query filters for multi-tenancy) |
| Database | PostgreSQL (production) / EF Core InMemory (local dev) |
| Auth | JWT (access + refresh), BCrypt password hashing, role/claim-based authorization |

---

## Architecture

The backend follows Clean Architecture with a strict inward dependency rule:

```
Api  ->  Application  ->  Domain
Infrastructure  ->  Application   (implements its interfaces)
Nothing points inward at Infrastructure or Api.
```

```
backend/
  src/
    FinanceTracker.Domain          # Entities, enums, value objects (no dependencies)
    FinanceTracker.Application      # Services, DTOs, abstractions, Result<T>
    FinanceTracker.Infrastructure   # EF Core, persistence, security, DI wiring
    FinanceTracker.Api              # Minimal endpoints, middleware, auth pipeline
  tests/
    FinanceTracker.UnitTests
    FinanceTracker.IntegrationTests
```

### Multi-tenancy

Every tenant-scoped entity carries an `OrganisationId`. EF Core global query filters scope every query automatically, so there are no manual `WHERE organisation_id = ?` clauses scattered through the code. Tenant isolation is enforced at the data-access layer.

### Role-based access control (Enterprise)

Five roles — **Owner, Admin, Manager, Member, Viewer** — map to a permission matrix (`transaction.create`, `transaction.approve`, `member.invite`, `budget.manage`, `report.view`, `audit.view`, `settings.manage`, …). Permission checks run through a single `IPermissionService` rather than inline role comparisons. In Individual mode the sole user resolves as Owner-equivalent, so Individual mode is a strict subset of Enterprise with no special-casing in the domain.

### Approval workflow

Transactions move through a status state machine: `Draft → PendingApproval → Approved | Rejected`, with `AutoApproved` for Individual mode (and below-threshold amounts). In Individual mode the workflow code exists but is bypassed — every transaction is auto-approved on creation.

---

## Current functionality

### Backend
- Auth: register (auto-creates an Individual organisation with the user as Owner), login, JWT refresh, logout, invitation accept.
- Transactions: full CRUD, plus submit / approve / reject and a pending-approval queue for Enterprise.
- Categories, Budgets (personal and department-level), Reports (monthly, category breakdown, trends, department summary).
- Organisation settings and Individual ↔ Enterprise mode switch.
- Members (invite via secure token with TTL, role change, remove) and Departments (CRUD, assign manager) for Enterprise.
- Audit log written on state changes and queryable by Admin+.

### Frontend
- **Individual:** dashboard (net summary, today cards, budget rings, top-spending, recent list), date-grouped transactions list with filters, budgets with progress bars, reports (bar + doughnut charts with month navigation), settings with dark/light theme toggle.
- **Enterprise:** approvals queue, members list + invite flow, departments, audit log, plus a role badge, status chip, and a `hasPermission` directive that hides UI the current role can't use.
- Design system with light/dark CSS variables and shared utility classes.

---

## Roadmap

### Phase 1 — Core Individual Mode ✅ (complete)
Clean Architecture solution, auth with auto-created Individual org, CRUD for transactions/categories/budgets, individual dashboard, monthly reports, PWA manifest + service worker.

### Phase 2 — Enterprise Mode ✅ (complete)
Mode switching, RBAC with permission matrix, department management, member invite flow, transaction approval workflow, approval queue UI, department budgets, enterprise dashboard, audit log, department reports.

### Phase 3 — Polish (both modes) — planned
- CSV import (upload → column mapper → preview → import) and bulk export.
- Recurring transactions.
- Multi-currency support.
- Data export (CSV, PDF summary).
- Capacitor build → Android APK.

### Phase 4 — Bank Link — planned
- Account Aggregator (India) and Plaid (US/EU) integrations.
- OAuth consent flow + account linking UI.
- Background sync via a .NET hosted service.
- Duplicate detection via an idempotency key (`external_ref`) so repeated syncs never double-insert.

The schema already carries the `Account` entity and `external_ref` field, so this phase needs no migration changes to the existing tables.

### Phase 5 — Desktop + SSO — planned
- Electron wrapper → Windows/macOS/Linux installer with system-tray quick-add and auto-update.
- SSO / SAML for Enterprise (Okta, Azure AD).

### Cross-platform delivery
```
Angular + Ionic
    |-- PWA        -> Chrome, Edge, Safari (installable)        [today]
    |-- Capacitor  -> Android / iOS native app                 [Phase 3]
    |-- Electron   -> Windows / macOS / Linux desktop           [Phase 5]
```

---

## Running locally

### Backend
Set the Development environment (uses the InMemory database; data resets on restart):

```bash
cd backend
ASPNETCORE_ENVIRONMENT=Development dotnet run \
  --project src/FinanceTracker.Api/FinanceTracker.Api.csproj \
  --no-launch-profile --urls http://localhost:5229
```

- Health check: `GET http://localhost:5229/api/v1/health`
- Swagger UI at `/swagger`.
- Without `ASPNETCORE_ENVIRONMENT=Development`, it loads production config and expects PostgreSQL.

### Frontend

```bash
cd frontend
npx ng serve --port 4200 --host 127.0.0.1
```

API base URL is `http://localhost:5229/api/v1` (`frontend/src/environments/environment.ts`).

### Tests

```bash
cd backend && dotnet test
```

---

## API surface

Base URL: `/api/v1`

- **Auth:** `register`, `login`, `refresh`, `logout`, `invite/accept`
- **Transactions:** CRUD + `submit`, `approve`, `reject`, `pending`
- **Categories / Budgets / Reports:** CRUD and query endpoints (monthly, breakdown, trends, departments)
- **Organisation:** get, update, mode switch
- **Members / Departments / Audit:** Enterprise management endpoints

See the source under `backend/src/FinanceTracker.Api/Endpoints/` for the full list.
