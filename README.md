# Framework

A modern, enterprise-grade application framework built with .NET 9.0, featuring a Blazor WebAssembly Admin Panel. Follows Clean Architecture principles and Domain-Driven Design (DDD) patterns.

## Overview

Framework provides a robust foundation for building scalable, maintainable, and testable enterprise applications. It includes a fully-featured API backend and a modern Admin UI for complete application management.

## Technology Stack

| Component | Technology |
|-----------|------------|
| Backend | .NET 9.0, ASP.NET Core |
| Frontend | Blazor WebAssembly |
| UI Framework | MudBlazor |
| ORM | Entity Framework Core |
| Database | SQL Server, PostgreSQL, MySQL |
| Authentication | JWT Bearer Tokens |
| Background Jobs | Hangfire |
| Logging | Serilog |
| Validation | FluentValidation |
| Mediator | MediatR |
| Testing | xUnit, Shouldly, NSubstitute |
| Code Generation | Scriban, Roslyn |

## Architecture

```
+--------------------------------------------------+
|                    API Layer                      |
|         (Controllers, Endpoints, Filters)         |
+--------------------------------------------------+
                        |
+--------------------------------------------------+
|               Application Layer                   |
|       (Commands, Queries, DTOs, Validators)       |
+--------------------------------------------------+
                        |
+--------------------------------------------------+
|                  Domain Layer                     |
|      (Entities, Value Objects, Specifications)    |
+--------------------------------------------------+
                        |
+--------------------------------------------------+
|              Infrastructure Layer                 |
|       (EF Core, Services, External APIs)          |
+--------------------------------------------------+
```

## Key Features

### Core Architecture
- **Clean Architecture** - Clear separation of concerns with well-defined layers
- **Domain-Driven Design** - Rich domain models with business logic encapsulation
- **CQRS Pattern** - Command Query Responsibility Segregation
- **Repository Pattern** - Abstraction over data access
- **Unit of Work** - Transaction management
- **Specification Pattern** - Composable query specifications

### Security & Authentication
- **JWT Authentication** - Secure token-based authentication
- **Permission-Based Authorization** - Fine-grained access control
- **Two-Factor Authentication** - TOTP-based 2FA with QR code setup
- **API Key Authentication** - Secure API access for integrations
- **Password Policies** - Configurable password requirements

### Multi-Tenancy
- **Tenant Resolution** - Header, query string, and subdomain strategies
- **Data Isolation** - Per-tenant data filtering
- **Tenant Management** - Full CRUD operations for tenants

### Background Processing
- **Hangfire Integration** - Reliable background job processing
- **Fire-and-Forget Jobs** - Immediate execution
- **Delayed Jobs** - Scheduled execution
- **Recurring Jobs** - Cron-based scheduling
- **Job Dashboard** - Visual job monitoring

### Caching
- **Memory Cache** - In-memory caching
- **Distributed Cache** - Redis support
- **Cache Abstraction** - ICacheService interface

### Email & Notifications
- **Email Service** - Template-based emails with SMTP support
- **Email Templates** - Dynamic content with placeholders
- **Email Logging** - Track sent emails
- **Notification Service** - Multi-channel notifications

### Localization
- **Multi-Language Support** - Configurable languages
- **Resource Management** - Key-value based translations
- **Culture Detection** - Request-based culture resolution

### Observability
- **Structured Logging** - Serilog with Console/File sinks
- **Audit Logging** - Track all entity changes
- **Health Checks** - Database, Memory, URL checks
- **Request Logging** - HTTP request/response logging

### API Features
- **Rate Limiting** - IP, User, Client, Endpoint-based
- **API Versioning** - Query, Header, URL Segment strategies
- **Swagger/OpenAPI** - Interactive API documentation
- **Result Pattern** - Consistent API responses

### Code Generation CLI
- **Entity Analysis** - Reflection and Roslyn source parsing
- **Full Stack Generation** - API + Admin UI from entity definitions
- **Foreign Key Detection** - Auto-generates dropdowns for relationships
- **Customizable Templates** - Scriban-based templates
- **CRUD Generation** - Commands, Queries, Handlers, Controllers, Blazor Pages

---

## Admin Panel Screens

The Framework includes a complete Blazor WebAssembly Admin Panel with the following screens:

### Dashboard
| Screen | Description |
|--------|-------------|
| **Dashboard** | Overview with statistics widgets, charts, and recent activity |

### Identity & Access Management

| Screen | Description |
|--------|-------------|
| **Users** | User management with create, edit, delete, and role assignment |
| **Roles** | Role management with permission assignment |
| **Permissions** | View and manage system permissions |
| **API Keys** | Generate and manage API keys for integrations |
| **Two-Factor Auth** | Enable/disable 2FA, view recovery codes |

### Tenant Management

| Screen | Description |
|--------|-------------|
| **Tenants** | Multi-tenant management (create, edit, delete) |
| **Tenant Features** | Manage features enabled per tenant |
| **Tenant Users** | View and manage users within a tenant |

### Communication

| Screen | Description |
|--------|-------------|
| **Email Templates** | Create and manage email templates with placeholders |
| **Email Logs** | View sent email history and status |
| **Notifications** | Manage and send notifications |

### Localization

| Screen | Description |
|--------|-------------|
| **Languages** | Configure supported languages |
| **Resources** | Manage localization strings (key-value pairs) |

### System Configuration

| Screen | Description |
|--------|-------------|
| **Settings** | Application settings management |
| **Feature Flags** | Enable/disable features dynamically |

### Monitoring & Audit

| Screen | Description |
|--------|-------------|
| **Audit Logs** | View system audit trail with filtering |
| **Background Jobs** | Hangfire dashboard for job monitoring |
| **Health Checks** | System health status overview |

### User Account

| Screen | Description |
|--------|-------------|
| **Profile** | User profile management |
| **Change Password** | Password update functionality |
| **Security Settings** | Two-factor authentication setup |

---

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authenticate user |
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/refresh-token` | Refresh JWT token |
| POST | `/api/auth/forgot-password` | Request password reset |
| POST | `/api/auth/reset-password` | Reset password |
| POST | `/api/auth/confirm-email` | Confirm email address |

### Two-Factor Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/two-factor/status` | Get 2FA status |
| POST | `/api/two-factor/setup` | Setup authenticator |
| POST | `/api/two-factor/confirm` | Confirm authenticator |
| POST | `/api/two-factor/disable` | Disable 2FA |
| POST | `/api/two-factor/recovery-codes` | Generate new recovery codes |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List all users (paginated) |
| GET | `/api/users/{id}` | Get user by ID |
| POST | `/api/users` | Create new user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user |
| GET | `/api/users/{id}/roles` | Get user roles |
| PUT | `/api/users/{id}/roles` | Update user roles |

### Roles
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/roles` | List all roles |
| GET | `/api/roles/{id}` | Get role by ID |
| POST | `/api/roles` | Create new role |
| PUT | `/api/roles/{id}` | Update role |
| DELETE | `/api/roles/{id}` | Delete role |
| GET | `/api/roles/{id}/permissions` | Get role permissions |
| PUT | `/api/roles/{id}/permissions` | Update role permissions |

### API Keys
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/apikeys` | List all API keys |
| GET | `/api/apikeys/{id}` | Get API key by ID |
| POST | `/api/apikeys` | Create new API key |
| PUT | `/api/apikeys/{id}` | Update API key |
| DELETE | `/api/apikeys/{id}` | Delete/revoke API key |

### Tenants
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tenants` | List all tenants |
| GET | `/api/tenants/{id}` | Get tenant by ID |
| POST | `/api/tenants` | Create new tenant |
| PUT | `/api/tenants/{id}` | Update tenant |
| DELETE | `/api/tenants/{id}` | Delete tenant |
| GET | `/api/tenants/{id}/features` | Get tenant features |
| PUT | `/api/tenants/{id}/features` | Update tenant features |

### Email Templates
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/emailtemplates` | List all templates |
| GET | `/api/emailtemplates/{id}` | Get template by ID |
| POST | `/api/emailtemplates` | Create new template |
| PUT | `/api/emailtemplates/{id}` | Update template |
| DELETE | `/api/emailtemplates/{id}` | Delete template |
| GET | `/api/emailtemplates/logs` | Get email logs |

### Notifications
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications` | List notifications |
| GET | `/api/notifications/{id}` | Get notification by ID |
| POST | `/api/notifications` | Create notification |
| PUT | `/api/notifications/{id}/read` | Mark as read |
| PUT | `/api/notifications/read-all` | Mark all as read |
| DELETE | `/api/notifications/{id}` | Delete notification |

### Localization
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/languages` | List all languages |
| GET | `/api/languages/{id}` | Get language by ID |
| POST | `/api/languages` | Create new language |
| PUT | `/api/languages/{id}` | Update language |
| DELETE | `/api/languages/{id}` | Delete language |
| GET | `/api/resources` | List all resources |
| GET | `/api/resources/{id}` | Get resource by ID |
| POST | `/api/resources` | Create new resource |
| PUT | `/api/resources/{id}` | Update resource |
| DELETE | `/api/resources/{id}` | Delete resource |

### Background Jobs
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/jobs` | List background jobs |
| GET | `/api/jobs/{id}` | Get job details |
| POST | `/api/jobs/{id}/requeue` | Requeue failed job |
| DELETE | `/api/jobs/{id}` | Delete job |
| GET | `/api/jobs/recurring` | List recurring jobs |
| POST | `/api/jobs/recurring/{id}/trigger` | Trigger recurring job |
| DELETE | `/api/jobs/recurring/{id}` | Remove recurring job |

### Audit Logs
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/audit-logs` | List audit logs (paginated, filterable) |
| GET | `/api/audit-logs/{id}` | Get audit log details |

### Settings
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/settings` | Get all settings |
| GET | `/api/settings/{key}` | Get setting by key |
| PUT | `/api/settings/{key}` | Update setting |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check endpoint |
| GET | `/health/ready` | Readiness check |
| GET | `/health/live` | Liveness check |

---

## Project Structure

```
Framework/
├── src/
│   ├── Framework.Domain/           # Domain entities, value objects, specifications
│   ├── Framework.Application/      # Use cases, DTOs, interfaces, validators
│   ├── Framework.Infrastructure/   # EF Core, service implementations
│   ├── Framework.Api/              # REST API, controllers, middleware
│   ├── Framework.Admin/            # Blazor WebAssembly Admin Panel
│   └── Framework.CodeGen/          # Code Generator CLI Tool
├── tests/
│   ├── Framework.Domain.Tests/     # Domain unit tests
│   ├── Framework.Application.Tests/# Application unit tests
│   ├── Framework.Infrastructure.Tests/# Infrastructure unit tests
│   └── Framework.Api.Tests/        # API integration tests
└── docs/                           # Documentation
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server, PostgreSQL, or MySQL
- Node.js (for frontend tooling, optional)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/framework.git
   cd framework
   ```

2. **Update connection string** in `src/Framework.Api/appsettings.json`
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=Framework;..."
     }
   }
   ```

3. **Run database migrations**
   ```bash
   cd src/Framework.Api
   dotnet ef database update
   ```

4. **Run the API**
   ```bash
   dotnet run --project src/Framework.Api
   ```

5. **Run the Admin Panel** (in a separate terminal)
   ```bash
   dotnet run --project src/Framework.Admin
   ```

6. **Access the applications**
   - API: `https://localhost:5001` (Swagger UI)
   - Admin Panel: `https://localhost:5002`

---

## Code Generator CLI (`fwgen`)

The Framework includes a powerful code generator CLI tool that generates full-stack CRUD code from your entity definitions.

### Installation

```bash
# Install as a global tool
dotnet pack src/Framework.CodeGen
dotnet tool install --global --add-source ./nupkg Framework.CodeGen

# Or run directly without installing
dotnet run --project src/Framework.CodeGen -- [commands]
```

### Usage

```bash
# Generate full-stack code using reflection (requires compiled DLL)
fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll -o ./src

# Generate code using Roslyn source parsing
fwgen generate -e ./src/Framework.Domain/Entities/Product.cs --source -o ./src

# Generate API only (Commands, Queries, Handlers, Controller)
fwgen generate -e Product -a ./path/to/assembly.dll --api-only

# Generate Admin UI only (Blazor Pages, Services)
fwgen generate -e Product -a ./path/to/assembly.dll --admin-only

# Preview what would be generated (dry run)
fwgen generate -e Product -a ./path/to/assembly.dll --dry-run

# Initialize custom templates for customization
fwgen init -o ./templates
```

### What It Generates

**API Layer:**
| File | Description |
|------|-------------|
| `Create{Entity}Command.cs` | Command for creating entities |
| `Update{Entity}Command.cs` | Command for updating entities |
| `Delete{Entity}Command.cs` | Command for deleting entities |
| `*CommandHandler.cs` | Handlers for all commands |
| `*CommandValidator.cs` | FluentValidation validators |
| `Get{Entity}ByIdQuery.cs` | Query to get entity by ID |
| `Get{Entities}Query.cs` | Paginated list query |
| `*QueryHandler.cs` | Handlers for all queries |
| `{Entity}ListResponse.cs` | DTO for list items |
| `{Entity}DetailResponse.cs` | DTO for entity details |
| `{Entities}Controller.cs` | API controller with CRUD endpoints |

**Admin UI Layer:**
| File | Description |
|------|-------------|
| `{Entity}ApiService.cs` | HTTP client service for API calls |
| `{Entities}.razor` | List page with MudDataGrid |
| `{Entity}Dialog.razor` | Create/Edit dialog with form |

### Features

- **Auto-detects foreign keys** - Generates dropdown selectors for relationships
- **Smart property mapping** - Maps types to appropriate form controls
- **MudDataGrid integration** - Server-side pagination, sorting, filtering
- **Permission-based** - Generates permission constants for authorization
- **Customizable templates** - Override default Scriban templates

### CLI Options

| Option | Description |
|--------|-------------|
| `-e, --entity` | Entity name or source file path (required) |
| `-o, --output` | Output directory |
| `-a, --assembly` | Path to compiled assembly (for reflection) |
| `-s, --source` | Use Roslyn source parsing instead of reflection |
| `-n, --namespace` | Custom namespace for generated code |
| `--api-only` | Generate only API code |
| `--admin-only` | Generate only Admin UI code |
| `-f, --overwrite` | Overwrite existing files |
| `--dry-run` | Preview without creating files |
| `-t, --templates` | Path to custom templates directory |

---

### Default Credentials

| Username | Password | Role |
|----------|----------|------|
| admin@framework.local | Admin123! | Administrator |

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Framework;..."
  },
  "JwtSettings": {
    "Secret": "your-secret-key-min-32-characters",
    "Issuer": "Framework",
    "Audience": "Framework.Api",
    "ExpirationMinutes": 60
  },
  "MultiTenancy": {
    "Enabled": true,
    "DefaultTenantId": "default"
  },
  "Email": {
    "Provider": "Smtp",
    "FromAddress": "noreply@example.com",
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587
  },
  "Hangfire": {
    "DashboardEnabled": true,
    "WorkerCount": 5
  },
  "HealthChecks": {
    "Enabled": true,
    "Path": "/health"
  }
}
```

## Test Coverage

| Layer | Tests |
|-------|-------|
| Domain | 126 |
| Application | 396 |
| Infrastructure | 387 |
| API | 1 |
| **Total** | **910** |

## Documentation

Detailed documentation is available in the `/docs` folder:

- [Getting Started Guide](docs/getting-started.md)
- [Architecture Overview](docs/architecture.md)
- [Developer Guide](docs/DeveloperGuide.md)
- [Adding New Modules](docs/adding-new-module.md)
- [Code Generator CLI](docs/code-generator.md)
- [Coding Standards](docs/coding-standards.md)
- [Testing Guide](docs/testing.md)
- [API Layer](docs/api-layer.md)
- [Domain Layer](docs/domain-layer.md)
- [Application Layer](docs/application-layer.md)
- [Infrastructure Layer](docs/infrastructure-layer.md)

## Contributing

1. Follow Clean Architecture principles
2. Keep domain logic in the Domain layer
3. Use CQRS for all business operations
4. Write tests for all new features
5. Follow the coding standards in [coding-standards.md](docs/coding-standards.md)

## Version

Current Version: **2.0.0**

## License

Copyright (c) 2024-2025 Framework Team. All rights reserved.
