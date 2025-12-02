# SQL Server Migrations

This folder contains database migrations for Microsoft SQL Server.

## Generating Migrations

To generate migrations for SQL Server, configure your application to use SQL Server:

```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=FrameworkDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Then run the EF Core migration command:

```bash
dotnet ef migrations add InitialCreate --project src/Framework.Infrastructure --startup-project src/Framework.Api --output-dir Persistence/Migrations/SqlServer --context ApplicationDbContext
```

## Applying Migrations

```bash
dotnet ef database update --project src/Framework.Infrastructure --startup-project src/Framework.Api --context ApplicationDbContext
```

## Notes

- SQL Server uses different data types and conventions than MySQL
- The migration files will be generated automatically by EF Core
- Review generated migrations before applying to production databases
