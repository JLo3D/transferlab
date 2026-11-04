# TransferLab Compose stack

The Compose stack is the deployment rehearsal for Kubernetes. It runs the same
four application images that will be used by the cluster:

```text
sqlserver (healthy)
    |
    v
migrations (exit 0)
    |              \
    v               v
api (ready)       worker
    |
    v
web
```

## Start

Create `.env` from the repository template and set a local SQL Server password:

```bash
cp .env.example .env
```

Build and start the stack:

```bash
docker compose --env-file .env -f docker/docker-compose.yml up -d --build
```

Open `http://localhost:8088`.

## Inspect

```bash
docker compose --env-file .env -f docker/docker-compose.yml ps --all
docker compose --env-file .env -f docker/docker-compose.yml logs -f
```

The expected steady state is:

- SQL Server: running and healthy
- Migrations: exited successfully with code 0
- API: running and healthy
- Worker: running
- Web: running and healthy

## Stop

Preserve the SQL volume:

```bash
docker compose --env-file .env -f docker/docker-compose.yml down
```

Delete the SQL volume as well:

```bash
docker compose --env-file .env -f docker/docker-compose.yml down -v
```

The `-v` form permanently deletes local database contents.
