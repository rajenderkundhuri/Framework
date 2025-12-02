# Framework Documentation

Welcome to the Framework documentation. This is a modern, enterprise-grade application framework built with .NET 9.0, following Clean Architecture principles and Domain-Driven Design (DDD) patterns.

## Overview

Framework is a robust foundation for building scalable, maintainable, and testable enterprise applications. It implements industry best practices and provides essential building blocks for rapid application development.

## Key Features

### Core Architecture
- **Clean Architecture** - Clear separation of concerns with well-defined layers
- **Domain-Driven Design** - Rich domain models with business logic encapsulation
- **CQRS Pattern** - Command Query Responsibility Segregation for optimized operations
- **Repository Pattern** - Abstraction over data access logic
- **Unit of Work** - Transaction management
- **Specification Pattern** - Composable query specifications with And/Or/Not operators

### Event-Driven Architecture
- **Domain Events** - IDomainEvent, IEventHandler<T>, IEventDispatcher
- **In-Memory Event Dispatcher** - DI-based event handling with parallel dispatch
- **Outbox Pattern** - Reliable message delivery with retry and cleanup

### Multi-Tenancy
- **Tenant Resolution** - Header, query string, and subdomain strategies
- **Tenant Context** - ITenantContext for current tenant access
- **Data Isolation** - Per-tenant data filtering

### Audit & Logging
- **Audit Logging** - IAuditService with configurable entity tracking
- **Audit Fields** - CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, DeletedAt
- **Soft Delete** - ISoftDeletable support

### Background Jobs
- **Job Service** - IJobService abstraction
- **Hangfire Integration** - Fire-and-forget, delayed, and recurring jobs
- **Job Settings** - Configurable dashboard and worker options

### Caching
- **Cache Service** - ICacheService with Get, Set, Remove, GetOrCreate
- **Memory Cache** - In-memory caching implementation
- **Distributed Cache** - Redis support via IDistributedCache

### Email & Notifications
- **Email Service** - IEmailService with templates and attachments
- **SMTP Support** - SmtpEmailService with full configuration
- **Template Rendering** - File-based email templates with placeholders
- **Notification Service** - INotificationService for multi-channel notifications

### File Storage
- **Storage Service** - IStorageService abstraction
- **Local Storage** - File system storage with path validation
- **In-Memory Storage** - For testing and development
- **Metadata Support** - Content type, size, and custom metadata

### API Features
- **Rate Limiting** - IP, User, Client, and Endpoint-based policies
- **API Versioning** - Query, Header, URL Segment, and MediaType strategies
- **Health Checks** - Aggregated health reports with Database, Memory, URL checks
- **Request/Response Pipeline** - MediatR with behaviors

### Localization
- **Localization Service** - ILocalizationService with culture support
- **JSON Resources** - File-based localization with caching
- **Fallback Behavior** - Parent culture, default culture, or return key
- **Request Localization** - Header, query, and cookie-based culture detection

### Feature Flags
- **Feature Manager** - IFeatureManager for feature toggles
- **Filter Evaluators** - Percentage, TimeWindow, Targeting filters
- **Configuration Provider** - Settings-based feature definitions
- **Feature Gate Attribute** - Controller/Action level feature gating

### Validation & Security
- **FluentValidation** - Declarative validation rules
- **Result Pattern** - Consistent success/failure handling
- **Guard Clauses** - Input validation helpers

## Architecture Layers

```
┌─────────────────────────────────────┐
│         API Layer                   │
│  (Controllers, Endpoints, Filters)  │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│      Application Layer              │
│  (Commands, Queries, DTOs, Events)  │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│       Domain Layer                  │
│  (Entities, Value Objects, Specs)   │
└─────────────────────────────────────┘
              ↑
┌─────────────────────────────────────┐
│    Infrastructure Layer             │
│  (EF Core, Services, Integrations)  │
└─────────────────────────────────────┘
```

## Technology Stack

- **.NET 9.0** - Latest .NET runtime
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM and data access
- **MediatR** - Mediator pattern implementation
- **FluentValidation** - Validation framework
- **Hangfire** - Background job processing
- **xUnit** - Testing framework
- **Shouldly** - Fluent assertions
- **NSubstitute** - Mocking library

## Project Structure

```
Framework/
├── src/
│   ├── Framework.Domain/          # Domain entities, value objects, specs
│   ├── Framework.Application/     # Use cases, DTOs, interfaces
│   ├── Framework.Infrastructure/  # Implementations, data access
│   └── Framework.Api/             # REST API, controllers
├── tests/
│   ├── Framework.Domain.Tests/
│   ├── Framework.Application.Tests/
│   ├── Framework.Infrastructure.Tests/
│   └── Framework.Api.Tests/
└── docs/                          # Documentation
```

## Test Coverage

The framework includes comprehensive unit tests:

- **910 Total Tests**
- **Domain Tests**: 126 tests
- **Application Tests**: 396 tests
- **Infrastructure Tests**: 387 tests
- **API Tests**: 1 test

## Quick Start

```csharp
// Register services
services.AddFrameworkDomain();
services.AddFrameworkApplication();
services.AddFrameworkInfrastructure(configuration);

// Use features
public class MyHandler : ICommandHandler<MyCommand>
{
    private readonly ICacheService _cache;
    private readonly IEventDispatcher _events;
    private readonly IAuditService _audit;

    public async Task<Result> HandleAsync(MyCommand command)
    {
        // Business logic with framework services
    }
}
```

## Configuration

```json
{
  "MultiTenancy": {
    "Enabled": true,
    "DefaultTenantId": "default"
  },
  "Caching": {
    "DefaultDurationMinutes": 30
  },
  "Email": {
    "Provider": "Smtp",
    "FromAddress": "noreply@example.com"
  },
  "FeatureFlags": {
    "Enabled": true,
    "Features": {
      "NewFeature": { "Enabled": true }
    }
  },
  "HealthChecks": {
    "Enabled": true,
    "Path": "/health"
  },
  "Localization": {
    "DefaultCulture": "en-US",
    "SupportedCultures": ["en-US", "de-DE"]
  }
}
```

## Documentation Structure

### Getting Started
- [Getting Started Guide](getting-started.md) - Quick start, installation, and basic setup

### Architecture & Design
- [Architecture Overview](architecture.md) - Clean Architecture layers and patterns
- [Domain Layer](domain-layer.md) - Entities, value objects, and domain logic
- [Application Layer](application-layer.md) - Commands, queries, and business workflows
- [Infrastructure Layer](infrastructure-layer.md) - Data access, external services
- [API Layer](api-layer.md) - RESTful APIs, controllers, and endpoints

### Development Guide
- [Developer Guide](DeveloperGuide.md) - Comprehensive guide to all framework features
- [Adding New Modules](adding-new-module.md) - Step-by-step guide to adding new functionality
- [Coding Standards](coding-standards.md) - Naming conventions, formatting, best practices
- [Testing Guide](testing.md) - Unit tests, integration tests, best practices
- [Modules](modules.md) - Built-in modules and features

## Contributing

When working with this framework:

1. Follow Clean Architecture principles
2. Keep domain logic in the Domain layer
3. Use CQRS for all business operations
4. Write tests for all new features
5. Follow the coding standards defined in `.editorconfig`

## Version

Current Version: 2.0.0

## License

Copyright (c) 2024-2025 Framework Team
