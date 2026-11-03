# Tasks

## 1. Solution scaffolding

- [ ] 1.1 Create `Chores.slnx`, `Directory.Build.props` (net10.0, nullable, warnings as errors) and `Directory.Packages.props` with pinned Aspire, EF Core/Npgsql, Mediator, FluentValidation, Quartz and xUnit versions; verify `dotnet build` succeeds on the empty solution
- [ ] 1.2 Create the projects `Chores.Domain`, `Chores.Application`, `Chores.Infrastructure`, `Chores.Api`, `Chores.Worker`, `Chores.MigrationService`, `Chores.ServiceDefaults` and `Chores.AppHost` with the project references from design.md; verify `dotnet build` succeeds
- [ ] 1.3 Create the test projects `Chores.Domain.Tests`, `Chores.Application.Tests`, `Chores.Architecture.Tests` and `Chores.Api.Tests`; verify `dotnet test` runs (zero tests is fine)
- [ ] 1.4 Add architecture tests asserting the layer rules in design.md (Domain has no dependencies, Application only on Domain, Api and Worker independent); verify they pass, and that a deliberate bad reference makes them fail

## 2. Application pipeline

- [ ] 2.1 Register the `Mediator` source generator and a FluentValidation `ValidationBehavior` in `Chores.Application`; verify with a unit test that an invalid request throws `ValidationException` and the handler isn't invoked
- [ ] 2.2 Add a `GetServerInfo` query and handler returning the app version; verify with a unit test

## 3. Persistence

- [ ] 3.1 Add `ChoresDbContext` (Npgsql) in Infrastructure with the initial empty migration; verify `dotnet ef migrations list` shows it
- [ ] 3.2 Add a migration that creates the Quartz.NET PostgreSQL tables in the `quartz` schema from the official DDL for the pinned version; verify the tables exist after migrating a local database
- [ ] 3.3 Implement `Chores.MigrationService` (migrate, then exit with code 0, or non-zero on failure); verify by running it against the Aspire Postgres

## 4. Api and Worker hosts

- [ ] 4.1 In `Chores.ServiceDefaults`, map `/health` and `/alive` in all environments, with a Postgres check tagged for `/health` only and a status-only response writer; verify with `Chores.Api.Tests` (WebApplicationFactory) that `/alive` returns 200, and `/health` returns 503 with no connection details when the database is unreachable (specs: platform/service-health)
- [ ] 4.2 Expose `GET /api/info` in `Chores.Api` through Mediator, mapping `ValidationException` to `400 ProblemDetails`; verify with an Api test
- [ ] 4.3 Configure Quartz in `Chores.Worker` with the clustered Postgres job store and a `HeartbeatJob` every minute; verify the heartbeat log appears in the Aspire dashboard and that two Worker replicas don't both fire it in the same minute

## 5. Web client

- [ ] 5.1 Scaffold `src/web` with Vite + React + TypeScript, ESLint and Vitest; verify `npm run lint`, `npm run typecheck`, `npm test` and `npm run build` pass
- [ ] 5.2 Add `vite-plugin-pwa` with manifest (name, icons 192/512 and maskable, `display: standalone`), shell precaching and an offline fallback; verify with a test that the generated manifest has the required fields, and manually that the built app installs in Chrome and shows the offline message with the network disabled (spec: platform/web-shell)
- [ ] 5.3 Add `react-i18next` with `sv`/`en` resources, browser-language detection with `en` fallback, a persistent language switch, and a Vitest test asserting both locales have identical keys; verify the language scenarios in the platform/web-shell spec with component tests
- [ ] 5.4 Configure the Vite dev proxy for `/api` and `/connect`, and a multi-stage Dockerfile serving the build with nginx (SPA fallback to `index.html`); verify `docker build` succeeds and the container serves the app

## 6. Orchestration and delivery

- [ ] 6.1 Wire the AppHost: Postgres with a data volume, MigrationService, Api and Worker (both `WaitForCompletion` on migrations), and the web client through `AddViteApp`; verify `dotnet run --project src/Chores.AppHost` brings everything up healthy in the dashboard
- [ ] 6.2 Add `AddDockerComposeEnvironment` and verify `aspire publish` produces a `docker-compose.yaml` that starts all services with `docker compose up`
- [ ] 6.3 Add the GitHub Actions workflow (dotnet build + test, client lint, typecheck, test and build); verify it passes on a push
- [ ] 6.4 Write `README.md` covering prerequisites (.NET 10 SDK, Node 22+, Docker), running locally, publishing, and the HTTPS reverse-proxy requirement; verify the documented commands work from a clean clone

## 7. Integration check

- [ ] 7.1 From a clean clone, run the AppHost, open the web client, switch the language, call `/api/info` through the dev proxy, and confirm `/health` is 200 for Api and Worker
- [ ] 7.2 Manually install the PWA on an iOS 16.4+ device from an HTTPS tunnel (for example `cloudflared`) and confirm standalone mode
