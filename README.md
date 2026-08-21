# DispatchPal

DispatchPal is a learning project that demonstrates a complete asynchronous
dispatch-processing workflow using ASP.NET Core, Angular, PostgreSQL,
RabbitMQ and Docker Compose.

## Architecture

The solution contains the following applications:

- `DispatchPal.Api` — HTTP API, authentication, persistence, outbox publisher
  and processed-event consumer.
- `DispatchPal.Processing` — consumes newly created dispatch requests,
  simulates processing and publishes completed events.
- `DispatchPal.Notification` — consumes completed events and simulates sending
  customer notifications.
- `DispatchPal.Contracts` — shared integration-event contracts.
- `DispatchPal.Web` — Angular frontend.
- `DispatchPal.Api.UnitTests` — controller and application unit tests.
- `DispatchPal.EndToEndTests` — tests the complete running system.

## Message flow

1. Angular sends a request to `DispatchPal.Api`.
2. The API saves the dispatch request and an outbox message in PostgreSQL.
3. The outbox worker publishes `DispatchRequestCreated` to RabbitMQ.
4. `DispatchPal.Processing` processes the request.
5. Processing publishes `DispatchRequestProcessed`.
6. The API updates the request status to `Completed`.
7. `DispatchPal.Notification` handles the completed event.
8. Angular polling retrieves and displays the updated status.

## Main features

- JWT authentication
- Customer and dispatch-request management
- Search and pagination
- Editing requests while they are still pending
- Status history
- PostgreSQL persistence with EF Core migrations
- RabbitMQ topic exchanges and separate queues
- Transactional outbox
- Idempotent message consumption
- Health checks
- Angular route guards and HTTP interceptor
- Unit and end-to-end tests
- Docker Compose orchestration

## Docker Compose

Start the complete system:

```powershell
docker compose up -d --build
```

Check the running services:

```powershell
docker compose ps
```

Stop the system without deleting the persisted PostgreSQL data:

```powershell
docker compose down
```

To delete containers and persisted volumes intentionally:

```powershell
docker compose down --volumes
```

> `docker compose down --volumes` deletes the PostgreSQL data volume. Use it
> only when you intentionally want a clean database.

## Application URLs

- Angular web application: `http://localhost:4200`
- API: `http://localhost:5247`
- API liveness: `http://localhost:5247/health/live`
- API readiness: `http://localhost:5247/health/ready`
- RabbitMQ management: `http://localhost:15672`

## Demo authentication

- Email: `admin@dispatchpal.local`
- Password: `DispatchPal123!`

The demo user, password and JWT signing key are intended only for local
development and learning. They must be replaced by proper secret management
and a real user store in a production system.

## Running locally without application containers

PostgreSQL and RabbitMQ can run through Docker Compose while the .NET and
Angular applications run directly on the host.

Start the infrastructure:

```powershell
docker compose up -d postgres rabbitmq
```

Run the API:

```powershell
dotnet run --project .\src\DispatchPal.Api\DispatchPal.Api.csproj
```

Run the Processing worker in a separate terminal:

```powershell
dotnet run --project .\src\DispatchPal.Processing\DispatchPal.Processing.csproj
```

Run the Notification worker in a separate terminal:

```powershell
dotnet run --project .\src\DispatchPal.Notification\DispatchPal.Notification.csproj
```

Run Angular from its project directory:

```powershell
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
worker to be running. The test API address defaults to
`http://localhost:5247` and can be overridden with `DISPATCHPAL_API_URL`.

Example:

```powershell
$env:DISPATCHPAL_API_URL = "http://localhost:5247"
dotnet test .\tests\DispatchPal.EndToEndTests\DispatchPal.EndToEndTests.csproj
```

## Rebuilding individual Docker services

When application code changes, rebuild only the affected service:

```powershell
docker compose up -d --build api
docker compose up -d --build processing
docker compose up -d --build notification
docker compose up -d --build web
```

Changes to Compose environment variables normally require container
recreation but not an image rebuild:

```powershell
docker compose up -d api
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

## Learning scope

DispatchPal intentionally uses a single configured demo user. Production
authentication would require a user database, password hashing, refresh-token
rotation, token revocation, role or policy authorization and secure secret
storage.
