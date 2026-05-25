# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack

- **Backend**: ASP.NET Core on **.NET 10** (`backend/Api/Api.csproj`), EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Frontend**: React 18 + TypeScript 5 + Vite 5 (`frontend/`). No state library — only React Context.
- **Database**: PostgreSQL 15, run via `docker-compose.yml` (also exposes pgAdmin on `:5050`).
- No solution file — the backend project is referenced directly by path.

## Common commands

Run from repo root unless noted. The `backend/Api` path is used for both the project dir and the `--project` argument to dotnet.

```powershell
# DB (Postgres + pgAdmin)
docker-compose up -d

# Restore / install
dotnet restore .\backend\Api
npm install --prefix .\frontend

# EF Core migrations (Tools package is referenced in Api.csproj)
dotnet ef database update --project backend/Api
dotnet ef migrations add <Name> --project backend/Api

# Dev (run in two terminals)
dotnet watch run --project backend/Api      # API at http://localhost:5200, https://localhost:7241
npm run dev --prefix frontend               # UI at http://localhost:3000, proxies /api -> :5200

# Production build
dotnet publish backend/Api -c Release
npm run build --prefix frontend             # runs `tsc && vite build`
```

No test framework is wired up; do not invent a test command. `plan.txt` mentions `--seed` / `--command` CLI flags but those are **not implemented** in `Program.cs` — ignore them unless asked to add them.

## Configuration

DB settings are read from configuration keys `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` in `Program.cs` (assembled into the Npgsql connection string — there is **no** `ConnectionStrings:Default` entry). Defaults match `docker-compose.yml` (`postgres`/`postgres`/`thosedays` on `localhost:5432`). Root `.env` is consumed by docker-compose for the Postgres container; the API does not auto-load it — pass overrides via environment variables or `appsettings.{Environment}.json`.

## Architecture

### Request flow
1. Browser hits Vite at `:3000`; Vite proxies `/api/*` to the ASP.NET API at `:5200` (`frontend/vite.config.ts`).
2. Controllers in `backend/Api/Controllers/` are routed under `api/auth/...` and `api/user/{userId}/...`. They are **thin** — they delegate to services and project entities into DTOs (`DTOs/`).
3. Services (`Services/CycleService.cs`, `Services/AuthService.cs`) hold business logic and talk to `AppDbContext` directly. Both are registered Scoped in `Program.cs`.
4. EF Core (`Data/AppDbContext.cs`) owns `Users`, `Cycles`, `Predictions`. Cascade delete from `User` to its `Cycles` and `Predictions` is configured in `OnModelCreating`.

There is currently **no auth middleware** — the "token" returned from `AuthController` is a Base64(`userId:ticks`) string and the cycles/predictions controllers accept the `userId` from the route without verifying it. Treat anything that depends on identity as trust-on-route; don't assume `[Authorize]` is in effect.

### Cycle → Period → Prediction model (the non-obvious part)

The DB stores **Cycles** as `(start_date, duration_days)` rows, but the prediction math operates on **Periods**, which are the maximal runs of consecutive days covered by any cycle. `CycleService.GetPeriodsAsync` expands each `Cycle` into individual days, dedupes/sorts them, and groups consecutive days into `(Start, Length)` tuples. This means overlapping or back-to-back `Cycle` rows merge into a single period for stats and prediction purposes.

Derived values (`GetStatsAsync`):
- `averageCycleLength` = mean period **length** (days bleeding).
- `averageInterval` = mean gap between consecutive period **starts**. Falls back to `28` when there is only one period.

`GeneratePredictionsAsync(userId, n)`:
- Deletes all existing `Predictions` for the user, then writes `n` new ones starting at `lastPeriod.Start + round(avgInterval)`, each `round(avgInterval)` days apart with duration `round(avgLength)` (min 1, defaults to 5 if no length history).
- Confidence is hard-coded to `0.85f`.
- Add / Update / Delete in `CycleService` **always** call this with `n=15` afterward, so predictions stay in sync with cycles automatically. `PredictionsController.POST /predict` lets the client force regeneration with a different count.

`CycleService.ToUtc` normalizes incoming `DateTime` to `Kind = Utc` because Npgsql's `timestamp with time zone` mapping rejects unspecified-kind values — preserve this when adding new date inputs.

### Frontend

- `src/App.tsx` is the shell: if `useAuth().user` is null it renders `LoginPage`; otherwise it fetches `/api/user/{id}/cycles` and `/stats` in parallel and renders `StatusBar` + `Calendar`.
- `src/context/AuthContext.tsx` is the only global state — user + token are persisted in `localStorage` under `user` / `token`. The token is currently not attached to API requests (the backend doesn't check it).
- Components (`Calendar`, `StatusBar`, `BloodDropIcon`) live in `src/components/`. Pages in `src/pages/`. Styling is plain CSS in `src/styles/`.
- `Calendar.onCycleAdded` is the refresh hook — after mutating cycles, components call back up to `App` to refetch both endpoints (stats depend on cycles).

## Conventions worth keeping

- Controllers use primary constructors (`Controller(IService service)`) and `Microsoft.AspNetCore.Mvc` attribute routing. Keep them thin; put logic in services.
- DTOs are explicit response/request types in `DTOs/` — do not return EF entities directly from controllers.
- EF model configuration is centralized in `AppDbContext.OnModelCreating`; prefer adding `entity.Property(...)` rules there over data annotations.
- When you change anything affecting cycle storage or interval math, update both `GetPeriodsAsync` and `GeneratePredictionsAsync` together — predictions are eagerly regenerated on every cycle mutation, so a bug in either is immediately user-visible.
