using Framework.Application.Auditing;
using Framework.Domain.Auditing;

namespace Framework.Infrastructure.Tests.Auditing;

public class AuditLogFilterTests
{
    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var filter = new AuditLogFilter();

        // Assert
        Assert.Equal(1, filter.PageNumber);
        Assert.Equal(20, filter.PageSize);
        Assert.Null(filter.TenantId);
        Assert.Null(filter.UserId);
        Assert.Null(filter.Action);
        Assert.Null(filter.EntityType);
        Assert.Null(filter.FromDate);
        Assert.Null(filter.ToDate);
        Assert.Null(filter.IsSuccess);
        Assert.Null(filter.SearchTerm);
    }

    [Fact]
    public void Filter_ShouldBeImmutable()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        // Act
        var filter = new AuditLogFilter
        {
            TenantId = tenantId,
            UserId = "user-1",
            Action = AuditAction.Create,
            EntityType = "Order",
            EntityId = "123",
            FromDate = fromDate,
            ToDate = toDate,
            IsSuccess = true,
            SearchTerm = "test",
            PageNumber = 2,
            PageSize = 50
        };

        // Assert
        Assert.Equal(tenantId, filter.TenantId);
        Assert.Equal("user-1", filter.UserId);
        Assert.Equal(AuditAction.Create, filter.Action);
        Assert.Equal("Order", filter.EntityType);
        Assert.Equal("123", filter.EntityId);
        Assert.Equal(fromDate, filter.FromDate);
        Assert.Equal(toDate, filter.ToDate);
        Assert.True(filter.IsSuccess);
        Assert.Equal("test", filter.SearchTerm);
        Assert.Equal(2, filter.PageNumber);
        Assert.Equal(50, filter.PageSize);
    }
}

public class AuditLogDtoTests
{
    [Fact]
    public void ActionName_ShouldReturnActionAsString()
    {
        // Arrange & Act
        var dto = new AuditLogDto { Action = AuditAction.Create };

        // Assert
        Assert.Equal("Create", dto.ActionName);
    }

    [Theory]
    [InlineData(AuditAction.Create, "Create")]
    [InlineData(AuditAction.Update, "Update")]
    [InlineData(AuditAction.Delete, "Delete")]
    [InlineData(AuditAction.SoftDelete, "SoftDelete")]
    [InlineData(AuditAction.Login, "Login")]
    [InlineData(AuditAction.Custom, "Custom")]
    public void ActionName_ShouldReturnCorrectString(AuditAction action, string expected)
    {
        var dto = new AuditLogDto { Action = action };
        Assert.Equal(expected, dto.ActionName);
    }

    [Fact]
    public void Dto_ShouldHaveAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var dto = new AuditLogDto
        {
            Id = id,
            TenantId = tenantId,
            UserId = "user-1",
            UserName = "John",
            IpAddress = "127.0.0.1",
            Action = AuditAction.Update,
            EntityType = "Product",
            EntityId = "456",
            OldValues = "{\"Price\":10}",
            NewValues = "{\"Price\":20}",
            AffectedColumns = "Price",
            Timestamp = timestamp,
            AdditionalInfo = "Price updated",
            ServiceName = "ProductService",
            MethodName = "UpdatePrice",
            Duration = 100,
            IsSuccess = true,
            ErrorMessage = null
        };

        // Assert
        Assert.Equal(id, dto.Id);
        Assert.Equal(tenantId, dto.TenantId);
        Assert.Equal("user-1", dto.UserId);
        Assert.Equal("John", dto.UserName);
        Assert.Equal("127.0.0.1", dto.IpAddress);
        Assert.Equal(AuditAction.Update, dto.Action);
        Assert.Equal("Update", dto.ActionName);
        Assert.Equal("Product", dto.EntityType);
        Assert.Equal("456", dto.EntityId);
        Assert.Equal("{\"Price\":10}", dto.OldValues);
        Assert.Equal("{\"Price\":20}", dto.NewValues);
        Assert.Equal("Price", dto.AffectedColumns);
        Assert.Equal(timestamp, dto.Timestamp);
        Assert.Equal("Price updated", dto.AdditionalInfo);
        Assert.Equal("ProductService", dto.ServiceName);
        Assert.Equal("UpdatePrice", dto.MethodName);
        Assert.Equal(100, dto.Duration);
        Assert.True(dto.IsSuccess);
        Assert.Null(dto.ErrorMessage);
    }
}

public class AuditSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new AuditSettings();

        // Assert
        Assert.True(settings.IsEnabled);
        Assert.True(settings.LogEntityChanges);
        Assert.True(settings.LogRequestInfo);
        Assert.Equal(90, settings.RetentionDays);
        Assert.Empty(settings.ExcludedEntityTypes);
        Assert.Empty(settings.ExcludedActions);
    }

    [Fact]
    public void SectionName_ShouldBeAudit()
    {
        Assert.Equal("Audit", AuditSettings.SectionName);
    }

    [Fact]
    public void ExcludedEntityTypes_ShouldBeConfigurable()
    {
        // Arrange
        var settings = new AuditSettings();

        // Act
        settings.ExcludedEntityTypes.Add("AuditLog");
        settings.ExcludedEntityTypes.Add("TempEntity");

        // Assert
        Assert.Equal(2, settings.ExcludedEntityTypes.Count);
        Assert.Contains("AuditLog", settings.ExcludedEntityTypes);
    }

    [Fact]
    public void ExcludedActions_ShouldBeConfigurable()
    {
        // Arrange
        var settings = new AuditSettings();

        // Act
        settings.ExcludedActions.Add(AuditAction.None);
        settings.ExcludedActions.Add(AuditAction.Custom);

        // Assert
        Assert.Equal(2, settings.ExcludedActions.Count);
        Assert.Contains(AuditAction.None, settings.ExcludedActions);
    }
}
