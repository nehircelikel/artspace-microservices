# ArtSpace Copilot Instructions

## Architecture Overview

ArtSpace follows a **three-layer clean architecture** pattern:

- **artspace.API** (.NET 8.0 Web API): Entry point, HTTP endpoints, Swagger documentation via Swashbuckle
- **artspace.Core**: Domain layer with `Entities/` (data models) and `Interfaces/` (repository contracts)
- **artspace.Infrastructure**: Implementation layer with `Data/` (EF Core DbContext) and `Services/` (repository implementations)

**Data Flow**: API endpoints → Core interfaces → Infrastructure repositories → EF Core DbContext → PostgreSQL (Npgsql)

## Key Technologies & Patterns

- **EF Core 8.0** with PostgreSQL (Npgsql) driver
- **Async repository pattern**: All repository methods use `async/await` returning `Task<T>`
- **Dependency Injection**: Via ASP.NET Core DI container; inject `ArtSpaceContext` into repositories
- **Password Security**: BCrypt.Net-Next for hashing (already added to Infrastructure layer)
- **API Docs**: Swagger/OpenAPI auto-generated from endpoint definitions

## Project-Specific Conventions

### Naming & Namespace Structure
- Follow `artspace.{Layer}` namespace convention (e.g., `artspace.Infrastructure.Services`)
- Interface files in `Interfaces/` (e.g., `IUserRepository.cs`); implementations in `Services/` (e.g., `UserRepository.cs`)
- Entity files in `Entities/` (e.g., `User.cs`)

### Entity Design
- All entities use `Guid Id` as primary key
- Include `DateTime CreatedAt = DateTime.UtcNow` for audit trails
- Use nullable strings (`string?`) for optional fields (e.g., Bio, ContactEmail)
- Configure unique constraints in `ArtSpaceContext.OnModelCreating()` (e.g., `Email` is unique)

### Repository Implementation
```csharp
// Follow UserRepository pattern:
// 1. Inject ArtSpaceContext in constructor
// 2. Use FirstOrDefaultAsync/FindAsync for queries
// 3. Call SaveChangesAsync after writes
// 4. Return Task<Entity?> or Task<Entity>
```

### EF Core Metadata Configuration
Apply constraints in `OnModelCreating()`:
```csharp
modelBuilder.Entity<User>(entity =>
{
    entity.HasKey(u => u.Id);
    entity.HasIndex(u => u.Email).IsUnique();
    entity.Property(u => u.Email).IsRequired();
});
```

## Common Workflows

### Build & Run
```bash
dotnet build artspace.sln
dotnet run --project artspace.API
# Swagger UI available at https://localhost:7xxx/swagger
```

### Add EF Core Migration
```bash
cd artspace.Infrastructure
dotnet ef migrations add {MigrationName} --project artspace.Infrastructure --startup-project artspace.API
```

### Add Package Dependency
```bash
dotnet add {ProjectName} package {PackageName} [--version X.Y.Z]
# Infrastructure already has: EntityFrameworkCore, BCrypt.Net-Next, Npgsql
```

### Add New Entity
1. Create class in `artspace.Core/Entities/` with `Guid Id` and `DateTime CreatedAt`
2. Add `DbSet<Entity>` to `ArtSpaceContext`
3. Configure in `OnModelCreating()` with key, constraints, indexes
4. Create `IEntityRepository` interface in `artspace.Core/Interfaces/`
5. Implement in `artspace.Infrastructure/Services/EntityRepository.cs`

## Cross-Component Communication

- **API → Core**: Reference interfaces only; never directly reference Infrastructure
- **Infrastructure → Core**: Reference entities and interfaces for implementation
- **Dependency Injection**: Wire up in `Program.cs` (e.g., `builder.Services.AddScoped<IUserRepository, UserRepository>()`)

## File Reference Guide

- [artspace.API/Program.cs](../artspace.API/Program.cs) — Service registration and middleware pipeline
- [artspace.Infrastructure/Data/ArtSpaceContext.cs](../artspace.Infrastructure/Data/ArtSpaceContext.cs) — EF Core model configuration
- [artspace.Core/Entities/User.cs](../artspace.Core/Entities/User.cs) — Example entity with required/optional fields
- [artspace.Infrastructure/Services/UserRepository.cs](../artspace.Infrastructure/Services/UserRepository.cs) — Example async repository implementation
- [artspace.Core/Interfaces/IUserRepository.cs](../artspace.Core/Interfaces/IUserRepository.cs) — Example repository interface contract
