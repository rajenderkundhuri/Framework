using Framework.Application.Localization;
using Framework.Application.MultiTenancy;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Localization;
using Framework.Domain.MultiTenancy;
using Framework.Infrastructure.Localization;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Framework.Infrastructure.Tests.Localization;

public class ResourceManagementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ResourceManagementService _service;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<ResourceManagementService> _logger;

    public ResourceManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.UserId.Returns("test-user");
        _dateTime = Substitute.For<IDateTime>();
        _dateTime.Now.Returns(DateTimeOffset.UtcNow);
        _logger = Substitute.For<ILogger<ResourceManagementService>>();

        _service = new ResourceManagementService(
            _context,
            _currentUser,
            _dateTime,
            _logger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetResourcesAsync Tests

    [Fact]
    public async Task GetResourcesAsync_ReturnsPagedList()
    {
        // Arrange
        await SeedTestResources();
        var request = new ResourceListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetResourcesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(4, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetResourcesAsync_WithSearchTerm_FiltersResources()
    {
        // Arrange
        await SeedTestResources();
        var request = new ResourceListRequest { SearchTerm = "Welcome", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetResourcesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task GetResourcesAsync_WithCultureFilter_FiltersResources()
    {
        // Arrange
        await SeedTestResources();
        var request = new ResourceListRequest { CultureName = "es-ES", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetResourcesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    [Fact]
    public async Task GetResourcesAsync_WithGroupFilter_FiltersResources()
    {
        // Arrange
        await SeedTestResources();
        var request = new ResourceListRequest { Group = "Common", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetResourcesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Items.Count); // Common.Welcome, Common.Goodbye, Common.Bienvenido
    }

    #endregion

    #region GetResourceByIdAsync Tests

    [Fact]
    public async Task GetResourceByIdAsync_ExistingResource_ReturnsResource()
    {
        // Arrange
        var resource = await CreateTestResource("test.key", "en-US", "Test Value");

        // Act
        var result = await _service.GetResourceByIdAsync(resource.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(resource.Key, result.Value!.Key);
    }

    [Fact]
    public async Task GetResourceByIdAsync_NonExistingResource_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetResourceByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region GetResourceAsync Tests

    [Fact]
    public async Task GetResourceAsync_ExistingKeyAndCulture_ReturnsResource()
    {
        // Arrange
        await CreateTestResource("test.key", "en-US", "Test Value");

        // Act
        var result = await _service.GetResourceAsync("test.key", "en-US");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Value", result.Value!.Value);
    }

    [Fact]
    public async Task GetResourceAsync_NonExistingKey_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetResourceAsync("nonexistent.key", "en-US");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region CreateResourceAsync Tests

    [Fact]
    public async Task CreateResourceAsync_ValidRequest_CreatesResource()
    {
        // Arrange
        var request = new CreateResourceRequest
        {
            Key = "new.key",
            CultureName = "en-US",
            Value = "New Value",
            Group = "Common"
        };

        // Act
        var result = await _service.CreateResourceAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var resource = await _context.Set<LocalizationResource>().FindAsync(result.Value);
        Assert.NotNull(resource);
        Assert.Equal("new.key", resource.Key);
    }

    [Fact]
    public async Task CreateResourceAsync_DuplicateKeyAndCulture_ReturnsConflict()
    {
        // Arrange
        await CreateTestResource("existing.key", "en-US", "Existing Value");
        var request = new CreateResourceRequest
        {
            Key = "existing.key",
            CultureName = "en-US",
            Value = "Duplicate Value"
        };

        // Act
        var result = await _service.CreateResourceAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    #endregion

    #region UpdateResourceAsync Tests

    [Fact]
    public async Task UpdateResourceAsync_ExistingResource_UpdatesResource()
    {
        // Arrange
        var resource = await CreateTestResource("test.key", "en-US", "Original Value");
        var request = new UpdateResourceRequest
        {
            Value = "Updated Value",
            Description = "Updated description"
        };

        // Act
        var result = await _service.UpdateResourceAsync(resource.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedResource = await _context.Set<LocalizationResource>().FindAsync(resource.Id);
        Assert.Equal("Updated Value", updatedResource!.Value);
    }

    [Fact]
    public async Task UpdateResourceAsync_SystemResource_ReturnsForbidden()
    {
        // Arrange
        var resource = await CreateTestResource("system.key", "en-US", "System Value", isSystem: true);
        var request = new UpdateResourceRequest { Value = "Updated" };

        // Act
        var result = await _service.UpdateResourceAsync(resource.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task UpdateResourceAsync_NonExistingResource_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateResourceRequest { Value = "Updated" };

        // Act
        var result = await _service.UpdateResourceAsync(Guid.NewGuid(), request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region DeleteResourceAsync Tests

    [Fact]
    public async Task DeleteResourceAsync_ExistingResource_DeletesResource()
    {
        // Arrange
        var resource = await CreateTestResource("test.key", "en-US", "Test Value");

        // Act
        var result = await _service.DeleteResourceAsync(resource.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var deletedResource = await _context.Set<LocalizationResource>().FindAsync(resource.Id);
        Assert.Null(deletedResource);
    }

    [Fact]
    public async Task DeleteResourceAsync_SystemResource_ReturnsForbidden()
    {
        // Arrange
        var resource = await CreateTestResource("system.key", "en-US", "System Value", isSystem: true);

        // Act
        var result = await _service.DeleteResourceAsync(resource.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    #endregion

    #region Import/Export Tests

    [Fact]
    public async Task ImportResourcesAsync_ValidResources_ImportsResources()
    {
        // Arrange
        var resources = new List<ImportResourceRequest>
        {
            new() { Key = "import.key1", CultureName = "en-US", Value = "Value 1" },
            new() { Key = "import.key2", CultureName = "en-US", Value = "Value 2" }
        };

        // Act
        var result = await _service.ImportResourcesAsync(resources);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.CreatedCount);
        Assert.Equal(0, result.Value.UpdatedCount);
    }

    [Fact]
    public async Task ImportResourcesAsync_ExistingResources_UpdatesResources()
    {
        // Arrange
        await CreateTestResource("existing.key", "en-US", "Original Value");
        var resources = new List<ImportResourceRequest>
        {
            new() { Key = "existing.key", CultureName = "en-US", Value = "Updated Value", OverwriteExisting = true }
        };

        // Act
        var result = await _service.ImportResourcesAsync(resources);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.CreatedCount);
        Assert.Equal(1, result.Value.UpdatedCount);
    }

    [Fact]
    public async Task ExportResourcesAsync_ReturnsResources()
    {
        // Arrange
        await SeedTestResources();
        var request = new ExportResourceRequest { CultureName = "en-US" };

        // Act
        var result = await _service.ExportResourcesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
    }

    #endregion

    #region Language Management Tests

    [Fact]
    public async Task GetLanguagesAsync_ReturnsLanguages()
    {
        // Arrange
        await SeedTestLanguages();

        // Act
        var result = await _service.GetLanguagesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task AddLanguageAsync_ValidRequest_AddsLanguage()
    {
        // Arrange
        var request = new AddLanguageRequest
        {
            CultureName = "fr-FR",
            DisplayName = "French (France)",
            NativeName = "Français",
            FlagCode = "fr"
        };

        // Act
        var result = await _service.AddLanguageAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task AddLanguageAsync_DuplicateCulture_ReturnsConflict()
    {
        // Arrange
        await CreateTestLanguage("en-US", "English (US)", "English");
        var request = new AddLanguageRequest
        {
            CultureName = "en-US",
            DisplayName = "English",
            NativeName = "English"
        };

        // Act
        var result = await _service.AddLanguageAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    [Fact]
    public async Task SetDefaultLanguageAsync_ExistingLanguage_SetsDefault()
    {
        // Arrange
        var language = await CreateTestLanguage("en-US", "English (US)", "English");

        // Act
        var result = await _service.SetDefaultLanguageAsync(language.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedLanguage = await _context.Set<SupportedLanguage>().FindAsync(language.Id);
        Assert.True(updatedLanguage!.IsDefault);
    }

    [Fact]
    public async Task EnableLanguageAsync_DisabledLanguage_EnablesLanguage()
    {
        // Arrange
        var language = await CreateTestLanguage("en-US", "English (US)", "English", isEnabled: false);

        // Act
        var result = await _service.EnableLanguageAsync(language.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedLanguage = await _context.Set<SupportedLanguage>().FindAsync(language.Id);
        Assert.True(updatedLanguage!.IsEnabled);
    }

    [Fact]
    public async Task DisableLanguageAsync_EnabledLanguage_DisablesLanguage()
    {
        // Arrange
        var language = await CreateTestLanguage("en-US", "English (US)", "English", isEnabled: true);

        // Act
        var result = await _service.DisableLanguageAsync(language.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedLanguage = await _context.Set<SupportedLanguage>().FindAsync(language.Id);
        Assert.False(updatedLanguage!.IsEnabled);
    }

    [Fact]
    public async Task DisableLanguageAsync_DefaultLanguage_ReturnsForbidden()
    {
        // Arrange
        var language = await CreateTestLanguage("en-US", "English (US)", "English", isDefault: true);

        // Act
        var result = await _service.DisableLanguageAsync(language.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        await SeedTestResources();
        await SeedTestLanguages();

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetResourceGroupsAsync_ReturnsGroups()
    {
        // Arrange
        await SeedTestResources();

        // Act
        var result = await _service.GetResourceGroupsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
    }

    [Fact]
    public async Task GetResourcesByGroupAsync_ReturnsGroupResources()
    {
        // Arrange
        await SeedTestResources();

        // Act
        var result = await _service.GetResourcesByGroupAsync("Common", null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
    }

    #endregion

    #region Helper Methods

    private async Task<LocalizationResource> CreateTestResource(
        string key, string cultureName, string value,
        string? group = null, bool isSystem = false)
    {
        var resource = new LocalizationResource(key, cultureName, value);
        resource.UpdateMetadata(group, null);
        if (isSystem)
            resource.MarkAsSystem();
        resource.SetCreated(_dateTime.Now, "test");

        _context.Set<LocalizationResource>().Add(resource);
        await _context.SaveChangesAsync();

        return resource;
    }

    private async Task<SupportedLanguage> CreateTestLanguage(
        string cultureName, string displayName, string nativeName,
        bool isEnabled = true, bool isDefault = false)
    {
        var language = new SupportedLanguage(cultureName, displayName, nativeName);
        if (isEnabled)
            language.Enable();
        else
            language.Disable();
        if (isDefault)
            language.SetAsDefault();
        language.SetCreated(_dateTime.Now, "test");

        _context.Set<SupportedLanguage>().Add(language);
        await _context.SaveChangesAsync();

        return language;
    }

    private async Task SeedTestResources()
    {
        await CreateTestResource("Common.Welcome", "en-US", "Welcome", group: "Common");
        await CreateTestResource("Common.Goodbye", "en-US", "Goodbye", group: "Common");
        await CreateTestResource("Common.Bienvenido", "es-ES", "Bienvenido", group: "Common");
        await CreateTestResource("Messages.Hello", "es-ES", "Hola", group: "Messages");
    }

    private async Task SeedTestLanguages()
    {
        await CreateTestLanguage("en-US", "English (US)", "English", isDefault: true);
        await CreateTestLanguage("es-ES", "Spanish (Spain)", "Español");
    }

    #endregion
}
