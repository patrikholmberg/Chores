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

The Aspire dashboard opens in your browser. It starts PostgreSQL, applies migrations (`migrations` resource), then starts `api`, `worker` and the `web` client. Open the `web` endpoint from the dashboard. Vite proxies `/api`, `/connect` and `/.well-known` to the Api, so the client and Api share one origin.

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
  - `/api/*`, `/connect/*` and `/.well-known/*` to the `api` service
  - everything else to the `web` service
- Keep `/health` and `/alive` reachable only from inside the host or the proxy. They're intended for container health checks.
- On iPhone, the app has to be added to the home screen from Safari (iOS 16.4 or later) to run as an installed app.

### Sign-in and CAPTCHA settings

`aspire publish` adds these parameters to `.env`. Fill them in before the first `docker compose up`:

| Parameter | What it is |
|---|---|
| `PUBLIC_URL` | The HTTPS origin people open, e.g. `https://chores.example.com`. It's the token issuer and the OAuth redirect origin. |
| `AUTH_SIGNING_CERTIFICATE`, `AUTH_SIGNING_CERTIFICATE_PASSWORD` | Base64 PFX that signs tokens, and its password. |
| `AUTH_ENCRYPTION_CERTIFICATE`, `AUTH_ENCRYPTION_CERTIFICATE_PASSWORD` | Base64 PFX that encrypts tokens, and its password. |
| `CAPTCHA_HMAC_KEY` | Base64 of 64 random bytes that sign the signup CAPTCHA challenges. |

Generate them once and keep them. Replacing the certificates signs everyone out, and replacing the CAPTCHA key only invalidates challenges that are in flight.

```bash
for kind in signing:digitalSignature encryption:keyEncipherment; do
  name=${kind%%:*}; usage=${kind##*:}
  openssl req -x509 -newkey rsa:2048 -nodes -days 3650 -subj "/CN=Chores $name" \
    -addext "keyUsage=critical,$usage" -keyout $name.key -out $name.crt
  openssl pkcs12 -export -in $name.crt -inkey $name.key -out $name.pfx -passout pass:'<password>'
  base64 -w0 $name.pfx > $name.pfx.b64   # value for AUTH_<NAME>_CERTIFICATE
done
openssl rand -base64 64 | tr -d '\n'     # value for CAPTCHA_HMAC_KEY
```

The reverse proxy must keep the original `Host` header and set `X-Forwarded-For` and `X-Forwarded-Proto` (Caddy and Traefik do by default). The authorization server builds its endpoint URLs from them, and signup rate limiting is per client IP.

Locally none of this is needed: in Development the Api uses OpenIddict's development certificates and a per-process CAPTCHA key, and the AppHost sets the issuer to the Vite origin.

## Operator tasks

### Reset a Member's password

Until Parents can reset their own password by email, an operator with access to the database can set any Member's password. The `migrations` image runs the command, reads the new password from stdin, checks it against the Member's password policy (Parent at least 10 characters, Child at least 4), and ends all of that Member's sessions:

```bash
printf '%s\n' 'the-new-password' | docker compose run --rm -T migrations reset-password alex
```

Locally, against the database Aspire started (copy the connection string from the dashboard):

```bash
printf '%s\n' 'the-new-password' | ConnectionStrings__choresdb='<connection string>' \
  dotnet run --project src/Chores.MigrationService -- reset-password alex
```

The command exits with 0 on success and 1 with a message if the username is unknown or the password is too short.
