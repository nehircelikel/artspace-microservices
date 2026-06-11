# Local environment & toolchain (don't re-probe this)

These are the facts a fresh session would otherwise rediscover by running
`which dotnet`, `dotnet --version`, `ls ~/.dotnet`, `which docker`, `node --version`.
They're stable on this machine — read them here instead of shelling out.

| Tool   | Location / version                          | Notes |
|--------|---------------------------------------------|-------|
| dotnet | `~/.dotnet/dotnet`, SDK **8.0.422**         | **Non-default location.** `DOTNET_ROOT` must be set (see below). |
| docker | `/usr/local/bin/docker`, Engine **24.2.0** | `docker compose` (v2 subcommand), not `docker-compose`. |
| node   | **v24.2.0**                                 | for the `frontend/` Vite app. |
| npm    | **11.3.0**                                  | |

## DOTNET_ROOT is required

The SDK is **not** in a default location, so `dotnet test`, `dotnet ef`, and the test
host can't resolve the runtime unless `DOTNET_ROOT` is set. `run-tests.sh` already does
this. If you invoke `dotnet` directly:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"
```

## Two ways to run the stack

- **Full stack** → `docker compose up --build`. This is the only mode where RabbitMQ
  works (hostname `rabbitmq` is hardcoded in the publisher/consumer) and where the
  Postgres `Host=postgres` overrides apply.
- **Single service / tests** → run from the service folder. `appsettings.json` points at
  `Host=localhost`, so a local Postgres on `5432` (password `artspace123`) is needed for
  real runs — **but tests need nothing**: they use in-memory SQLite (see
  [testing-blueprint.md](testing-blueprint.md)).

## Quick verify commands

```bash
./run-tests.sh        # all backend tests, in-memory, no Docker/Postgres/RabbitMQ
docker compose up --build
cd frontend && npm run dev   # Vite dev server (reads VITE_API_URL, defaults to gateway :5092)
```
