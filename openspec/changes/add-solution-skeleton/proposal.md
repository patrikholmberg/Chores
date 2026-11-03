# Proposal

## Why

Every later change (Family auth, Chore Library, Settlement, Wallet, notifications) needs the same foundation: a layered .NET 10 solution, a running PostgreSQL, a Worker host for scheduled jobs and an installable React PWA, all orchestrated by Aspire. Building it once, empty but runnable and tested, means feature changes add behaviour instead of plumbing.

## What Changes

- Create a .NET 10 solution with Clean Architecture projects: `Domain`, `Application`, `Infrastructure`, `Api`, `Worker`, plus `AppHost` (Aspire) and `ServiceDefaults`.
- Enforce layer dependencies (Domain depends on nothing; Application only on Domain; Infrastructure on Application; Api and Worker compose them) with an architecture test.
- Wire the `Mediator` source generator and a FluentValidation pipeline behavior in Application, proven by one trivial query.
- Add EF Core with Npgsql, an empty `ChoresDbContext`, the initial migration, and automatic migration on startup in development.
- Add Quartz.NET to the Worker with the Postgres job store and a no-op heartbeat job proving that clustered scheduling works.
- Add health (`/health`) and liveness (`/alive`) endpoints to Api and Worker, reporting database connectivity.
- Scaffold a Vite + React + TypeScript client that is an installable PWA (manifest, service worker, icons), with react-i18next configured for Swedish and English and a language switch.
- Orchestrate Postgres, Api, Worker and the web client in the Aspire AppHost. `dotnet run` on the AppHost starts everything.
- Configure Aspire Docker Compose publishing so every service ships as a container.
- Add CI (GitHub Actions) that builds, runs the .NET and client tests, and lints the client.

### Out of scope

- Any domain entity, Family, Member, login or authentication (next change: Family and Member auth).
- Web Push, VAPID keys and notification jobs.
- Reverse proxy and TLS configuration for production hosting (documented as a deployment prerequisite only).
- Production secrets management beyond Aspire parameters.

## Capabilities

### New Capabilities
- `platform/service-health`: health and liveness reporting of the Api and Worker services, including database reachability.
- `platform/web-shell`: the installable PWA shell of the client, including language selection between Swedish and English.

### Modified Capabilities
<!-- None: no existing specs. -->

## Impact

- New repository layout: `src/` (backend projects and `web/`), `tests/`, `.github/workflows/`.
- Dependencies: Aspire 13.x, EF Core 10 + Npgsql, Mediator (martinothamar), FluentValidation, Quartz.NET, xUnit, NetArchTest (or equivalent), Vite, React, vite-plugin-pwa, react-i18next, Vitest.
- Developers need the .NET 10 SDK, Node 22+, and Docker (for Postgres via Aspire).
