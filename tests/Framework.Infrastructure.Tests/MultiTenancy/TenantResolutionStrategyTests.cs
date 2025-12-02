using Framework.Application.MultiTenancy;
using Framework.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Framework.Infrastructure.Tests.MultiTenancy;

public class TenantResolutionStrategyTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly IOptions<MultiTenancySettings> _settings;

    public TenantResolutionStrategyTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _settings = Options.Create(new MultiTenancySettings
        {
            IsEnabled = true,
            TenantHeader = "X-Tenant-Id",
            TenantQueryParam = "tenant",
            TenantClaimType = "tenant_id",
            Resolution = new TenantResolutionSettings
            {
                UseHeader = true,
                UseQueryString = true,
                UseSubdomain = true,
                UseClaim = true,
                UseRoute = true
            }
        });
    }

    #region HeaderTenantResolutionStrategy Tests

    [Fact]
    public async Task HeaderStrategy_WithValidHeader_ShouldReturnTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "tenant-123";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new HeaderTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Equal("tenant-123", result);
    }

    [Fact]
    public async Task HeaderStrategy_WithMissingHeader_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new HeaderTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HeaderStrategy_WithDisabledHeader_ShouldReturnNull()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings
        {
            Resolution = new TenantResolutionSettings { UseHeader = false }
        });
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "tenant-123";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new HeaderTenantResolutionStrategy(_httpContextAccessorMock.Object, settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HeaderStrategy_WithNullHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var strategy = new HeaderTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region QueryStringTenantResolutionStrategy Tests

    [Fact]
    public async Task QueryStringStrategy_WithValidQueryParam_ShouldReturnTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?tenant=tenant-456");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new QueryStringTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Equal("tenant-456", result);
    }

    [Fact]
    public async Task QueryStringStrategy_WithMissingQueryParam_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new QueryStringTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryStringStrategy_WithDisabledQueryString_ShouldReturnNull()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings
        {
            TenantQueryParam = "tenant",
            Resolution = new TenantResolutionSettings { UseQueryString = false }
        });
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?tenant=tenant-456");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new QueryStringTenantResolutionStrategy(_httpContextAccessorMock.Object, settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SubdomainTenantResolutionStrategy Tests

    [Fact]
    public async Task SubdomainStrategy_WithValidSubdomain_ShouldReturnTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tenant1.example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new SubdomainTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Equal("tenant1", result);
    }

    [Fact]
    public async Task SubdomainStrategy_WithWwwSubdomain_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("www.example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new SubdomainTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SubdomainStrategy_WithApiSubdomain_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("api.example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new SubdomainTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SubdomainStrategy_WithNoSubdomain_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new SubdomainTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SubdomainStrategy_WithDisabledSubdomain_ShouldReturnNull()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings
        {
            Resolution = new TenantResolutionSettings { UseSubdomain = false }
        });
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tenant1.example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var strategy = new SubdomainTenantResolutionStrategy(_httpContextAccessorMock.Object, settings);

        // Act
        var result = await strategy.GetTenantIdentifierAsync();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Priority Tests

    [Fact]
    public void Strategies_ShouldHaveCorrectPriority()
    {
        // Arrange
        var subdomain = new SubdomainTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);
        var header = new HeaderTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);
        var route = new RouteTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);
        var queryString = new QueryStringTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);
        var claim = new ClaimTenantResolutionStrategy(_httpContextAccessorMock.Object, _settings);

        // Assert - Lower number = higher priority
        Assert.True(subdomain.Priority < header.Priority);
        Assert.True(header.Priority < route.Priority);
        Assert.True(route.Priority < queryString.Priority);
        Assert.True(queryString.Priority < claim.Priority);
    }

    #endregion
}
