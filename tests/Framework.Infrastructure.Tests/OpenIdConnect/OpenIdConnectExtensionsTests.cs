using Framework.Infrastructure.OpenIdConnect;
using Shouldly;

namespace Framework.Infrastructure.Tests.OpenIdConnect;

public class OpenIdConnectExtensionsTests
{
    [Fact]
    public void GenerateSecureKey_ShouldReturnBase64String()
    {
        // Act
        var key = OpenIdConnectExtensions.GenerateSecureKey();

        // Assert
        key.ShouldNotBeNullOrWhiteSpace();

        // Should be valid base64
        var action = () => Convert.FromBase64String(key);
        action.ShouldNotThrow();
    }

    [Fact]
    public void GenerateSecureKey_DefaultSize_ShouldReturn32Bytes()
    {
        // Act
        var key = OpenIdConnectExtensions.GenerateSecureKey();
        var bytes = Convert.FromBase64String(key);

        // Assert
        bytes.Length.ShouldBe(32);
    }

    [Fact]
    public void GenerateSecureKey_CustomSize_ShouldReturnCorrectBytes()
    {
        // Arrange
        var sizes = new[] { 16, 32, 48, 64 };

        foreach (var size in sizes)
        {
            // Act
            var key = OpenIdConnectExtensions.GenerateSecureKey(size);
            var bytes = Convert.FromBase64String(key);

            // Assert
            bytes.Length.ShouldBe(size);
        }
    }

    [Fact]
    public void GenerateSecureKey_ShouldReturnUniqueKeys()
    {
        // Act
        var keys = Enumerable.Range(0, 100)
            .Select(_ => OpenIdConnectExtensions.GenerateSecureKey())
            .ToList();

        // Assert - All keys should be unique
        keys.Distinct().Count().ShouldBe(keys.Count);
    }

    [Fact]
    public void GenerateSecureKey_ShouldReturnCryptographicallyStrongKey()
    {
        // Act
        var key1 = OpenIdConnectExtensions.GenerateSecureKey();
        var key2 = OpenIdConnectExtensions.GenerateSecureKey();

        // Assert - Keys should be different (statistically speaking)
        key1.ShouldNotBe(key2);
    }

    [Fact]
    public void GenerateSecureKey_SmallSize_ShouldWork()
    {
        // Act
        var key = OpenIdConnectExtensions.GenerateSecureKey(8);
        var bytes = Convert.FromBase64String(key);

        // Assert
        bytes.Length.ShouldBe(8);
    }

    [Fact]
    public void GenerateSecureKey_LargeSize_ShouldWork()
    {
        // Act
        var key = OpenIdConnectExtensions.GenerateSecureKey(256);
        var bytes = Convert.FromBase64String(key);

        // Assert
        bytes.Length.ShouldBe(256);
    }
}
