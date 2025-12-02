# MySQL Migrations

This folder contains database migrations for MySQL/MariaDB.

## Configuration

Configure your application to use MySQL:

```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=localhost;Database=frameworkdb;User=root;Password=yourpassword;",
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 30
  }
}
```

## Generating Migrations

Run the EF Core migration command:

```bash
dotnet ef migrations add InitialCreate --project src/Framework.Infrastructure --startup-project src/Framework.Api --output-dir Persistence/Migrations/MySql --context ApplicationDbContext
```

## Applying Migrations

```bash
dotnet ef database update --project src/Framework.Infrastructure --startup-project src/Framework.Api --context ApplicationDbContext
```

## Manual Migration

If you need to create the database manually, use the `V001_InitialCreate.sql` script in this folder.

## Notes

- MySQL uses different data types than SQL Server and PostgreSQL
- Use `utf8mb4` character set for full Unicode support
- Consider using `InnoDB` storage engine for transaction support
