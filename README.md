# Chores

A gamified family chore tracker. Children earn or lose points depending on how many chores they complete each day, as configured by their parents, and spend them on rewards.

- Domain language: [CONTEXT.md](CONTEXT.md)
- Architecture decisions: [docs/adr](docs/adr)
- Planned and in-flight work: [openspec](openspec) (specs in `openspec/specs`, changes in `openspec/changes`)

## Prerequisites

- .NET 10 SDK (pinned in `global.json`)
- Node.js 22+
- Docker (Aspire runs PostgreSQL in a container)
- Optional: the [Aspire CLI](https://aspire.dev) for publishing and deploying

## Run locally

```bash
dotnet tool restore                      # dotnet-ef
(cd src/web && npm install)
dotnet run --project src/Chores.AppHost  # or: aspire run
```

The Aspire dashboard opens in your browser. It starts PostgreSQL, applies migrations (`migrations` resource), then starts `api`, `worker` and the `web` client. Open the `web` endpoint from the dashboard. Vite proxies `/api` and `/connect` to the Api, so the client and Api share one origin.

## Test

```bash
dotnet test --solution Chores.slnx --ignore-exit-code 8   # exit code 8 = a test project has no tests yet

cd src/web
npm run lint && npm run typecheck && npm test
```

## Database migrations

```bash
dotnet ef migrations add <Name> -p src/Chores.Infrastructure -o Persistence/Migrations
```

Migrations are applied by `Chores.MigrationService`, which runs to completion before the Api and Worker start, both locally and in Docker Compose.

The Quartz.NET job store tables live in the `quartz` schema and are created by a migration from the official DDL. When you upgrade the Quartz package, check its DDL for changes and add a migration if needed.

## Publish and deploy (Docker Compose)

```bash
aspire publish -o ./publish   # writes docker-compose.yaml + .env (fill in the image names and password)
aspire deploy  -o ./publish   # builds the images and runs docker compose up on this machine
```

### Production requirements

- **HTTPS is required.** The client is a PWA, and service workers, Web Push and installation on phones only work over HTTPS. Put a TLS-terminating reverse proxy (for example Caddy or Traefik) in front of the stack. It should route:
  - `/api/*` and `/connect/*` to the `api` service
  - everything else to the `web` service
- Keep `/health` and `/alive` reachable only from inside the host or the proxy. They're intended for container health checks.
- On iPhone, the app has to be added to the home screen from Safari (iOS 16.4 or later) to run as an installed app.
