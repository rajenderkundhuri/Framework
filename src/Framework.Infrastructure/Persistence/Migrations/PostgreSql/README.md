# PostgreSQL Migrations

This folder contains database migrations for PostgreSQL.

## Generating Migrations

To generate migrations for PostgreSQL, configure your application to use PostgreSQL:

```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=localhost;Database=frameworkdb;Username=postgres;Password=yourpassword"
  }
}
```

Then run the EF Core migration command:

```bash
dotnet ef migrations add InitialCreate --project src/Framework.Infrastructure --startup-project src/Framework.Api --output-dir Persistence/Migrations/PostgreSql --context ApplicationDbContext
```

## Applying Migrations

```bash
dotnet ef database update --project src/Framework.Infrastructure --startup-project src/Framework.Api --context ApplicationDbContext
```

## Notes

- PostgreSQL uses snake_case naming convention by default
- Consider using the Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime package for better DateTime handling
- The migration files will be generated automatically by EF Core
- Review generated migrations before applying to production databases
