using Framework.Application.OpenIdConnect;
using Framework.Domain.Identity;
using Framework.Infrastructure.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.OpenIdConnect;

public class ExternalLoginServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly IOptions<OpenIdConnectSettings> _settings;
    private readonly ILogger<ExternalLoginService> _logger;
    private readonly ExternalLoginService _service;

    public ExternalLoginServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _settings = Options.Create(new OpenIdConnectSettings
        {
            ExternalProviders = new ExternalProvidersSettings
            {
                Enabled = true,
                RequireExistingUser = true,
                AutoCreateUser = false
            }
        });
        _logger = Substitute.For<ILogger<ExternalLoginService>>();
        _service = new ExternalLoginService(_context, _settings, _logger);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExternalLogin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<ApplicationUserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        }
    }

    [Fact]
    public async Task FindByLoginAsync_WhenLoginExists_ShouldReturnResult()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        var login = new ExternalLogin(
            Guid.NewGuid(),
            user.Id,
            "Google",
            "google-key-123",
            "Test User");
        _context.Set<ExternalLogin>().Add(login);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.FindByLoginAsync("Google", "google-key-123");

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(user.Id);
        result.Provider.ShouldBe("Google");
        result.ProviderKey.ShouldBe("google-key-123");
        result.IsLinked.ShouldBeTrue();
        result.UserFound.ShouldBeTrue();
    }

    [Fact]
    public async Task FindByLoginAsync_WhenLoginNotExists_ShouldReturnNull()
    {
        // Act
        var result = await _service.FindByLoginAsync("Google", "non-existent-key");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task LinkLoginAsync_WhenUserExists_ShouldLinkSuccessfully()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.LinkLoginAsync(
            user.Id,
            "AzureAD",
            "azure-key-123",
            "Test User");

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var login = await _context.Set<ExternalLogin>()
            .FirstOrDefaultAsync(e => e.UserId == user.Id);
        login.ShouldNotBeNull();
        login.Provider.ShouldBe("AzureAD");
        login.ProviderKey.ShouldBe("azure-key-123");
    }

    [Fact]
    public async Task LinkLoginAsync_WhenUserNotExists_ShouldReturnNotFound()
    {
        // Act
        var result = await _service.LinkLoginAsync(
            Guid.NewGuid(),
            "Google",
            "google-key-123");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task LinkLoginAsync_WhenLoginAlreadyLinkedToSameUser_ShouldReturnSuccess()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        var login = new ExternalLogin(
            Guid.NewGuid(),
            user.Id,
            "Google",
            "google-key-123");
        _context.Set<ExternalLogin>().Add(login);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.LinkLoginAsync(
            user.Id,
            "Google",
            "google-key-123");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task LinkLoginAsync_WhenLoginLinkedToAnotherUser_ShouldReturnConflict()
    {
        // Arrange
        var user1 = new ApplicationUser(Guid.NewGuid())
        {
            Email = "user1@example.com",
            UserName = "user1@example.com"
        };
        var user2 = new ApplicationUser(Guid.NewGuid())
        {
            Email = "user2@example.com",
            UserName = "user2@example.com"
        };
        _context.Set<ApplicationUser>().AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var login = new ExternalLogin(
            Guid.NewGuid(),
            user1.Id,
            "Google",
            "google-key-123");
        _context.Set<ExternalLogin>().Add(login);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.LinkLoginAsync(
            user2.Id,
            "Google",
            "google-key-123");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.ErrorCode.ShouldBe("CONFLICT");
    }

    [Fact]
    public async Task LinkLoginAsync_WhenUserAlreadyHasProviderLinked_ShouldReturnConflict()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        var login = new ExternalLogin(
            Guid.NewGuid(),
            user.Id,
            "Google",
            "google-key-123");
        _context.Set<ExternalLogin>().Add(login);
        await _context.SaveChangesAsync();

        // Act - Try to link another Google account
        var result = await _service.LinkLoginAsync(
            user.Id,
            "Google",
            "different-google-key");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.ErrorCode.ShouldBe("CONFLICT");
    }

    [Fact]
    public async Task UnlinkLoginAsync_WhenLoginExists_ShouldRemoveSuccessfully()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        var login = new ExternalLogin(
            Guid.NewGuid(),
            user.Id,
            "Google",
            "google-key-123");
        _context.Set<ExternalLogin>().Add(login);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UnlinkLoginAsync(user.Id, "Google");

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var removedLogin = await _context.Set<ExternalLogin>()
            .FirstOrDefaultAsync(e => e.UserId == user.Id);
        removedLogin.ShouldBeNull();
    }

    [Fact]
    public async Task UnlinkLoginAsync_WhenLoginNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UnlinkLoginAsync(user.Id, "Google");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task GetLoginsAsync_WhenUserHasLogins_ShouldReturnAll()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        _context.Set<ExternalLogin>().AddRange(
            new ExternalLogin(Guid.NewGuid(), user.Id, "Google", "google-key"),
            new ExternalLogin(Guid.NewGuid(), user.Id, "AzureAD", "azure-key"),
            new ExternalLogin(Guid.NewGuid(), user.Id, "Okta", "okta-key"));
        await _context.SaveChangesAsync();

        // Act
        var logins = await _service.GetLoginsAsync(user.Id);

        // Assert
        logins.Count().ShouldBe(3);
        logins.Select(l => l.Provider).ShouldContain("Google");
        logins.Select(l => l.Provider).ShouldContain("AzureAD");
        logins.Select(l => l.Provider).ShouldContain("Okta");
    }

    [Fact]
    public async Task GetLoginsAsync_WhenUserHasNoLogins_ShouldReturnEmpty()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var logins = await _service.GetLoginsAsync(user.Id);

        // Assert
        logins.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessExternalLoginAsync_WhenLoginExists_ShouldReturnExistingResult()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        var login = new ExternalLogin(
            Guid.NewGuid(),
            user.Id,
            "Google",
            "google-key-123");
        _context.Set<ExternalLogin>().Add(login);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessExternalLoginAsync(
            "Google",
            "google-key-123",
            "test@example.com");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.IsLinked.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessExternalLoginAsync_WhenUserExistsButNotLinked_ShouldLinkAndReturn()
    {
        // Arrange
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessExternalLoginAsync(
            "Google",
            "google-key-123",
            "test@example.com",
            "Test User");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.IsLinked.ShouldBeTrue();

        var login = await _context.Set<ExternalLogin>()
            .FirstOrDefaultAsync(e => e.UserId == user.Id);
        login.ShouldNotBeNull();
    }

    [Fact]
    public async Task ProcessExternalLoginAsync_WhenUserNotExistsAndRequireExisting_ShouldReturnUnauthorized()
    {
        // Act
        var result = await _service.ProcessExternalLoginAsync(
            "Google",
            "google-key-123",
            "nonexistent@example.com");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.ErrorCode.ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public async Task ProcessExternalLoginAsync_WhenAutoCreateEnabled_ShouldCreateUserAndLink()
    {
        // Arrange
        var settingsWithAutoCreate = Options.Create(new OpenIdConnectSettings
        {
            ExternalProviders = new ExternalProvidersSettings
            {
                Enabled = true,
                RequireExistingUser = false,
                AutoCreateUser = true
            }
        });
        var serviceWithAutoCreate = new ExternalLoginService(_context, settingsWithAutoCreate, _logger);

        // Act
        var result = await serviceWithAutoCreate.ProcessExternalLoginAsync(
            "Google",
            "google-key-123",
            "newuser@example.com",
            "New User");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsLinked.ShouldBeTrue();

        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        user.ShouldNotBeNull();
        user.EmailConfirmed.ShouldBeTrue();
        user.FirstName.ShouldBe("New");
        user.LastName.ShouldBe("User");
    }

    [Fact]
    public async Task ProcessExternalLoginAsync_WhenAutoCreateDisabledAndNoUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var settingsWithoutAutoCreate = Options.Create(new OpenIdConnectSettings
        {
            ExternalProviders = new ExternalProvidersSettings
            {
                Enabled = true,
                RequireExistingUser = false,
                AutoCreateUser = false
            }
        });
        var serviceWithoutAutoCreate = new ExternalLoginService(_context, settingsWithoutAutoCreate, _logger);

        // Act
        var result = await serviceWithoutAutoCreate.ProcessExternalLoginAsync(
            "Google",
            "google-key-123",
            "newuser@example.com");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.ErrorCode.ShouldBe("UNAUTHORIZED");
    }
}
