using Framework.Application.Common.Models;

namespace Framework.Application.Tests.Models;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";
        var errorCode = "ERROR_001";

        // Act
        var result = Result.Failure(errorMessage, errorCode);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        Assert.Equal(errorCode, result.ErrorCode);
    }

    [Fact]
    public void ValidationFailure_ShouldContainValidationErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Invalid email format" } },
            { "Name", new[] { "Name is required" } }
        };

        // Act
        var result = Result.ValidationFailure(validationErrors);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Equal(2, result.ValidationErrors.Count);
    }

    [Fact]
    public void NotFound_ShouldHaveCorrectErrorCode()
    {
        // Act
        var result = Result.NotFound("User not found");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal("User not found", result.Error);
    }

    [Fact]
    public void Unauthorized_ShouldHaveCorrectErrorCode()
    {
        // Act
        var result = Result.Unauthorized();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public void Forbidden_ShouldHaveCorrectErrorCode()
    {
        // Act
        var result = Result.Forbidden();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public void Conflict_ShouldHaveCorrectErrorCode()
    {
        // Act
        var result = Result.Conflict("Resource already exists");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }
}

public class ResultOfTTests
{
    [Fact]
    public void Success_ShouldContainValue()
    {
        // Arrange
        var expectedValue = "test value";

        // Act
        var result = Result<string>.Success(expectedValue);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public void Failure_ShouldHaveDefaultValue()
    {
        // Act
        var result = Result<int>.Failure("Error");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccessResult()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        Result<int> result = expectedValue;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public void GenericNotFound_ShouldHaveCorrectErrorCode()
    {
        // Act
        var result = Result<string>.NotFound("Item not found");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Null(result.Value);
    }

    [Fact]
    public void GenericValidationFailure_ShouldContainErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Field", new[] { "Field is invalid" } }
        };

        // Act
        var result = Result<object>.ValidationFailure(validationErrors);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.NotNull(result.ValidationErrors);
    }
}
