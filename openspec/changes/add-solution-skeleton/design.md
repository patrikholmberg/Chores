# Design

## Context

The repository contains only the domain glossary (`CONTEXT.md`), ADRs and OpenSpec setup. The stack is fixed by project context (.NET 10, Aspire, PostgreSQL, Clean Architecture, Mediator, Quartz.NET, React PWA). This change makes those choices concrete as projects, wiring and build pipeline. See proposal.md for motivation and the specs for the observable behaviour (health endpoints, PWA shell, language selection).

## Goals / Non-Goals

**Goals:**
- One command (`dotnet run --project src/Chores.AppHost`) starts Postgres, migrations, Api, Worker and the web client.
- Layer rules are enforced by tests, not only by convention.
- Every runtime piece (Mediator pipeline, EF migrations, Quartz clustering, PWA, i18n) is proven by one minimal working example, so feature changes only add to it.

**Non-Goals:**
- Choosing an email, push or reverse-proxy implementation.
- Production observability backends. Aspire's OpenTelemetry defaults and the dashboard are enough for now.

## Decisions

### Solution layout
```
src/
  Chores.Domain/           Domain layer: entities, value objects, domain rules. No package references.
  Chores.Application/      Application layer: commands/queries, Mediator handlers, validators, ports (interfaces)
  Chores.Infrastructure/   Infrastructure layer: EF Core DbContext + migrations, Quartz store, adapter implementations
  Chores.Api/              Presentation layer: ASP.NET Core minimal API endpoints
  Chores.Worker/           Presentation layer (host): Quartz scheduler and jobs
  Chores.MigrationService/ Host: applies EF migrations, then exits
  Chores.ServiceDefaults/  Aspire service defaults: OpenTelemetry, health checks, resilience
  Chores.AppHost/          Aspire orchestration
  web/                     Vite + React + TypeScript PWA
tests/
  Chores.Domain.Tests/  Chores.Application.Tests/  Chores.Architecture.Tests/  Chores.Api.Tests/
```
Use the `.slnx` solution format, `Directory.Build.props` (nullable, warnings as errors, `net10.0`) and Central Package Management (`Directory.Packages.props`).
*Alternative:* one project per feature slice. Rejected because the project context fixes Clean Architecture layers. Feature folders live inside Application instead.

### Migrations run in a dedicated MigrationService
The AppHost starts `Chores.MigrationService`, which runs `Database.MigrateAsync()` and exits. Api and Worker use `.WaitForCompletion(migrations)`.
*Alternative:* migrate on Api startup. Rejected because Api and Worker would race once Worker replicas exist, and production startup would mix concerns. The same container runs as a one-shot service in Docker Compose.

### Quartz uses the Postgres job store in its own schema
The Quartz tables live in a `quartz` schema of the same database, created by an EF migration that embeds Quartz's official PostgreSQL DDL. Clustering is enabled (`quartz.jobStore.clustered = true`), so scaling Worker replicas never double-fires a job. A `HeartbeatJob` logs once per minute to prove the wiring.
*Alternative:* a RAM job store. Rejected because it loses schedules on restart and can't be clustered.

### Mediator + validation pipeline
`Mediator` (martinothamar, source-generated) is registered in Application with a `ValidationBehavior<TMessage,TResponse>` that runs every FluentValidation validator and turns failures into a `ValidationException`. Api maps that to RFC 7807 `400 ProblemDetails`. A `GetServerInfo` query (returns app version) proves the path end to end. It will be removed or kept as `/api/info`.

### Health endpoints are mapped in all environments
Aspire's ServiceDefaults template maps `/health` and `/alive` only in Development. We map them in every environment (Docker Compose healthchecks need them) and use a response writer that returns only the status (see the `platform/service-health` spec). The Postgres check is tagged so that it applies to `/health` but not `/alive`.

### Web client hosting
- **Dev:** the AppHost runs Vite through `AddViteApp`, and Vite proxies `/api` and `/connect` to the Api. That keeps the setup same-origin and avoids CORS.
- **Prod:** a multi-stage Dockerfile builds static files and serves them with nginx. The HTTPS reverse proxy in front routes `/api` and `/connect` to Api and everything else to web. Keeping same-origin also simplifies the OIDC redirect and cookie setup in the next change.
- **PWA:** `vite-plugin-pwa` (Workbox `generateSW`) handles the manifest, precaching of the shell and an offline fallback. Using `injectManifest` later for Web Push is a non-breaking switch.
- **i18n:** `react-i18next` with `i18next-browser-languagedetector` (order: localStorage, navigator; fallback `en`; supported `sv`, `en`). JSON resources live in `web/src/locales/{sv,en}/`. A Vitest test asserts that both locales have identical key sets.

### Architecture tests
`Chores.Architecture.Tests` uses NetArchTest.eNhancedEdition to assert:
- Domain references no other project and no EF, ASP.NET or Mediator package.
- Application references only Domain.
- Api and Worker don't reference each other.

### Docker Compose publishing
`builder.AddDockerComposeEnvironment("chores")` is set in the AppHost, and `aspire publish` emits `docker-compose.yaml` + `.env`. Postgres uses a named volume. Secrets such as the Postgres password are Aspire parameters.

### CI
A GitHub Actions workflow builds the solution and runs `dotnet test` (the architecture and unit tests need no Docker). It also runs `npm ci`, lint, typecheck, `vitest run` and `vite build` for the client.

## Risks / Trade-offs

- [Aspire and `AddViteApp` APIs still change between minor versions] → Pin the Aspire SDK version in `Directory.Packages.props` and upgrade deliberately.
- [The Quartz DDL is embedded in an EF migration and must match the Quartz.NET version] → Pin Quartz and add a comment in the migration naming the source DDL version.
- [iOS PWA install can't be tested in CI] → Manual check listed in tasks. The installability criteria (manifest fields, icons) are covered by a Lighthouse-style check, or a manifest unit test if that is unavailable.
- [The health endpoints are public] → They return only a status (spec requirement), and the reverse proxy can block them externally.

## Migration Plan

Greenfield, so there's nothing to migrate. Deployment: `aspire publish -p docker-compose`, then `docker compose up -d` on the host behind the TLS proxy.
