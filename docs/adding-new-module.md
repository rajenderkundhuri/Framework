# Adding a New Module Guide

This guide walks you through the complete process of adding a new module to the Framework. We'll use a **"Products"** module as an example.

## Table of Contents

1. [Overview](#overview)
2. [Step 1: Domain Layer](#step-1-domain-layer)
3. [Step 2: Application Layer](#step-2-application-layer)
4. [Step 3: Infrastructure Layer](#step-3-infrastructure-layer)
5. [Step 4: API Layer](#step-4-api-layer)
6. [Step 5: Admin UI](#step-5-admin-ui)
7. [Step 6: Permissions](#step-6-permissions)
8. [Step 7: Database Migration](#step-7-database-migration)
9. [Step 8: Testing](#step-8-testing)
10. [Checklist](#checklist)

---

## Overview

When adding a new module, you'll create files across multiple layers following Clean Architecture:

```
Framework/
├── src/
│   ├── Framework.Domain/
│   │   └── Products/                    # Step 1: Domain entities
│   ├── Framework.Application/
│   │   └── Products/                    # Step 2: Commands, queries, DTOs
│   ├── Framework.Infrastructure/
│   │   └── Products/                    # Step 3: Services, repositories
│   ├── Framework.Api/
│   │   └── Controllers/ProductsController.cs  # Step 4: API endpoints
│   └── Framework.Admin/
│       └── Pages/Products/              # Step 5: Admin UI
└── tests/
    ├── Framework.Domain.Tests/Products/
    ├── Framework.Application.Tests/Products/
    └── Framework.Infrastructure.Tests/Products/
```

---

## Step 1: Domain Layer

The Domain layer contains your business entities and logic. It has no dependencies on other layers.

### 1.1 Create the Entity

**File:** `src/Framework.Domain/Products/Product.cs`

```csharp
using Framework.Domain.Common;

namespace Framework.Domain.Products;

/// <summary>
/// Product entity representing a sellable item
/// </summary>
public class Product : AuditableEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string? Sku { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public Guid CategoryId { get; private set; }

    // EF Core constructor
    private Product() { }

    public Product(string name, decimal price, Guid categoryId)
    {
        Id = Guid.NewGuid();
        SetName(name);
        SetPrice(price);
        CategoryId = categoryId;
        IsActive = true;
        StockQuantity = 0;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");

        if (name.Length > 200)
            throw new DomainException("Product name cannot exceed 200 characters");

        Name = name.Trim();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
    }

    public void SetPrice(decimal price)
    {
        if (price < 0)
            throw new DomainException("Price cannot be negative");

        Price = price;
    }

    public void SetSku(string? sku)
    {
        Sku = sku?.Trim().ToUpperInvariant();
    }

    public void UpdateStock(int quantity)
    {
        if (StockQuantity + quantity < 0)
            throw new DomainException("Insufficient stock");

        StockQuantity += quantity;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
```

### 1.2 Create Domain Events (Optional)

**File:** `src/Framework.Domain/Products/Events/ProductCreatedEvent.cs`

```csharp
using Framework.Domain.Events;

namespace Framework.Domain.Products.Events;

public class ProductCreatedEvent : IDomainEvent
{
    public Guid ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public ProductCreatedEvent(Guid productId, string name, decimal price)
    {
        ProductId = productId;
        Name = name;
        Price = price;
    }
}
```

### 1.3 Create Specifications (Optional)

**File:** `src/Framework.Domain/Products/Specifications/ActiveProductsSpec.cs`

```csharp
using Framework.Domain.Specifications;

namespace Framework.Domain.Products.Specifications;

public class ActiveProductsSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.IsActive;
    }
}

public class ProductsByCategory : Specification<Product>
{
    private readonly Guid _categoryId;

    public ProductsByCategory(Guid categoryId)
    {
        _categoryId = categoryId;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.CategoryId == _categoryId;
    }
}
```

---

## Step 2: Application Layer

The Application layer contains business use cases (commands/queries), DTOs, validators, and service interfaces.

### 2.1 Create DTOs

**File:** `src/Framework.Application/Products/ProductResponse.cs`

```csharp
namespace Framework.Application.Products;

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class ProductListRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public Guid CategoryId { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public Guid CategoryId { get; set; }
}
```

### 2.2 Create Service Interface

**File:** `src/Framework.Application/Products/IProductService.cs`

```csharp
using Framework.Application.Common.Models;

namespace Framework.Application.Products;

public interface IProductService
{
    Task<PagedList<ProductResponse>> GetProductsAsync(ProductListRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 2.3 Create Validators

**File:** `src/Framework.Application/Products/CreateProductRequestValidator.cs`

```csharp
using FluentValidation;

namespace Framework.Application.Products;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        RuleFor(x => x.Sku)
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Sku));
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");
    }
}
```

---

## Step 3: Infrastructure Layer

The Infrastructure layer contains implementations of application interfaces.

### 3.1 Add Entity Configuration

**File:** `src/Framework.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`

```csharp
using Framework.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Framework.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Sku)
            .HasMaxLength(50);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL");

        builder.HasIndex(p => p.CategoryId);

        builder.HasIndex(p => p.IsActive);
    }
}
```

### 3.2 Add DbSet to DbContext

**File:** `src/Framework.Infrastructure/Persistence/Context/ApplicationDbContext.cs`

Add the DbSet property:

```csharp
public DbSet<Product> Products => Set<Product>();
```

### 3.3 Implement the Service

**File:** `src/Framework.Infrastructure/Products/ProductService.cs`

```csharp
using Framework.Application.Common.Models;
using Framework.Application.Products;
using Framework.Domain.Products;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Framework.Infrastructure.Products;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<ProductResponse>> GetProductsAsync(
        ProductListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(request.SearchTerm) ||
                (p.Sku != null && p.Sku.Contains(request.SearchTerm)));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        // Apply sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => request.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "createdat" => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => MapToResponse(p))
            .ToListAsync(cancellationToken);

        return new PagedList<ProductResponse>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product == null ? null : MapToResponse(product);
    }

    public async Task<Result<Guid>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        // Check for duplicate SKU
        if (!string.IsNullOrEmpty(request.Sku))
        {
            var existingSku = await _context.Products
                .AnyAsync(p => p.Sku == request.Sku.ToUpperInvariant(), cancellationToken);

            if (existingSku)
            {
                return Result<Guid>.Failure("A product with this SKU already exists");
            }
        }

        var product = new Product(request.Name, request.Price, request.CategoryId);
        product.SetDescription(request.Description);
        product.SetSku(request.Sku);

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);

        if (product == null)
        {
            return Result.Failure("Product not found");
        }

        product.SetName(request.Name);
        product.SetDescription(request.Description);
        product.SetPrice(request.Price);
        product.SetSku(request.Sku);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);

        if (product == null)
        {
            return Result.Failure("Product not found");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);

        if (product == null)
        {
            return Result.Failure("Product not found");
        }

        product.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);

        if (product == null)
        {
            return Result.Failure("Product not found");
        }

        product.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Sku = product.Sku,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CreatedAt = product.CreatedAt,
            ModifiedAt = product.ModifiedAt
        };
    }
}
```

### 3.4 Register the Service

**File:** `src/Framework.Infrastructure/DependencyInjection.cs`

Add the service registration:

```csharp
// Product services
services.AddScoped<IProductService, ProductService>();
```

---

## Step 4: API Layer

The API layer exposes endpoints for the module.

### 4.1 Create the Controller

**File:** `src/Framework.Api/Controllers/ProductsController.cs`

```csharp
using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Product management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Get paginated list of products
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductListRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsView))
            return Forbid();

        var result = await _productService.GetProductsAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsView))
            return Forbid();

        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsCreate))
            return Forbid();

        var result = await _productService.CreateAsync(request, cancellationToken);
        return HandleCreatedResult(result, nameof(GetProduct), new { id = result.Value });
    }

    /// <summary>
    /// Update a product
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsEdit))
            return Forbid();

        var result = await _productService.UpdateAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsDelete))
            return Forbid();

        var result = await _productService.DeleteAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Activate a product
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProduct(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsEdit))
            return Forbid();

        var result = await _productService.ActivateAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivate a product
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsEdit))
            return Forbid();

        var result = await _productService.DeactivateAsync(id, cancellationToken);
        return HandleResult(result);
    }
}
```

---

## Step 5: Admin UI

Create the Blazor admin pages.

### 5.1 Create the API Service

**File:** `src/Framework.Admin/Services/ProductApiService.cs`

```csharp
using Framework.Application.Common.Models;
using Framework.Application.Products;
using System.Net.Http.Json;

namespace Framework.Admin.Services;

public class ProductApiService : BaseApiService
{
    public ProductApiService(HttpClient httpClient) : base(httpClient) { }

    public async Task<PagedList<ProductResponse>?> GetProductsAsync(ProductListRequest request)
    {
        var query = BuildQueryString(request);
        return await GetAsync<PagedList<ProductResponse>>($"api/products{query}");
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id)
    {
        return await GetAsync<ProductResponse>($"api/products/{id}");
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateProductRequest request)
    {
        return await PostAsync<CreateProductRequest, Guid>("api/products", request);
    }

    public async Task<ApiResponse> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        return await PutAsync($"api/products/{id}", request);
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        return await DeleteAsync($"api/products/{id}");
    }

    public async Task<ApiResponse> ActivateAsync(Guid id)
    {
        return await PostAsync($"api/products/{id}/activate");
    }

    public async Task<ApiResponse> DeactivateAsync(Guid id)
    {
        return await PostAsync($"api/products/{id}/deactivate");
    }
}
```

### 5.2 Create the List Page

**File:** `src/Framework.Admin/Pages/Products/Products.razor`

```razor
@page "/products"
@using Framework.Application.Products
@inject ProductApiService ProductService
@inject ISnackbar Snackbar
@inject IDialogService DialogService

<PageTitle>Products</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">Products</MudText>

<MudPaper Class="pa-4">
    <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-4">
        <MudTextField @bind-Value="_searchTerm"
                      Placeholder="Search products..."
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      Immediate="true"
                      DebounceInterval="300"
                      OnDebounceIntervalElapsed="@(_ => LoadProducts())" />
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="OpenCreateDialog">
            Add Product
        </MudButton>
    </MudStack>

    <MudTable @ref="_table"
              ServerData="@(new Func<TableState, Task<TableData<ProductResponse>>>(LoadServerData))"
              Hover="true"
              Breakpoint="Breakpoint.Sm"
              Loading="_loading"
              LoadingProgressColor="Color.Primary">
        <HeaderContent>
            <MudTh><MudTableSortLabel SortLabel="name" T="ProductResponse">Name</MudTableSortLabel></MudTh>
            <MudTh>SKU</MudTh>
            <MudTh><MudTableSortLabel SortLabel="price" T="ProductResponse">Price</MudTableSortLabel></MudTh>
            <MudTh>Stock</MudTh>
            <MudTh>Status</MudTh>
            <MudTh Style="width: 120px">Actions</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">@context.Name</MudTd>
            <MudTd DataLabel="SKU">@context.Sku</MudTd>
            <MudTd DataLabel="Price">@context.Price.ToString("C")</MudTd>
            <MudTd DataLabel="Stock">@context.StockQuantity</MudTd>
            <MudTd DataLabel="Status">
                <MudChip Color="@(context.IsActive ? Color.Success : Color.Default)" Size="Size.Small">
                    @(context.IsActive ? "Active" : "Inactive")
                </MudChip>
            </MudTd>
            <MudTd>
                <MudIconButton Icon="@Icons.Material.Filled.Edit"
                               Size="Size.Small"
                               OnClick="@(() => OpenEditDialog(context))" />
                <MudIconButton Icon="@Icons.Material.Filled.Delete"
                               Size="Size.Small"
                               Color="Color.Error"
                               OnClick="@(() => ConfirmDelete(context))" />
            </MudTd>
        </RowTemplate>
        <PagerContent>
            <MudTablePager />
        </PagerContent>
    </MudTable>
</MudPaper>

@code {
    private MudTable<ProductResponse>? _table;
    private string? _searchTerm;
    private bool _loading;

    private async Task<TableData<ProductResponse>> LoadServerData(TableState state)
    {
        _loading = true;
        StateHasChanged();

        var request = new ProductListRequest
        {
            PageNumber = state.Page + 1,
            PageSize = state.PageSize,
            SearchTerm = _searchTerm,
            SortBy = state.SortLabel,
            SortDescending = state.SortDirection == SortDirection.Descending
        };

        var result = await ProductService.GetProductsAsync(request);

        _loading = false;

        return new TableData<ProductResponse>
        {
            Items = result?.Items ?? new List<ProductResponse>(),
            TotalItems = result?.TotalCount ?? 0
        };
    }

    private async Task LoadProducts()
    {
        if (_table != null)
            await _table.ReloadServerData();
    }

    private async Task OpenCreateDialog()
    {
        var dialog = await DialogService.ShowAsync<ProductDialog>("Create Product");
        var result = await dialog.Result;

        if (!result.Canceled)
            await LoadProducts();
    }

    private async Task OpenEditDialog(ProductResponse product)
    {
        var parameters = new DialogParameters { ["Product"] = product };
        var dialog = await DialogService.ShowAsync<ProductDialog>("Edit Product", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
            await LoadProducts();
    }

    private async Task ConfirmDelete(ProductResponse product)
    {
        var result = await DialogService.ShowMessageBox(
            "Delete Product",
            $"Are you sure you want to delete '{product.Name}'?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (result == true)
        {
            var response = await ProductService.DeleteAsync(product.Id);
            if (response.Success)
            {
                Snackbar.Add("Product deleted successfully", Severity.Success);
                await LoadProducts();
            }
            else
            {
                Snackbar.Add(response.Error ?? "Failed to delete product", Severity.Error);
            }
        }
    }
}
```

### 5.3 Create the Dialog

**File:** `src/Framework.Admin/Pages/Products/ProductDialog.razor`

```razor
@using Framework.Application.Products
@inject ProductApiService ProductService
@inject ISnackbar Snackbar

<MudDialog>
    <DialogContent>
        <MudForm @ref="_form" @bind-IsValid="_formValid">
            <MudTextField @bind-Value="_model.Name"
                          Label="Name"
                          Required="true"
                          RequiredError="Name is required"
                          MaxLength="200" />

            <MudTextField @bind-Value="_model.Description"
                          Label="Description"
                          Lines="3"
                          MaxLength="2000" />

            <MudNumericField @bind-Value="_model.Price"
                             Label="Price"
                             Min="0"
                             Format="N2"
                             Adornment="Adornment.Start"
                             AdornmentText="$" />

            <MudTextField @bind-Value="_model.Sku"
                          Label="SKU"
                          MaxLength="50" />
        </MudForm>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="Submit"
                   Disabled="!_formValid || _saving">
            @(_saving ? "Saving..." : "Save")
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public ProductResponse? Product { get; set; }

    private MudForm? _form;
    private bool _formValid;
    private bool _saving;
    private ProductFormModel _model = new();

    private bool IsEditMode => Product != null;

    protected override void OnInitialized()
    {
        if (Product != null)
        {
            _model = new ProductFormModel
            {
                Name = Product.Name,
                Description = Product.Description,
                Price = Product.Price,
                Sku = Product.Sku,
                CategoryId = Product.CategoryId
            };
        }
    }

    private void Cancel() => MudDialog?.Cancel();

    private async Task Submit()
    {
        if (_form == null) return;

        await _form.Validate();
        if (!_formValid) return;

        _saving = true;

        try
        {
            if (IsEditMode)
            {
                var request = new UpdateProductRequest
                {
                    Name = _model.Name,
                    Description = _model.Description,
                    Price = _model.Price,
                    Sku = _model.Sku,
                    CategoryId = _model.CategoryId
                };

                var response = await ProductService.UpdateAsync(Product!.Id, request);
                if (response.Success)
                {
                    Snackbar.Add("Product updated successfully", Severity.Success);
                    MudDialog?.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(response.Error ?? "Failed to update product", Severity.Error);
                }
            }
            else
            {
                var request = new CreateProductRequest
                {
                    Name = _model.Name,
                    Description = _model.Description,
                    Price = _model.Price,
                    Sku = _model.Sku,
                    CategoryId = _model.CategoryId
                };

                var response = await ProductService.CreateAsync(request);
                if (response.Success)
                {
                    Snackbar.Add("Product created successfully", Severity.Success);
                    MudDialog?.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(response.Error ?? "Failed to create product", Severity.Error);
                }
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private class ProductFormModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Sku { get; set; }
        public Guid CategoryId { get; set; }
    }
}
```

### 5.4 Add Navigation Menu Item

**File:** `src/Framework.Admin/Layout/NavMenu.razor`

Add the menu item:

```razor
<MudNavLink Href="/products" Icon="@Icons.Material.Filled.Inventory">Products</MudNavLink>
```

---

## Step 6: Permissions

### 6.1 Add Permission Constants

**File:** `src/Framework.Application/Identity/Permissions.cs`

Add the new permissions:

```csharp
// Products
public const string ProductsView = "Products.View";
public const string ProductsCreate = "Products.Create";
public const string ProductsEdit = "Products.Edit";
public const string ProductsDelete = "Products.Delete";
```

### 6.2 Add to Permission Groups

In the same file, add to the `AllPermissions` list:

```csharp
public static readonly IReadOnlyList<string> AllPermissions = new List<string>
{
    // ... existing permissions ...

    // Products
    ProductsView,
    ProductsCreate,
    ProductsEdit,
    ProductsDelete
};
```

---

## Step 7: Database Migration

### 7.1 Create Migration

```bash
cd src/Framework.Infrastructure

# For SQL Server
dotnet ef migrations add AddProducts --context ApplicationDbContext --output-dir Persistence/Migrations/SqlServer -- --provider SqlServer

# For MySQL
dotnet ef migrations add AddProducts --context ApplicationDbContext --output-dir Persistence/Migrations/MySql -- --provider MySql

# For PostgreSQL
dotnet ef migrations add AddProducts --context ApplicationDbContext --output-dir Persistence/Migrations/PostgreSql -- --provider PostgreSql
```

### 7.2 Apply Migration

```bash
dotnet ef database update --context ApplicationDbContext
```

---

## Step 8: Testing

### 8.1 Domain Tests

**File:** `tests/Framework.Domain.Tests/Products/ProductTests.cs`

```csharp
using Framework.Domain.Products;
using Shouldly;

namespace Framework.Domain.Tests.Products;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var product = new Product("Test Product", 29.99m, Guid.NewGuid());

        // Assert
        product.Name.ShouldBe("Test Product");
        product.Price.ShouldBe(29.99m);
        product.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetName_WithInvalidName_ShouldThrow(string? name)
    {
        // Arrange
        var product = new Product("Valid", 10m, Guid.NewGuid());

        // Act & Assert
        Should.Throw<DomainException>(() => product.SetName(name!));
    }

    [Fact]
    public void SetPrice_WithNegativeValue_ShouldThrow()
    {
        // Arrange
        var product = new Product("Test", 10m, Guid.NewGuid());

        // Act & Assert
        Should.Throw<DomainException>(() => product.SetPrice(-1));
    }

    [Fact]
    public void UpdateStock_WhenSufficientStock_ShouldSucceed()
    {
        // Arrange
        var product = new Product("Test", 10m, Guid.NewGuid());
        product.UpdateStock(10);

        // Act
        product.UpdateStock(-5);

        // Assert
        product.StockQuantity.ShouldBe(5);
    }

    [Fact]
    public void UpdateStock_WhenInsufficientStock_ShouldThrow()
    {
        // Arrange
        var product = new Product("Test", 10m, Guid.NewGuid());
        product.UpdateStock(5);

        // Act & Assert
        Should.Throw<DomainException>(() => product.UpdateStock(-10));
    }
}
```

### 8.2 Application Tests

**File:** `tests/Framework.Application.Tests/Products/CreateProductRequestValidatorTests.cs`

```csharp
using FluentValidation.TestHelper;
using Framework.Application.Products;

namespace Framework.Application.Tests.Products;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Price = 29.99m,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "",
            Price = 29.99m,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNegativePrice_ShouldFail()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test",
            Price = -1,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }
}
```

---

## Checklist

Use this checklist when adding a new module:

### Domain Layer
- [ ] Create entity in `Framework.Domain/{Module}/{Entity}.cs`
- [ ] Add domain events (if needed)
- [ ] Add specifications (if needed)
- [ ] Write domain unit tests

### Application Layer
- [ ] Create DTOs in `Framework.Application/{Module}/`
- [ ] Create service interface
- [ ] Create validators
- [ ] Add permissions to `Permissions.cs`

### Infrastructure Layer
- [ ] Create entity configuration
- [ ] Add DbSet to ApplicationDbContext
- [ ] Implement service
- [ ] Register service in DependencyInjection.cs
- [ ] Create database migration

### API Layer
- [ ] Create controller with CRUD endpoints
- [ ] Add authorization checks
- [ ] Add Swagger documentation

### Admin UI
- [ ] Create API service
- [ ] Create list page
- [ ] Create create/edit dialog
- [ ] Add navigation menu item
- [ ] Register service in Program.cs

### Testing
- [ ] Write domain unit tests
- [ ] Write validator tests
- [ ] Write service tests
- [ ] Write API integration tests

### Documentation
- [ ] Update API documentation
- [ ] Update modules.md if applicable
