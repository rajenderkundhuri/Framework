using System.Security.Claims;
using Framework.Api.Controllers;
using Framework.Application.Common.Models;
using Framework.Application.Identity.Interfaces;
using Framework.Application.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Framework.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _tokenService = Substitute.For<ITokenService>();
        _controller = new AuthController(_identityService, _tokenService);

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@test.com", Password = "Password123!" };
        var tokenResponse = new TokenResponse { AccessToken = "test-token", RefreshToken = "refresh-token" };
        _identityService.LoginAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Success(tokenResponse));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<TokenResponse>();
        response.AccessToken.ShouldBe("test-token");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@test.com", Password = "wrong" };
        _identityService.LoginAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Failure("Invalid credentials", "UNAUTHORIZED"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithUserId()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };
        var userId = Guid.NewGuid();
        _identityService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(userId));

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(userId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };
        _identityService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Email already exists", "CONFLICT"));

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ConflictObjectResult>();
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };
        var tokenResponse = new TokenResponse { AccessToken = "new-token", RefreshToken = "new-refresh" };
        _tokenService.RefreshTokenAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Success(tokenResponse));

        // Act
        var result = await _controller.Refresh(request, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<TokenResponse>();
        response.AccessToken.ShouldBe("new-token");
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid-token" };
        _tokenService.RefreshTokenAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Failure("Invalid refresh token", "UNAUTHORIZED"));

        // Act
        var result = await _controller.Refresh(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Profile Tests

    [Fact]
    public async Task GetProfile_AuthenticatedUser_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var profile = new UserProfileResponse
        {
            Id = userId,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        _identityService.GetUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserProfileResponse>.Success(profile));

        // Act
        var result = await _controller.GetProfile(CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<UserProfileResponse>();
        response.Id.ShouldBe(userId);
    }

    [Fact]
    public async Task GetProfile_NoUserId_ReturnsUnauthorized()
    {
        // Arrange - No user claims set

        // Act
        var result = await _controller.GetProfile(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateProfile_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new UpdateProfileRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "1234567890"
        };
        _identityService.UpdateProfileAsync(userId, request.FirstName, request.LastName, request.PhoneNumber, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.UpdateProfile(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    #endregion

    #region Password Tests

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };
        _identityService.ChangePasswordAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };
        _identityService.ChangePasswordAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Current password is incorrect"));

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_AnyEmail_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "test@test.com" };
        _identityService.GeneratePasswordResetTokenAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("reset-token"));

        // Act
        var result = await _controller.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOk()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Email = "test@test.com",
            Token = "valid-token",
            NewPassword = "NewPassword123!"
        };
        _identityService.ResetPasswordAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.ResetPassword(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _identityService.LogoutAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.Logout(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task Logout_NoUserId_ReturnsUnauthorized()
    {
        // Arrange - No user claims set

        // Act
        var result = await _controller.Logout(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Theme Settings Tests

    [Fact]
    public async Task GetThemeSettings_AuthenticatedUser_ReturnsSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var settings = new ThemeSettingsResponse { IsDarkMode = true, ThemeColorName = "blue" };
        _identityService.GetThemeSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<ThemeSettingsResponse>.Success(settings));

        // Act
        var result = await _controller.GetThemeSettings(CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<ThemeSettingsResponse>();
        response.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateThemeSettings_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new UpdateThemeSettingsRequest { IsDarkMode = true, ThemeColorName = "purple" };
        _identityService.UpdateThemeSettingsAsync(userId, request.IsDarkMode, request.ThemeColorName, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.UpdateThemeSettings(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@test.com")
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
    }

    #endregion
}
