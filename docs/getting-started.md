# Getting Started

This guide will help you get up and running with the Framework project quickly.

## Prerequisites

Before you begin, ensure you have the following installed:

### Required

- **.NET 9.0 SDK** or later
  - Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
  - Verify installation: `dotnet --version`

### Recommended

- **IDE** - Choose one:
  - Visual Studio 2022 (v17.8 or later) with ASP.NET and web development workload
  - Visual Studio Code with C# extension
  - JetBrains Rider 2024.1 or later

- **Database**
  - SQL Server 2019 or later (for production)
  - SQL Server LocalDB or SQLite (for development)

- **Git** - For version control
  - Download from [https://git-scm.com/](https://git-scm.com/)

## Installation

### 1. Clone the Repository

```bash
git clone <repository-url>
cd framework
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

This will build all projects in the solution:
- Framework.Domain
- Framework.Application
- Framework.Infrastructure
- Framework.Api

### 4. Verify the Build

Ensure all projects compile successfully:

```bash
dotnet build --configuration Release
```

## Project Structure

The Framework follows Clean Architecture principles with clear separation of concerns:

```
Framework/
├── src/
│   ├── Framework.Domain/
│   │   ├── Entities/              # Domain entities
│   │   ├── ValueObjects/          # Value objects
│   │   ├── Events/                # Domain events
│   │   ├── Specifications/        # Domain specifications
│   │   ├── Exceptions/            # Domain exceptions
│   │   └── Interfaces/            # Domain interfaces
│   │
│   ├── Framework.Application/
│   │   ├── Commands/              # CQRS commands
│   │   ├── Queries/               # CQRS queries
│   │   ├── DTOs/                  # Data transfer objects
│   │   ├── Mappings/              # Object mappings
│   │   ├── Validators/            # FluentValidation validators
│   │   ├── Behaviors/             # MediatR pipeline behaviors
│   │   └── Interfaces/            # Application interfaces
│   │
│   ├── Framework.Infrastructure/
│   │   ├── Persistence/           # EF Core DbContext
│   │   │   ├── Configurations/    # Entity configurations
│   │   │   ├── Migrations/        # Database migrations
│   │   │   └── Repositories/      # Repository implementations
│   │   ├── Services/              # External services
│   │   └── DependencyInjection.cs # Service registration
│   │
│   └── Framework.Api/
│       ├── Controllers/           # API controllers
│       ├── Filters/               # Action filters
│       ├── Middleware/            # Custom middleware
│       ├── Extensions/            # Extension methods
│       └── Program.cs             # Application entry point
│
├── tests/
│   ├── Framework.Domain.Tests/    # Domain layer tests
│   ├── Framework.Application.Tests/ # Application layer tests
│   └── Framework.Api.Tests/       # API layer tests
│
├── docs/                          # Documentation
├── Framework.sln                  # Solution file
├── Directory.Build.props          # Shared MSBuild properties
└── .editorconfig                  # Code style configuration
```

## Layer Responsibilities

### Domain Layer (`Framework.Domain`)
- Contains the core business logic and domain models
- No dependencies on other layers
- Defines entities, value objects, domain events, and business rules
- Pure C# with no framework dependencies

### Application Layer (`Framework.Application`)
- Implements use cases and business workflows
- Depends only on Domain layer
- Contains commands, queries, DTOs, and validation
- Uses MediatR for CQRS pattern

### Infrastructure Layer (`Framework.Infrastructure`)
- Implements data access and external integrations
- Depends on Application and Domain layers
- Contains EF Core context, repositories, and external services
- Implements interfaces defined in other layers

### API Layer (`Framework.Api`)
- Presents the RESTful API interface
- Depends on Application and Infrastructure layers
- Contains controllers, filters, and middleware
- Entry point for HTTP requests

## Running the Application

### Development Mode

Run the API project:

```bash
cd src/Framework.Api
dotnet run
```

Or with hot reload:

```bash
dotnet watch run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### Using Visual Studio

1. Open `Framework.sln` in Visual Studio
2. Set `Framework.Api` as the startup project
3. Press F5 to run with debugging, or Ctrl+F5 to run without debugging

### Using Visual Studio Code

1. Open the framework folder in VS Code
2. Open the Terminal (Ctrl+`)
3. Run: `dotnet run --project src/Framework.Api/Framework.Api.csproj`

## Configuration

### Application Settings

Configuration is managed through `appsettings.json` files:

- `appsettings.json` - Default settings
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings

Example configuration structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FrameworkDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment Variables

You can override settings using environment variables:

```bash
# Windows
set ConnectionStrings__DefaultConnection="Server=.;Database=FrameworkDb;..."

# Linux/Mac
export ConnectionStrings__DefaultConnection="Server=.;Database=FrameworkDb;..."
```

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Tests for Specific Project

```bash
dotnet test tests/Framework.Application.Tests/Framework.Application.Tests.csproj
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Tests in Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All" or run individual tests

## Database Setup

### Using Migrations (Recommended)

When the Infrastructure layer includes EF Core migrations:

```bash
# Navigate to the API project
cd src/Framework.Api

# Apply migrations
dotnet ef database update

# Or create a new migration
dotnet ef migrations add InitialCreate --project ../Framework.Infrastructure
```

### Connection String

Update the connection string in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FrameworkDb;Trusted_Connection=true"
  }
}
```

## Next Steps

Now that you have the Framework running, explore the following:

1. **[Architecture](architecture.md)** - Understand the Clean Architecture implementation
2. **[Domain Layer](domain-layer.md)** - Learn about entities, value objects, and domain logic
3. **[Application Layer](application-layer.md)** - Discover CQRS commands and queries
4. **[Infrastructure Layer](infrastructure-layer.md)** - Explore data access and repositories
5. **[API Layer](api-layer.md)** - Build RESTful endpoints
6. **[Testing](testing.md)** - Write comprehensive tests

## Common Commands Reference

```bash
# Build
dotnet build                          # Build all projects
dotnet build --configuration Release  # Release build

# Run
dotnet run --project src/Framework.Api/Framework.Api.csproj
dotnet watch run                      # Run with hot reload

# Test
dotnet test                          # Run all tests
dotnet test --logger "console;verbosity=detailed"  # Detailed output

# Clean
dotnet clean                         # Clean build artifacts

# Database Migrations
dotnet ef migrations add <MigrationName> --project src/Framework.Infrastructure
dotnet ef database update --project src/Framework.Api
dotnet ef database drop --project src/Framework.Api

# Package Management
dotnet add package <PackageName>     # Add NuGet package
dotnet restore                       # Restore packages
dotnet list package                  # List installed packages
```

## Troubleshooting

### Build Errors

**Issue**: "The type or namespace name could not be found"
- Solution: Run `dotnet restore` to ensure all NuGet packages are restored

**Issue**: "The command 'dotnet' is not found"
- Solution: Ensure .NET 9.0 SDK is installed and in your PATH

### Runtime Errors

**Issue**: "Unable to connect to the database"
- Solution: Verify your connection string in `appsettings.json`
- Ensure SQL Server/LocalDB is running

**Issue**: Port already in use
- Solution: Change the port in `Properties/launchSettings.json` or stop the conflicting application

### Migration Errors

**Issue**: "Build failed" when running migrations
- Solution: Build the solution first with `dotnet build`

**Issue**: "No DbContext was found"
- Solution: Ensure you're running the command from the correct project directory

## Development Tips

1. **Use Hot Reload**: Run with `dotnet watch run` for automatic recompilation
2. **Follow Coding Standards**: The project includes `.editorconfig` for consistent formatting
3. **Write Tests First**: Consider TDD approach for new features
4. **Use the Layered Approach**: Keep business logic in Domain, use cases in Application
5. **Leverage Dependency Injection**: Register services properly for testability

## Additional Resources

- [.NET 9.0 Documentation](https://docs.microsoft.com/dotnet)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
