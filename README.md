# Dapper template

Provisioned from [`Qode-Fleet-Control/fleet-template-v1`](https://github.com/Qode-Fleet-Control/fleet-template-v1) — the fleet
lifecycle contract (`bin/`, `fleet.conf`, `compose.yaml`, deploy workflows) with a Dapper
console app (.NET 10) laid on top: plain SQL, mapped to C# objects by Dapper.

    src/App/Data/Db.cs               opens the connection: Npgsql or Microsoft.Data.Sqlite
    src/App/Data/Todo.cs             the row type
    src/App/Data/TodoRepository.cs   the SQL: schema, insert … RETURNING, update, query
    src/App/Program.cs               the job: ensure schema, write, read back, exit 0

**Database:** PostgreSQL when `DATABASE_URL` is set (the fleet injects
`postgres://user:pass@host:5432/db`; an Npgsql `Host=…;` string works too), otherwise a SQLite
file at `SQLITE_PATH` (default `data/app.db`). Only the `CREATE TABLE` differs between the two;
every query is shared.

**This repo is not a service.** It serves no HTTP: `START_CMD` and `DOCKER_START_CMD` are
empty, so `bin/run` installs/builds and stops there. Run the job with
`docker compose run --rm app`.

## Origin

Dapper has no project generator. The project is the official console template plus the
packages, generated 2026-10-05 inside the official SDK image (.NET SDK 10.0.401):

    docker run --rm -u $(id -u):$(id -g) -e HOME=/tmp -v "$PWD":/w -w /w \
      mcr.microsoft.com/dotnet/sdk:10.0 bash -c '
        dotnet new console -n App -o src/App --framework net10.0
        cd src/App
        dotnet add package Dapper
        dotnet add package Microsoft.Data.Sqlite
        dotnet add package Npgsql'

`Data/` and `Program.cs` are hand-written, the way Dapper's README teaches it: extension
methods (`ExecuteAsync`, `QueryAsync<T>`, `ExecuteScalarAsync<T>`) on an open `DbConnection`,
with anonymous-object parameters.

## Running it

**On the fleet** — `bin/run` builds the image; run the job with `docker compose run --rm app`.
The fleet passes `DATABASE_URL` through, so it uses the workspace's Postgres.

**With docker, locally**

    docker compose build
    docker compose run --rm app                       # SQLite, inside the container (ephemeral)

    docker compose --profile local up -d db           # a local Postgres (profile "local" only)
    DATABASE_URL=postgres://app:app@db:5432/app docker compose run --rm app
    docker compose --profile local down -v

**Without docker** (needs the .NET 10 SDK on PATH)

    dotnet run --project src/App                      # SQLite at ./data/app.db
    FLEET_RUNTIME=process bin/run                     # = restore + publish to .out
    dotnet .out/App.dll

| step | process runtime | docker runtime |
|---|---|---|
| install | `dotnet restore src/App/App.csproj` | — |
| build | `dotnet publish src/App/App.csproj -c Release --no-restore -o .out` | `docker compose build` |
| run the job | `dotnet .out/App.dll` | `docker compose run --rm app` |

## Deviations from the stock generator output, and why

- `Program.cs` replaced by the job; `Data/` added.
- Project under `src/App/`, not the repo root: .NET writes build output to the project's
  `bin/`/`obj/`, which at the root would collide with the fleet's `bin/` scripts.
- `compose.yaml` adds `SQLITE_PATH` to the pass-through list, and a `db` Postgres service
  under `profiles: ["local"]`, so it never starts on the fleet.
- Added: `Dockerfile`, `compose.yaml`, `.dockerignore`, a compact `.gitignore` (the stock
  `dotnet new gitignore` ignores every `bin/` — including the fleet's — and this one also
  ignores the SQLite file), `.env.example`, `fleet.conf`, `bin/`, `.github/workflows/`,
  `docs/fleet-lifecycle.md`.
- No NuGet lock file: the generator does not create one.

## Verified

**The docker runtime has NOT been verified yet.** On 2026-10-05 the shared docker host's disk
stayed at 0-5G free (under the 6G floor for a build) for over five hours, so `docker compose build`
was never run for this repo. Run the checks below once before trusting the image.

What did pass, inside `mcr.microsoft.com/dotnet/sdk:10.0` (.NET SDK 10.0.401): `dotnet run`
against SQLite twice (schema created, rows written and read back, open count 1 then 2).

Still to run: `docker compose build && docker compose run --rm app` (SQLite), and the Postgres path:
`docker compose --profile local up -d db` then
`DATABASE_URL=postgres://app:app@db:5432/app docker compose run --rm app` (twice).
