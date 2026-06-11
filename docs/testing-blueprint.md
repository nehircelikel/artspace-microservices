# Testing blueprint — copy-paste test harness

**Read this instead of opening `*.Tests/Helpers/*.cs`, `GlobalUsings.cs`, and the test
`.csproj` across services to reverse-engineer the harness.** Every service's test project
uses the same three helpers and the same in-memory SQLite approach; the templates below
are verbatim (from `ArtService.Tests`) — swap `<Service>` / `<Entity>` and you're done.

## How to run

```bash
./run-tests.sh                                  # all services (must be listed in SOLUTIONS array)
cd <Service> && dotnet test <Service>.sln       # one service
```

`DOTNET_ROOT` must point at the SDK (`run-tests.sh` sets `$HOME/.dotnet`). If you call
`dotnet test` yourself, export it first — see [environment.md](environment.md).

## Layout

```
<Service>.Tests/
  <Service>.Tests.csproj
  GlobalUsings.cs                 # global using Xunit;
  Helpers/
    SqliteContextFactory.cs       # unit tests: fresh in-memory DbContext
    <Service>ApiFactory.cs        # integration tests: WebApplicationFactory<Program>
    TestJwt.cs                    # mints test JWTs + supplies test config
  Unit/
    <Entity>RepositoryTests.cs    # repo against SQLite
    <Entity>ControllerTests.cs    # controller with mocked repo (Moq)
  Integration/
    <Entity>EndpointsTests.cs     # full HTTP round-trip via the factory
```

NotificationService has no `Integration/` folder — its `BackgroundService` consumer logic
is unit-tested directly via the extracted `RabbitMQConsumer.HandleMessageAsync`, not over HTTP.

## Two in-memory SQLite strategies (don't confuse them)

- **Unit / repository tests** → `SqliteContextFactory`: each call to `NewContext()`
  returns a `DbContext` on a single shared in-memory connection with the schema created
  via `EnsureCreated()`.
- **Integration tests** → `<Service>ApiFactory : WebApplicationFactory<Program>`: boots
  the real app under the `Testing` environment (which is why `Program.cs` skips
  `db.Database.Migrate()` and exposes `public partial class Program;`), swaps the Npgsql
  `DbContext` for SQLite, and injects `TestJwt.Config` so JWT validation works. For
  CommentService the factory also replaces `IRabbitMQPublisher` with a no-op.

## `<Service>.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\<Service>.API\<Service>.API.csproj" />
    <ProjectReference Include="..\<Service>.Infrastructure\<Service>.Infrastructure.csproj" />
    <ProjectReference Include="..\<Service>.Core\<Service>.Core.csproj" />
  </ItemGroup>
</Project>
```

`GlobalUsings.cs` is a single line: `global using Xunit;`

## `Helpers/TestJwt.cs`

> ⚠️ The **test** JWT secret/issuer/audience are deliberately **different** from
> production (`artspace_super_secret_key_...` / `artspace`). The factory injects these
> test values via `TestJwt.Config`, so tokens minted by `TestJwt.Create` validate inside
> the test host. Claims match production: `NameIdentifier`=user id, `Name`=username,
> `Role` (default `"Artist"`).

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace <Service>.Tests.Helpers;

public static class TestJwt
{
    public const string Secret = "test-secret-key-for-integration-tests-only-1234567890";
    public const string Issuer = "artspace-test";
    public const string Audience = "artspace-test";

    public static readonly Dictionary<string, string?> Config = new()
    {
        ["Jwt:Secret"] = Secret,
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
    };

    public static string Create(Guid userId, string username, string role = "Artist")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## `Helpers/SqliteContextFactory.cs`

```csharp
using <Service>.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace <Service>.Tests.Helpers;

public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public <Service>Context NewContext()
    {
        var options = new DbContextOptionsBuilder<<Service>Context>()
            .UseSqlite(_connection)
            .Options;
        var context = new <Service>Context(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
```

## `Helpers/<Service>ApiFactory.cs`

```csharp
using <Service>.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace <Service>.Tests.Helpers;

public sealed class <Service>ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public <Service>ApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(TestJwt.Config));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<<Service>Context>>();
            services.AddDbContext<<Service>Context>(o => o.UseSqlite(_connection));

            // If this service publishes events, also no-op the publisher here, e.g.:
            //   services.RemoveAll<IRabbitMQPublisher>();
            //   services.AddSingleton<IRabbitMQPublisher, NoOpPublisher>();

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<<Service>Context>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
```

## Test-writing conventions

- **Repository tests**: `using var f = new SqliteContextFactory();` then exercise the
  repo against `f.NewContext()`. Use separate contexts for arrange vs. assert to prove
  persistence.
- **Controller tests**: construct the controller with a `Mock<I<Entity>Repository>`; for
  `[Authorize]`/identity logic set
  `controller.ControllerContext.HttpContext.User` to a `ClaimsPrincipal` with the same
  claim types `TestJwt.Create` uses.
- **Integration tests**: `var client = factory.CreateClient();` then
  `client.DefaultRequestHeaders.Authorization = new("Bearer", TestJwt.Create(id, name));`
  and assert on real HTTP status codes + JSON bodies.
- Prefer adding a test here over throwaway `curl`/scripts when verifying a change.
- After scaffolding a new service's tests, **add its `.sln` to `run-tests.sh`** or CI
  won't run them.
