using Framework.Domain.Auditing;

namespace Framework.Domain.Tests.Auditing;

public class AuditLogTests
{
    [Fact]
    public void Constructor_ShouldCreateAuditLog()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var auditLog = new AuditLog(
            AuditAction.Create,
            "TestEntity",
            "123",
            "user-1",
            "John Doe",
            tenantId);

        // Assert
        Assert.Equal(AuditAction.Create, auditLog.Action);
        Assert.Equal("TestEntity", auditLog.EntityType);
        Assert.Equal("123", auditLog.EntityId);
        Assert.Equal("user-1", auditLog.UserId);
        Assert.Equal("John Doe", auditLog.UserName);
        Assert.Equal(tenantId, auditLog.TenantId);
        Assert.True(auditLog.IsSuccess);
        Assert.NotEqual(default, auditLog.Timestamp);
    }

    [Fact]
    public void Constructor_WithNullOptionalParameters_ShouldCreateAuditLog()
    {
        // Act
        var auditLog = new AuditLog(
            AuditAction.Login,
            "User");

        // Assert
        Assert.Equal(AuditAction.Login, auditLog.Action);
        Assert.Equal("User", auditLog.EntityType);
        Assert.Null(auditLog.EntityId);
        Assert.Null(auditLog.UserId);
        Assert.Null(auditLog.UserName);
        Assert.Null(auditLog.TenantId);
    }

    [Fact]
    public void SetRequestInfo_ShouldSetIpAndUserAgent()
    {
        // Arrange
        var auditLog = new AuditLog(AuditAction.Create, "Entity");

        // Act
        auditLog.SetRequestInfo("192.168.1.1", "Mozilla/5.0");

        // Assert
        Assert.Equal("192.168.1.1", auditLog.IpAddress);
        Assert.Equal("Mozilla/5.0", auditLog.UserAgent);
    }

    [Fact]
    public void SetEntityChanges_ShouldSetOldNewValuesAndColumns()
    {
        // Arrange
        var auditLog = new AuditLog(AuditAction.Update, "Entity", "1");
        var oldValues = "{\"Name\":\"Old\"}";
        var newValues = "{\"Name\":\"New\"}";
        var columns = "Name";

        // Act
        auditLog.SetEntityChanges(oldValues, newValues, columns);

        // Assert
        Assert.Equal(oldValues, auditLog.OldValues);
        Assert.Equal(newValues, auditLog.NewValues);
        Assert.Equal(columns, auditLog.AffectedColumns);
    }

    [Fact]
    public void SetMethodInfo_ShouldSetServiceMethodAndDuration()
    {
        // Arrange
        var auditLog = new AuditLog(AuditAction.Custom, "Report");

        // Act
        auditLog.SetMethodInfo("ReportService", "GenerateReport", 1500);

        // Assert
        Assert.Equal("ReportService", auditLog.ServiceName);
        Assert.Equal("GenerateReport", auditLog.MethodName);
        Assert.Equal(1500, auditLog.Duration);
    }

    [Fact]
    public void SetAdditionalInfo_ShouldSetInfo()
    {
        // Arrange
        var auditLog = new AuditLog(AuditAction.Export, "Data");

        // Act
        auditLog.SetAdditionalInfo("Exported 1000 records to CSV");

        // Assert
        Assert.Equal("Exported 1000 records to CSV", auditLog.AdditionalInfo);
    }

    [Fact]
    public void SetError_ShouldSetErrorMessageAndIsSuccess()
    {
        // Arrange
        var auditLog = new AuditLog(AuditAction.Import, "Data");
        Assert.True(auditLog.IsSuccess);

        // Act
        auditLog.SetError("Failed to import: invalid format");

        // Assert
        Assert.False(auditLog.IsSuccess);
        Assert.Equal("Failed to import: invalid format", auditLog.ErrorMessage);
    }

    [Fact]
    public void SetTenantId_ShouldUpdateTenantId()
    {
        // Arrange
        var auditLog = new AuditLog(AuditAction.Create, "Entity");
        var tenantId = Guid.NewGuid();

        // Act
        auditLog.SetTenantId(tenantId);

        // Assert
        Assert.Equal(tenantId, auditLog.TenantId);
    }

    [Fact]
    public void SetTenantId_WithNull_ShouldClearTenantId()
    {
        // Arrange
        var auditLog = new AuditLog(AuditAction.Create, "Entity", tenantId: Guid.NewGuid());

        // Act
        auditLog.SetTenantId(null);

        // Assert
        Assert.Null(auditLog.TenantId);
    }
}

public class AuditActionTests
{
    [Theory]
    [InlineData(AuditAction.None, 0)]
    [InlineData(AuditAction.Create, 1)]
    [InlineData(AuditAction.Update, 2)]
    [InlineData(AuditAction.Delete, 3)]
    [InlineData(AuditAction.SoftDelete, 4)]
    [InlineData(AuditAction.Restore, 5)]
    [InlineData(AuditAction.Login, 10)]
    [InlineData(AuditAction.Logout, 11)]
    [InlineData(AuditAction.FailedLogin, 12)]
    [InlineData(AuditAction.Custom, 100)]
    public void AuditAction_ShouldHaveCorrectValues(AuditAction action, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)action);
    }
}
