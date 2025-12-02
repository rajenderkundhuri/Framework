# Code Generator CLI (`fwgen`)

The Framework Code Generator is a powerful CLI tool that generates full-stack CRUD code from your domain entity definitions. It analyzes your entities and generates all the boilerplate code following the Framework's patterns and conventions.

## Overview

The code generator can create:

- **API Layer**: Commands, Queries, Handlers, Validators, DTOs, Controllers
- **Admin UI Layer**: Blazor pages with MudDataGrid, Create/Edit dialogs, API services

## Installation

### As a Global Tool

```bash
# Build and pack the tool
dotnet pack src/Framework.CodeGen

# Install globally
dotnet tool install --global --add-source ./nupkg Framework.CodeGen

# Verify installation
fwgen --help
```

### Running Without Installation

```bash
dotnet run --project src/Framework.CodeGen -- generate -e MyEntity -a ./path/to/assembly.dll
```

## Quick Start

### 1. Create Your Entity

```csharp
// src/Framework.Domain/Catalog/Product.cs
public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign key
    public Guid CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
}
```

### 2. Build Your Domain Project

```bash
dotnet build src/Framework.Domain
```

### 3. Generate Code

```bash
fwgen generate -e Product -a ./src/Framework.Domain/bin/Debug/net9.0/Framework.Domain.dll -o ./src
```

### 4. Register Services

Add to `Framework.Admin/Program.cs`:
```csharp
builder.Services.AddScoped<IProductApiService, ProductApiService>();
```

Add permissions to `Framework.Application/Identity/Permissions.cs`:
```csharp
// Products
public const string ProductsView = "Products.View";
public const string ProductsCreate = "Products.Create";
public const string ProductsEdit = "Products.Edit";
public const string ProductsDelete = "Products.Delete";
```

## Commands

### `generate`

Generates CRUD code for an entity.

```bash
fwgen generate [options]
```

**Options:**

| Option | Short | Description | Required |
|--------|-------|-------------|----------|
| `--entity` | `-e` | Entity name or source file path | Yes |
| `--assembly` | `-a` | Path to compiled assembly | Yes (for reflection) |
| `--output` | `-o` | Output directory | No (default: current) |
| `--namespace` | `-n` | Custom namespace | No |
| `--source` | `-s` | Use Roslyn source parsing | No |
| `--api-only` | | Generate only API code | No |
| `--admin-only` | | Generate only Admin UI | No |
| `--overwrite` | `-f` | Overwrite existing files | No |
| `--dry-run` | | Preview without creating files | No |
| `--templates` | `-t` | Custom templates directory | No |

**Examples:**

```bash
# Full-stack generation with reflection
fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll -o ./src

# Source parsing (no compilation needed)
fwgen generate -e ./src/Framework.Domain/Catalog/Product.cs --source -o ./src

# API only
fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll --api-only

# Admin UI only
fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll --admin-only

# With custom namespace
fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll -n MyApp.Catalog

# Dry run to preview
fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll --dry-run
```

### `init`

Initializes custom templates in your project.

```bash
fwgen init [options]
```

**Options:**

| Option | Short | Description |
|--------|-------|-------------|
| `--output` | `-o` | Output directory (default: `.fwgen/templates`) |

**Example:**

```bash
fwgen init -o ./templates
```

## Entity Analysis

### Reflection Mode (Default)

Uses .NET reflection to analyze compiled entities. This is the most accurate method as it has full type information.

**Requirements:**
- Entity must be compiled into a DLL
- Assembly path must be provided with `-a`

**Detects:**
- Base class hierarchy (Entity, AuditableEntity, FullAuditableEntity, etc.)
- Property types and nullability
- Data annotations (MaxLength, Required, etc.)
- Navigation properties
- Foreign key relationships

### Source Parsing Mode

Uses Roslyn to parse C# source files directly. Useful when you don't want to compile.

**Usage:**
```bash
fwgen generate -e ./path/to/Entity.cs --source
```

**Limitations:**
- Cannot resolve types from other files
- Enum detection is limited

## Generated Code Structure

### API Layer

```
Framework.Application/{EntityPlural}/
├── Commands/
│   ├── Create{Entity}Command.cs
│   ├── Create{Entity}CommandHandler.cs
│   ├── Create{Entity}CommandValidator.cs
│   ├── Update{Entity}Command.cs
│   ├── Update{Entity}CommandHandler.cs
│   ├── Update{Entity}CommandValidator.cs
│   ├── Delete{Entity}Command.cs
│   └── Delete{Entity}CommandHandler.cs
├── Queries/
│   ├── Get{Entity}ByIdQuery.cs
│   ├── Get{Entity}ByIdQueryHandler.cs
│   ├── Get{Entities}Query.cs
│   └── Get{Entities}QueryHandler.cs
├── {Entity}ListRequest.cs
├── {Entity}ListResponse.cs
└── {Entity}DetailResponse.cs

Framework.Api/Controllers/
└── {Entities}Controller.cs
```

### Admin UI Layer

```
Framework.Admin/
├── Services/
│   └── {Entity}ApiService.cs
└── Pages/
    ├── {Entities}.razor
    └── {Entity}Dialog.razor
```

## Foreign Key Handling

The generator automatically detects foreign keys and generates:

1. **Dropdown filters** on list pages
2. **Dropdown selectors** in create/edit dialogs
3. **Related entity loading** in API services

### Detection Rules

Foreign keys are detected when:
- Property name ends with `Id` (e.g., `CategoryId`)
- There's a corresponding navigation property (e.g., `Category`)

### Generated Code

**List Page Filter:**
```razor
<MudSelect T="Guid?" @bind-Value="_categoryIdFilter" Label="Category" Clearable="true">
    @foreach (var item in _categoryList)
    {
        <MudSelectItem T="Guid?" Value="@item.Id">@item.Name</MudSelectItem>
    }
</MudSelect>
```

**Dialog Dropdown:**
```razor
<MudSelect T="Guid" @bind-Value="_model.CategoryId" Label="Category" Required="true">
    @foreach (var item in _categoryList)
    {
        <MudSelectItem T="Guid" Value="@item.Id">@item.Name</MudSelectItem>
    }
</MudSelect>
```

## Property Type Mapping

The generator maps property types to appropriate form controls:

| Property Type | Form Control |
|---------------|--------------|
| `string` | MudTextField |
| `string` (Email) | MudTextField (InputType.Email) |
| `string` (Password) | MudTextField (InputType.Password) |
| `string` (>255 chars) | MudTextField (Lines=3) |
| `int`, `decimal`, etc. | MudNumericField |
| `bool` | MudCheckBox |
| `DateTime` | MudDatePicker |
| `TimeSpan` | MudTimePicker |
| `Guid` (FK) | MudSelect (dropdown) |
| `enum` | MudSelect |

## Template Customization

### Extracting Default Templates

```bash
fwgen init -o ./templates
```

This creates:
```
templates/
├── Api/
│   ├── CreateCommand.scriban
│   ├── UpdateCommand.scriban
│   ├── DeleteCommand.scriban
│   ├── CreateCommandHandler.scriban
│   ├── UpdateCommandHandler.scriban
│   ├── DeleteCommandHandler.scriban
│   ├── CreateCommandValidator.scriban
│   ├── UpdateCommandValidator.scriban
│   ├── GetByIdQuery.scriban
│   ├── GetListQuery.scriban
│   ├── GetByIdQueryHandler.scriban
│   ├── GetListQueryHandler.scriban
│   ├── ListRequest.scriban
│   ├── ListResponse.scriban
│   ├── DetailResponse.scriban
│   ├── Controller.scriban
│   └── Permissions.scriban
└── Admin/
    ├── ApiService.scriban
    ├── ListPage.scriban
    └── Dialog.scriban
```

### Using Custom Templates

```bash
fwgen generate -e Product -a ./assembly.dll -t ./templates
```

### Template Variables

| Variable | Description |
|----------|-------------|
| `entity_name` | Entity name (e.g., "Product") |
| `entity_plural` | Plural form (e.g., "Products") |
| `namespace` | Target namespace |
| `route_path` | API route path (e.g., "products") |
| `is_auditable` | Entity has audit properties |
| `is_soft_delete` | Entity supports soft delete |
| `create_properties` | Properties for create command |
| `update_properties` | Properties for update command |
| `list_properties` | Properties for list display |
| `filter_properties` | Filterable properties |
| `foreign_keys` | Foreign key relationships |

## Excluded Properties

The following properties are automatically excluded from generated commands:

- `Id` - Auto-generated primary key
- `CreatedAt`, `CreatedBy` - Audit properties
- `LastModifiedAt`, `LastModifiedBy` - Audit properties
- `IsDeleted`, `DeletedAt`, `DeletedBy` - Soft delete properties
- `TenantId` - Multi-tenancy property
- Navigation properties (virtual collections)

## Best Practices

1. **Build before generating** - Ensure your Domain project is compiled
2. **Use dry-run first** - Preview changes with `--dry-run`
3. **Don't overwrite blindly** - Review generated code before using `-f`
4. **Customize templates** - Adapt templates to your team's conventions
5. **Register services** - Don't forget to register generated services in DI

## Troubleshooting

### Entity Not Found

```
Error: Entity 'Product' not found in assembly
```

**Solution:** Ensure the entity is public and not abstract.

### Assembly Not Found

```
Error: Assembly path is required for reflection mode
```

**Solution:** Provide the `-a` flag with path to compiled DLL.

### Template Not Found

```
Error: Template not found: Api/CreateCommand.scriban
```

**Solution:** Check custom templates path or use default templates.
