# Service blueprint — copy-paste scaffolding

**Read this instead of opening every service's `.csproj`, `.sln`, `Dockerfile`, and
`Program.cs` to figure out the pattern.** All four backend services are structurally
**identical** boilerplate; only the names and the entity/business logic differ. The
templates below are verbatim from the existing services — fill in `<Service>` /
`<Entity>` and you have a working skeleton without reading any other file.

> Naming caveat: **AuthService** uses lowercase `artspace.*` namespaces and
> `artspace.sln`. The other three (Art, Comment, Notification, Request) use
> `<Service>Service.*`. Match whichever service you're editing.

---

## 1. Ports & wiring map (the single source of truth)

| Service              | Host port | Container | DB name          | Ocelot prefix        | RabbitMQ? |
|----------------------|-----------|-----------|------------------|----------------------|-----------|
| API Gateway (Ocelot) | 5092      | 8080      | —                | (router only)        | no        |
| AuthService          | 5144      | 8080      | `authdb`         | `/api/Auth/*`        | no        |
| ArtService           | 5280      | 8080      | `artdb`          | `/api/Artwork/*`     | no        |
| CommentService       | 5285      | 8080      | `commentdb`      | `/api/Comment/*`     | publisher |
| NotificationService  | 5012      | 8080      | `notificationdb` | `/api/Notification/*`| consumer  |
| Frontend (nginx)     | 3000      | 80        | —                | —                    | no        |

Every container listens on **8080**; the host port is only the Docker port mapping.
JWT config (`Jwt:Secret` / `Issuer` / `Audience`) is **identical** across all
services — see [testing-blueprint.md](testing-blueprint.md) and CLAUDE.md "Cross-cutting auth".

---

## 2. Adding a NEW service — wiring checklist

Scaffolding the four projects is only half the job. A new service is invisible until
it's wired into **six** places. (As of this writing `RequestService` exists on disk but
was **never wired** into items 3–6 below — a concrete example of how easy these are to
miss.) Do all of them:

1. **`<Service>/`** — four projects + `.sln` (see §3) and `Dockerfile` (§5).
2. **Pick a host port** and add a row to the table in §1.
3. **`ApiGateway/ApiGateway/ocelot.json`** — add a route (Host = `<service>-service`,
   Port = `8080`). See §6.
4. **`ApiGateway/ApiGateway/ocelot.Development.json`** — add the same route but
   Host = `localhost`, Port = the host port from §1.
5. **`docker-compose.yml`** — add a service block (§7) and, if it consumes/publishes
   events, add `rabbitmq` to `depends_on`. Also add the new DB to
   `POSTGRES_MULTIPLE_DATABASES` on the `postgres` service.
6. **`init-db.sh`** — add `CREATE DATABASE <name>db;`.
7. **`run-tests.sh`** — append `"<Service>/<Service>.sln"` to the `SOLUTIONS` array.

---

## 3. The four-project split (Clean Architecture)

```
<Service>/
  <Service>.sln                      # references all 4 projects (see §4)
  Dockerfile
  <Service>.API/                     # controllers, DTOs, Program.cs (composition root)
    <Service>.API.csproj
    appsettings.json
    Program.cs
    Controllers/<Entity>Controller.cs
    DTOs/<Entity>DTOs.cs
  <Service>.Core/                    # entities + repo interfaces. NO dependencies.
    <Service>.Core.csproj
    Entities/<Entity>.cs
    Interfaces/I<Entity>Repository.cs
  <Service>.Infrastructure/          # DbContext, repo impls, Migrations/
    <Service>.Infrastructure.csproj
    Data/<Service>Context.cs
    Services/<Entity>Repository.cs
    Migrations/
  <Service>.Tests/                   # see testing-blueprint.md
```

### `.csproj` templates

**`<Service>.API.csproj`** (`Microsoft.NET.Sdk.Web`). Drop `RabbitMQ.Client` if the
service neither publishes nor consumes events:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.23" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\<Service>.Core\<Service>.Core.csproj" />
    <ProjectReference Include="..\<Service>.Infrastructure\<Service>.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**`<Service>.Core.csproj`** — no packages, no references:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**`<Service>.Infrastructure.csproj`** — EF Core + Npgsql + JWT, references Core:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\<Service>.Core\<Service>.Core.csproj" />
  </ItemGroup>
</Project>
```

For the `<Service>.Tests.csproj` template, see [testing-blueprint.md](testing-blueprint.md).

---

## 4. `.sln`

The `.sln` lists all four projects (API, Core, Infrastructure, Tests). The GUIDs are
arbitrary — **don't hand-edit the GUID blocks**; instead generate the file:

```bash
cd <Service>
dotnet new sln -n <Service>
dotnet sln add <Service>.API <Service>.Core <Service>.Infrastructure <Service>.Tests
```

(`run-tests.sh` and `dotnet test` operate on the `.sln`, so the Tests project must be
added to it.)

---

## 5. `Dockerfile` (verbatim, per service — just swap the name)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["<Service>.API/<Service>.API.csproj", "<Service>.API/"]
COPY ["<Service>.Core/<Service>.Core.csproj", "<Service>.Core/"]
COPY ["<Service>.Infrastructure/<Service>.Infrastructure.csproj", "<Service>.Infrastructure/"]
RUN dotnet restore "<Service>.API/<Service>.API.csproj"
COPY . .
WORKDIR "/src/<Service>.API"
RUN dotnet build "<Service>.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "<Service>.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "<Service>.API.dll"]
```

## 5b. `appsettings.json` (verbatim — swap the DB name)

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=<name>db;Username=postgres;Password=artspace123"
  },
  "Jwt": {
    "Secret": "artspace_super_secret_key_123456789_must_be_long",
    "Issuer": "artspace",
    "Audience": "artspace"
  }
}
```

Docker Compose overrides `ConnectionStrings__DefaultConnection` (→ `Host=postgres`) and
the `Jwt__*` values via env vars; the `localhost` connection string is for running the
service directly.

---

## 6. Ocelot route entry (`ocelot.json`)

```json
{
  "DownstreamPathTemplate": "/api/<Prefix>/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [ { "Host": "<service>-service", "Port": 8080 } ],
  "UpstreamPathTemplate": "/api/<Prefix>/{everything}",
  "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
}
```

`<Prefix>` is the controller name (e.g. `Artwork`, not the service name). In
`ocelot.Development.json` use `"Host": "localhost"` and the host port from §1.

---

## 7. `docker-compose.yml` service block

```yaml
  <service>-service:
    build: ./<Service>
    restart: on-failure
    ports:
      - "<hostPort>:8080"
    depends_on:
      - postgres          # add `- rabbitmq` if it publishes/consumes events
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=<name>db;Username=postgres;Password=artspace123
      - Jwt__Secret=artspace_super_secret_key_123456789_must_be_long
      - Jwt__Issuer=artspace
      - Jwt__Audience=artspace
```

Also add `<name>db` to the `postgres` service's `POSTGRES_MULTIPLE_DATABASES` and to
`init-db.sh`. Add the service to `api-gateway`'s `depends_on`.

---

## 8. `Program.cs` composition root (verbatim pattern)

This is identical across services except for the namespaces, the `DbContext` type, and
the `AddScoped` repository registrations. The two non-obvious bits — both required for
the test harness — are the `Testing`-environment migration skip and
`public partial class Program;`.

```csharp
using <Service>.Core.Interfaces;
using <Service>.Infrastructure.Data;
using <Service>.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<<Service>Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<I<Entity>Repository, <Entity>Repository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Skipped under "Testing" so integration tests boot on in-memory SQLite (see testing-blueprint.md).
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<<Service>Context>();
    db.Database.Migrate();
}

app.Run();

// Exposed so WebApplicationFactory<Program> can host the app in integration tests.
public partial class Program;
```

---

## 9. Vertical slice (entity → interface → DbContext → repo → DTOs → controller)

The canonical CRUD shape, distilled from `ArtService`. Identity comes from JWT claims
(`ClaimTypes.NameIdentifier` = user id, `ClaimTypes.Name` = username); ownership is
enforced by comparing the entity's owner id to the caller's claim and returning
`Forbid()`.

**Entity** (`Core/Entities/<Entity>.cs`) — POCO, `Guid Id`, string props default to
`string.Empty`, timestamps default to `DateTime.UtcNow`.

**Interface** (`Core/Interfaces/I<Entity>Repository.cs`) — async CRUD:
`GetByIdAsync`, `GetAllAsync`, query methods, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.

**DbContext** (`Infrastructure/Data/<Service>Context.cs`):

```csharp
public class <Service>Context : DbContext
{
    public <Service>Context(DbContextOptions<<Service>Context> options) : base(options) { }
    public DbSet<<Entity>> <Entities> { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<<Entity>>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title).IsRequired();
        });
    }
}
```

**Repository** (`Infrastructure/Services/<Entity>Repository.cs`) — constructor-injected
context, `FindAsync`/`Add`/`Update`/`Remove` + `SaveChangesAsync`.

**Controller** (`API/Controllers/<Entity>Controller.cs`) —
`[ApiController]`, `[Route("api/[controller]")]`, constructor-injected repo, `[Authorize]`
on mutations, reads identity via
`Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)`, maps entity→DTO with a
private `MapToDto`.

**DTOs** (`API/DTOs/<Entity>DTOs.cs`) — `Create*Dto` (required fields, non-null),
`Update*Dto` (all nullable for partial update), `*ResponseDto` (full projection).

For the full source of any of these, the freshest complete example is **ArtService**
(`ArtService/ArtService.API/Controllers/ArtworkController.cs` and siblings).

---

## 10. Migrations

Auto-applied at startup via `db.Database.Migrate()` (skipped under `Testing`). To add one:

```bash
cd <Service>
dotnet ef migrations add <Name> \
  --project <Service>.Infrastructure --startup-project <Service>.API
```

Needs `DOTNET_ROOT` set — see [environment.md](environment.md).

---

## 11. Async events (only CommentService ↔ NotificationService today)

Publisher (CommentService) sends an anonymous object to the durable `comment_created`
queue; consumer (NotificationService) deserializes into `CommentCreatedEvent`. The event
shape `{ ArtistId, Username, Content }` is **duplicated, not shared** — keep both sides in
sync by hand. The RabbitMQ hostname `rabbitmq` is hardcoded in the publisher/consumer, so
those classes only work inside Compose. If a new service needs events, follow
`CommentService.Infrastructure/Services/RabbitMQPublisher.cs` (publish) or
`NotificationService.Infrastructure/Services/RabbitMQConsumer.cs` (consume; logic is
extracted into `HandleMessageAsync` so it can be unit-tested).
