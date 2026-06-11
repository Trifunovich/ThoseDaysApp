---
name: project-startup
description: Start, restart, build, and drive the ThoseDaysApp app locally (ASP.NET Core backend + React/Vite frontend + Postgres). Use when asked to run, start, restart, build, or screenshot the app, or verify a change in the real app.
---

# ThoseDaysApp startup steps

Full-stack app: ASP.NET Core (net10.0) backend, React 18 + Vite 5 frontend,
PostgreSQL via Docker. Three pieces must be up: Postgres, backend, frontend.

## 1. Postgres (first — backend dies without it)

```powershell
docker compose -f J:\sources\ThoseDaysApp\docker-compose.yml up -d
docker exec -e PGPASSWORD=postgres those-days-postgres pg_isready -U postgres
```

Container `those-days-postgres`, db `thosedays`, user/pass `postgres`/`postgres`,
port 5432. Connection details also live in `.env` and `appsettings.json`.

## 2. Backend → http://localhost:5200

```powershell
cd J:\sources\ThoseDaysApp\backend\Api
dotnet run
```

- HTTP profile is **:5200** (see `Properties/launchSettings.json`); root returns
  404, that's expected — hit a real route.
- Serilog writes a daily compact-JSON log to `backend/Api/logs/log-<date>.json`.
- **Rebuild gotcha:** `dotnet build`/`run` fails with a locked `Api.exe` if the
  app is already running. Stop it first:
  `Get-Process -Name Api -ErrorAction SilentlyContinue | Stop-Process -Force`
- **Schema changes:** apply migrations with
  `dotnet ef database update --project backend/Api`.

## 3. Frontend → http://localhost:3000

```powershell
cd J:\sources\ThoseDaysApp\frontend
npm run dev
```

Vite is hard-coded to **:3000** and proxies `/api` → `http://localhost:5200`
(see `vite.config.ts`). HMR picks up source edits; no restart needed.

### Driving it with the Claude Preview tool

`.claude/launch.json` is configured with `autoPort: false` and
`runtimeArgs: ["--prefix","frontend","run","dev"]` because the app needs :3000.
If :3000 is held by a manually-started Vite, free it first:
`Get-NetTCPConnection -LocalPort 3000 -State Listen | %{ Stop-Process -Id $_.OwningProcess -Force }`

React inputs are controlled — `preview_fill` sets the DOM value but doesn't fire
React's onChange. To log in via the preview, set values with a native-setter +
`input` event, or just `preview_click` real elements (clicks do dispatch).

## Smoke checks

```powershell
# backend up (returns the RecalcConfig JSON)
Invoke-WebRequest http://localhost:5200/api/config -UseBasicParsing
# frontend up (200)
Invoke-WebRequest http://localhost:3000 -UseBasicParsing
```

## Dev helpers

- **Reset a password** (no email infra): `POST /api/auth/reset-password`
  with `{ "email": "...", "newPassword": "..." }` (min 8 chars).
- **Inspect data:**
  `docker exec -e PGPASSWORD=postgres those-days-postgres psql -U postgres -d thosedays -c 'SELECT * FROM "Cycles";'`
- Don't run destructive recalc/test writes against a real user account — the
  recalculate endpoint **replaces** that user's cycles.
