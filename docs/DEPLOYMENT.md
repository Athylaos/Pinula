# Infrastructure, Environments & CI/CD

Pinula runs as a containerized multi-service application hosted on Proxmox VE Linux Containers (LXC), exposed to the public internet through a Cloudflare Tunnel, and verified on every commit by GitHub Actions.

---

## 1. Environment Topology & Network Architecture

The infrastructure separates local development, pre-production staging, and live production across three runtime environments:



| Environment | Host / Endpoint                                                       | Runtime Execution                                                             | Configuration & Database                                                                                                                                                     |
| --- |-----------------------------------------------------------------------|-------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Local Development** | `localhost` (`10.0.10.101:5432` DB)                                   | Developer runs `Pinula.WASM` and `Pinula.API` natively via IDE or `dotnet run` | Configured via `appsettings.Development.json` (templated from `appsettings.example.Development.json`); connects directly to the staging PostgreSQL instance (`Pinula_Stage`) |
| **Staging** | `stage.pinula.app`, `stage-api.pinula.app` (`CT 101` / `10.0.10.101`) | Docker Compose stack inside unprivileged Debian LXC container                 | `ASPNETCORE_ENVIRONMENT=Staging`; validates EF Core migrations, API contracts, and WASM bundles prior to production release                                                  |
| **Production** | `pinula.app`, `api.pinula.app` (`CT 102` / `10.0.10.102`)             | Isolated Docker Compose stack inside mirrored Debian LXC container            | `ASPNETCORE_ENVIRONMENT=Production`; serves live user traffic with isolated database volume and media storage                                                                |

### Engineering Trade-Off: Shared Staging Database in Local Development

* **Advantage:** Running `Pinula.API` locally against the staging PostgreSQL container eliminates local database setup overhead and lets developers test queries against populated recipe, ingredient, and inventory datasets.
* **Limitation:** Schema-altering EF Core migrations (`Pinula.API/Migrations/`) must be tested carefully because destructive changes immediately affect the shared `Pinula_Stage` database.



---

## 2. Containerization & Docker Compose Stack

Each application LXC container organizes persistent host storage and source code under `/home/pinula/`:

* `/home/pinula/app/` holds the cloned Git repository, `docker-compose.yml`, and Docker build contexts.


* `/home/pinula/images/` stores uploaded user avatars, category icons, ingredient images, and recipe photos outside container lifecycles.



### Service Definitions

1. **`pinula-db` (`postgres:16-alpine`):**
* Runs PostgreSQL 16 and persists relational tables and `jsonb` columns to a named Docker volume (`postgres_stage_data` / `postgres_prod_data`) mounted at `/var/lib/postgresql/data`.


* Publishes port `5432:5432` on the internal network so local developer API instances can connect directly to the staging database.


2. **`pinula-api` (`Pinula.API/Dockerfile`):**
* Compiles `Pinula.API.csproj` in `Release` configuration and runs on the .NET 10 ASP.NET Core Linux runtime image.


* Bundles `SkiaSharp.NativeAssets.Linux.NoDependencies` so server-side WebP resizing and encoding (`IImageProcessingService`) execute without OS-level graphics dependencies.


* Mounts `/home/pinula/images:/app/wwwroot/images` so media assets persist across container rebuilds, listens on port `5000` (`ASPNETCORE_HTTP_PORTS=5000`).




3. **`pinula-web` (`Pinula.WASM/Dockerfile` & `Pinula.WASM/nginx.conf`):**
* Publishes the static Blazor WebAssembly bundle and serves it via Nginx on container port `80` (mapped to host port `8080`).


* `Pinula.WASM/nginx.conf` configures fallback routing to `index.html` so direct browser navigation to client-side routes resolves cleanly without HTTP 404 responses.





### Secrets & Environment Configuration

Sensitive credentials are excluded from source control (`appsettings.example.json` and `appsettings.example.Development.json` serve as templates) and injected via `docker-compose.yml` environment variables:

* **Database & Auth:** `ConnectionStrings__DefaultConnection`, `JWTKey__Default`
* **External Integrations:** `Resend__ApiKey`, `Resend__FromEmail` (transactional verification emails), and `DeepL__ApiKey` (automated localization)
* **Hardcoded Origin & Endpoint Routing (`Program.cs`):** Allowed CORS origins (`[https://stage.pinula.app](https://stage.pinula.app)`, `[https://pinula.app](https://pinula.app)`, `http://localhost:5119`) are defined directly in `Pinula.API/Program.cs` rather than injected via environment variables—avoiding ASP.NET Core array-binding issues (`AllowedCORS__0`) and trailing-slash origin mismatches. Similarly, `Pinula.WASM/Program.cs` selects `[https://stage-api.pinula.app](https://stage-api.pinula.app)` or `[https://api.pinula.app](https://api.pinula.app)` at runtime based on `builder.HostEnvironment.BaseAddress` because static Nginx-hosted WASM bundles cannot read container environment variables directly.


---

## 3. Deployment & Update Workflow

Deployments use a pull-based Git workflow over SSH rather than pushing pre-built images to an external container registry. Each LXC container authenticates with GitHub using a configured SSH deploy key.

### Remote Deployment Script (`deploy.sh`)

```bash
#!/bin/bash

ENV=$1

if [ "$ENV" == "stage" ]; then
    SERVER="root@10.0.10.101"
elif [ "$ENV" == "prod" ]; then
    SERVER="root@10.0.10.102"
else
    echo "Usage: ./deploy.sh [stage|prod]"
    exit 1
fi

echo "Starting remote deploy for: $ENV ($SERVER)..."

ssh -t $SERVER "cd /home/pinula/app && git pull origin main && docker compose up -d --build"

echo "Deploy for $ENV successfull!"

```

1. The operator invokes `./deploy.sh stage` (`root@10.0.10.101`) or `./deploy.sh prod` (`root@10.0.10.102`).


2. The script opens an SSH session to the target LXC container and executes:


```bash
cd /home/pinula/app && git pull origin main && docker compose up -d --build

```


3. Docker Compose pulls the latest commits from `main`, rebuilds modified `pinula-api` and `pinula-web` images, and recreates the containers with `restart: always`.



### Engineering Trade-Offs

* **Advantage:** Zero registry hosting cost and minimal operational complexity—updates require only SSH access and standard Docker Compose tooling.


* **Limitation:** Building .NET images directly on the LXC host consumes CPU and memory during deployment and introduces brief container restart downtime. Additionally, new EF Core migrations in `Pinula.API/Migrations/` must be applied against the target database when schema changes are deployed.



---

## 4. Cloudflare Tunnel Ingress

### Encrypted Outbound Tunnel Ingress (`cloudflared`)

A dedicated LXC container (`CT 200: cloudflared-ct`) maintains an encrypted outbound tunnel to Cloudflare's edge network. Cloudflare routes incoming HTTPS traffic for `stage.pinula.app & stage-api.pinula.app` and `pinula.app & api.pinula.app` to the internal LXC IP addresses (`10.0.10.101` and `10.0.10.102`) without exposing inbound ports on the host firewall.

---

## 5. Automated Testing & CI Pipeline (`.github/workflows/test.yml`)

GitHub Actions executes automated build and regression checks on every `push` and `pull_request` targeting the `main` branch:

1. **Repository Checkout & SDK Setup:** Clones the repository and installs the exact .NET SDK version pinned in `global.json` (.NET 10).


2. **Dependency Restore & Release Build:** Runs `dotnet restore` followed by `dotnet build CookRecipesApp.slnx --configuration Release --no-restore` to verify compilation and NuGet vulnerability audits across `Pinula.API`, `Pinula.Shared`, `Pinula.WASM`, and `Pinula.API.Tests`.


3. **Automated xUnit Test Execution:** Runs `dotnet test CookRecipesApp.slnx --configuration Release --no-build`, executing `Pinula.API.Tests` (Currently only implemented for `CategoryBackendServiceTests` as reference implementation, ready for full test coverage) against isolated relational SQLite in-memory databases (`TestDbContextFactory`) without requiring a PostgreSQL service container in CI.

