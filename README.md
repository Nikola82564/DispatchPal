# DispatchPal

DispatchPal is a learning project that demonstrates a complete asynchronous
dispatch-processing workflow using ASP.NET Core, Angular, PostgreSQL,
RabbitMQ, Docker Compose and .NET Aspire.

## Architecture

The solution contains the following applications:

- `DispatchPal.Api` - HTTP API, authentication, persistence, outbox publisher
  and processed-event consumer.
- `DispatchPal.Processing` - consumes newly created dispatch requests,
  simulates processing and publishes completed events.
- `DispatchPal.Notification` - consumes completed events and simulates sending
  customer notifications.
- `DispatchPal.Contracts` - shared integration-event contracts.
- `DispatchPal.Web` - Angular frontend.
- `DispatchPal.AppHost` - .NET Aspire development orchestrator.
- `DispatchPal.Api.UnitTests` - controller and application unit tests.
- `DispatchPal.EndToEndTests` - tests the complete running system.

## Message flow

1. Angular sends a request to `DispatchPal.Api`.
2. The API saves the dispatch request and an outbox message in PostgreSQL.
3. The outbox worker publishes `DispatchRequestCreated` to RabbitMQ.
4. `DispatchPal.Processing` consumes and processes the request.
5. Processing publishes `DispatchRequestProcessed`.
6. The API consumes that event and updates the request status to `Completed`.
7. `DispatchPal.Notification` handles the completed event.
8. Angular polling retrieves and displays the updated status and status history.

## Main features

- JWT authentication
- Customer and dispatch-request management
- Search and pagination
- Editing requests while they are still pending
- Status history
- PostgreSQL persistence with EF Core migrations
- RabbitMQ topic exchanges and separate consumer queues
- Transactional outbox
- Idempotent message consumption
- Health checks
- Angular route guards and HTTP interceptor
- Unit and end-to-end tests
- Docker Compose orchestration
- .NET Aspire local orchestration and dashboard

## Local development options

The complete application can be started in two ways:

1. Docker Compose runs every application and infrastructure service in
   containers.
2. .NET Aspire runs PostgreSQL and RabbitMQ in containers while the .NET
   projects and Angular development server run as local processes.

Do not run both complete environments at the same time because they can
compete for ports such as Angular port `4200`.

Docker Compose and Aspire also use separate PostgreSQL and RabbitMQ volumes.
Data created in one environment is therefore not automatically visible in
the other environment.

## Running with Docker Compose

Create a local `.env` file from the provided example:

```powershell
Copy-Item .env.example .env
```

Fill in the local development values inside `.env`. The real `.env` file is
ignored by Git and must never be committed.

Start the complete system:

```powershell
docker compose up -d --build
```

Check the running services:

```powershell
docker compose ps
```

Stop and remove the application containers without deleting persisted
PostgreSQL and RabbitMQ data:

```powershell
docker compose down
```

To intentionally delete containers and persisted volumes:

```powershell
docker compose down --volumes
```

> `docker compose down --volumes` deletes the PostgreSQL and RabbitMQ data
> volumes. Use it only when you intentionally want a clean environment.

## Running with .NET Aspire

.NET Aspire provides a development dashboard and starts the complete local
system from `DispatchPal.AppHost`.

In the current Aspire setup:

- PostgreSQL runs in a Docker container.
- RabbitMQ runs in a Docker container.
- `DispatchPal.Api` runs as a local .NET process.
- `DispatchPal.Processing` runs as a local .NET process.
- `DispatchPal.Notification` runs as a local .NET process.
- Angular runs as a local `npm start` process.
- `web-installer` is a one-time helper resource that runs `npm install`.

The `web-installer` resource is expected to reach the `Finished` state.
It installs the frontend dependencies and then exits. The `web` resource is
the long-running Angular development server and should remain in the
`Running` state.

### Aspire prerequisites

The following tools are required:

- .NET SDK
- Docker Desktop
- Node.js and npm
- .NET Aspire project templates

Install the Aspire templates if they are not already installed:

```powershell
dotnet new install Aspire.ProjectTemplates
```

### Configure the Aspire RabbitMQ password

The RabbitMQ password is stored with .NET user secrets outside the repository.
Configure it before starting the AppHost:

```powershell
dotnet user-secrets set `
  "Parameters:rabbitmq-password" `
  "CHOOSE-YOUR-LOCAL-RABBITMQ-PASSWORD" `
  --project .\src\DispatchPal.AppHost\DispatchPal.AppHost.csproj
```

This value is intended only for local development. It is stored in the local
user-secrets store and is not committed to Git.

The Aspire RabbitMQ username is configured as `dispatchpal` by the AppHost.

### Start the Aspire environment

First stop the Docker Compose application environment if it is running:

```powershell
docker compose down
```

Start the Aspire AppHost:

```powershell
dotnet run `
  --project .\src\DispatchPal.AppHost\DispatchPal.AppHost.csproj
```

The terminal prints the Aspire dashboard URL. Open that URL in the browser.

The dashboard displays these resources:

- `postgres` - PostgreSQL container
- `dispatchpal-database` - logical database inside PostgreSQL
- `rabbitmq` - RabbitMQ container and management interface
- `api` - DispatchPal HTTP API
- `processing` - message-processing worker
- `notification` - notification worker
- `web-installer` - one-time npm dependency installer
- `web` - Angular development server

The exact infrastructure ports can be dynamically assigned by Aspire. Use
the URLs displayed in the Aspire dashboard instead of assuming the Docker
Compose ports.

The Angular application uses port `4200` in the current local development
configuration.

Stop the Aspire environment by pressing `Ctrl+C` in the terminal running the
AppHost. Stopping Aspire does not intentionally delete the persisted Aspire
database and RabbitMQ volumes.

## Docker Compose application URLs

When the complete system is running through Docker Compose:

- Angular web application: `http://localhost:4200`
- API: `http://localhost:5247`
- API liveness: `http://localhost:5247/health/live`
- API readiness: `http://localhost:5247/health/ready`
- RabbitMQ management: `http://localhost:15672`

When running with Aspire, use the dynamically displayed resource URLs in the
Aspire dashboard.

## Demo authentication

- Email: `admin@dispatchpal.local`
- Password: `DispatchPal123!`

The demo user and password are intended only for local development and
learning. Production authentication would use a user store, password hashing
and secure secret management.

JWT signing keys and infrastructure passwords must be supplied through local
secrets or environment variables. Real credentials must never be committed
to Git.

## Running locally without full orchestration

PostgreSQL and RabbitMQ can run through Docker Compose while the .NET and
Angular applications run directly on the host.

Start the infrastructure:

```powershell
docker compose up -d postgres rabbitmq
```

Run the API:

```powershell
dotnet run `
  --project .\src\DispatchPal.Api\DispatchPal.Api.csproj
```

Run the Processing worker in a separate terminal:

```powershell
dotnet run `
  --project .\src\DispatchPal.Processing\DispatchPal.Processing.csproj
```

Run the Notification worker in a separate terminal:

```powershell
dotnet run `
  --project .\src\DispatchPal.Notification\DispatchPal.Notification.csproj
```

Run Angular from its project directory:

```powershell
Set-Location .\src\DispatchPal.Web
npm start
```

## Database migrations

Create a migration:

```powershell
dotnet ef migrations add MigrationName `
  --project .\src\DispatchPal.Api\DispatchPal.Api.csproj `
  --startup-project .\src\DispatchPal.Api\DispatchPal.Api.csproj
```

Apply pending migrations:

```powershell
dotnet ef database update `
  --project .\src\DispatchPal.Api\DispatchPal.Api.csproj `
  --startup-project .\src\DispatchPal.Api\DispatchPal.Api.csproj
```

The API also applies pending migrations during startup.

## Tests

Build the solution:

```powershell
dotnet build "DispatchPal Demo.slnx"
```

Run all tests:

```powershell
dotnet test "DispatchPal Demo.slnx" --no-build
```

The end-to-end tests require the API, PostgreSQL, RabbitMQ and Processing
worker to be running.

The test API address defaults to `http://localhost:5247` and can be overridden
with `DISPATCHPAL_API_URL`.

Example:

```powershell
$env:DISPATCHPAL_API_URL = "http://localhost:5247"

dotnet test `
  .\tests\DispatchPal.EndToEndTests\DispatchPal.EndToEndTests.csproj
```

When the tests are finished, remove the environment variable from the current
PowerShell session if it is no longer required:

```powershell
Remove-Item Env:DISPATCHPAL_API_URL
```

## Rebuilding individual Docker services

When application source code changes, rebuild only the affected image:

```powershell
docker compose up -d --build api
docker compose up -d --build processing
docker compose up -d --build notification
docker compose up -d --build web
```

When running the applications directly or through Aspire, Docker image rebuilds
are not required for changes to local application source code.

Changes to Docker Compose environment variables normally require container
recreation but not an image rebuild:

```powershell
docker compose up -d --force-recreate api
```

## Useful diagnostics

Show recent service logs:

```powershell
docker compose logs --tail 100 api
docker compose logs --tail 100 processing
docker compose logs --tail 100 notification
```

Inspect RabbitMQ queues:

```powershell
docker compose exec rabbitmq `
  rabbitmqctl list_queues `
  name messages_ready messages_unacknowledged consumers
```

When using Aspire, logs for each application and infrastructure resource are
available directly in the Aspire dashboard.

## Security and configuration

The repository contains example configuration suitable for local learning.

The following values must never be committed:

- production database passwords
- production RabbitMQ credentials
- production JWT signing keys
- Azure credentials
- GitHub deployment credentials
- complete production connection strings

Local Docker Compose secrets belong in the ignored `.env` file.

Local Aspire secrets belong in the .NET user-secrets store.

Production secrets should be provided through the deployment platform's
secret-management features.

## Azure deployment

The learning deployment runs in the `rg-dispatchpal-dev` resource group in
Azure Poland Central.

Public endpoints:

- Web: `https://web.agreeableground-f565eeb7.polandcentral.azurecontainerapps.io`
- API: `https://api.agreeableground-f565eeb7.polandcentral.azurecontainerapps.io`

Azure resources:

- Azure Container Registry: `dispatchpal96884.azurecr.io`
- Container Apps environment: `cae-dispatchpal-dev`
- PostgreSQL Flexible Server: `dispatchpal-pg-46308`
- Container Apps: `web`, `api`, `processing`, `notification` and `rabbitmq`
- User-assigned managed identity: `id-dispatchpal-acr-pull`

The API and web applications have public HTTPS ingress. Processing and
Notification are background applications without ingress. RabbitMQ exposes
only internal TCP ingress on port 5672, so it can be reached only by
applications in the same Container Apps environment.

The managed identity has only the `AcrPull` role and is used by the
application containers to download private images from ACR. Database,
RabbitMQ and JWT credentials are stored as Container Apps secrets.

The Azure PostgreSQL server uses the smallest Burstable learning SKU used by
this deployment, `Standard_B1ms`, with 32 GB of storage. The `citext`
extension must be included in the server's `azure.extensions` allow-list
before the API can apply its EF Core migration:

```powershell
az postgres flexible-server parameter set `
  --resource-group rg-dispatchpal-dev `
  --server-name dispatchpal-pg-46308 `
  --name azure.extensions `
  --value citext
```

The web image uses the official Nginx image's startup template processing.
`API_UPSTREAM` defaults to `http://api:8080` for Docker Compose. In Azure it is
set to `http://api`, which uses Container Apps service discovery. The proxy
preserves forwarded request information, and the API enables ASP.NET Core
forwarded-header processing through
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.

This is a low-cost learning deployment, not a production topology. RabbitMQ
uses ephemeral container storage, so broker data can be lost when its replica
is replaced. The PostgreSQL server, ACR storage and continuously running
container replicas can consume Azure credit even when the application is not
being actively tested.

To stop all charges after the demo, delete the complete resource group:

```powershell
az group delete `
  --name rg-dispatchpal-dev `
  --yes `
  --no-wait
```

This command permanently deletes the cloud database, Container Apps,
registry and all other resources in the group. It does not delete the local
repository or GitHub repository.

## Learning scope and production trade-offs

DispatchPal intentionally uses a single configured demo user. A production
authentication system would require:

- a persistent user database
- password hashing
- refresh-token rotation
- token revocation
- role-based or policy-based authorization
- secure secret storage
- account recovery and lockout protection

The project uses RabbitMQ to demonstrate asynchronous communication.
Production deployment would additionally require decisions about managed
messaging, broker availability, backups, retry policies, dead-letter queues,
monitoring and operational ownership.
