using Framework.Application.Versioning;
using Shouldly;

namespace Framework.Application.Tests.Versioning;

public class ApiVersionTests
{
    [Theory]
    [InlineData("1", 1, 0)]
    [InlineData("1.0", 1, 0)]
    [InlineData("2.1", 2, 1)]
    [InlineData("10.20", 10, 20)]
    public void Parse_ValidVersion_ShouldParse(string input, int expectedMajor, int expectedMinor)
    {
        // Act
        var version = ApiVersion.Parse(input);

        // Assert
        version.Major.ShouldBe(expectedMajor);
        version.Minor.ShouldBe(expectedMinor);
    }

    [Theory]
    [InlineData("1.0-beta", 1, 0, "beta")]
    [InlineData("2.0-preview", 2, 0, "preview")]
    [InlineData("1.0-rc1", 1, 0, "rc1")]
    public void Parse_WithStatus_ShouldParseStatus(string input, int expectedMajor, int expectedMinor, string expectedStatus)
    {
        // Act
        var version = ApiVersion.Parse(input);

        // Assert
        version.Major.ShouldBe(expectedMajor);
        version.Minor.ShouldBe(expectedMinor);
        version.Status.ShouldBe(expectedStatus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_InvalidVersion_ShouldThrow(string input)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => ApiVersion.Parse(input));
    }

    [Fact]
    public void TryParse_ValidVersion_ShouldReturnTrue()
    {
        // Act
        var result = ApiVersion.TryParse("1.0", out var version);

        // Assert
        result.ShouldBeTrue();
        version.ShouldNotBeNull();
        version!.Major.ShouldBe(1);
    }

    [Fact]
    public void TryParse_InvalidVersion_ShouldReturnFalse()
    {
        // Act
        var result = ApiVersion.TryParse("invalid", out var version);

        // Assert
        result.ShouldBeFalse();
        version.ShouldBeNull();
    }

    [Fact]
    public void CompareTo_SameVersion_ShouldBeZero()
    {
        // Arrange
        var v1 = new ApiVersion(1, 0);
        var v2 = new ApiVersion(1, 0);

        // Act & Assert
        v1.CompareTo(v2).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_GreaterMajor_ShouldBePositive()
    {
        // Arrange
        var v1 = new ApiVersion(2, 0);
        var v2 = new ApiVersion(1, 0);

        // Act & Assert
        v1.CompareTo(v2).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_LesserMinor_ShouldBeNegative()
    {
        // Arrange
        var v1 = new ApiVersion(1, 0);
        var v2 = new ApiVersion(1, 1);

        // Act & Assert
        v1.CompareTo(v2).ShouldBeLessThan(0);
    }

    [Theory]
    [InlineData(1, 0, null, "1")]
    [InlineData(1, 1, null, "1.1")]
    [InlineData(2, 0, "beta", "2-beta")]
    [InlineData(1, 5, "preview", "1.5-preview")]
    public void ToString_ShouldFormatCorrectly(int major, int minor, string? status, string expected)
    {
        // Arrange
        var version = new ApiVersion(major, minor, status);

        // Act & Assert
        version.ToString().ShouldBe(expected);
    }

    [Fact]
    public void Equals_SameValues_ShouldBeTrue()
    {
        // Arrange
        var v1 = new ApiVersion(1, 0, "beta");
        var v2 = new ApiVersion(1, 0, "beta");

        // Act & Assert
        v1.Equals(v2).ShouldBeTrue();
        (v1 == v2).ShouldBeTrue();
        (v1 != v2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentValues_ShouldBeFalse()
    {
        // Arrange
        var v1 = new ApiVersion(1, 0);
        var v2 = new ApiVersion(2, 0);

        // Act & Assert
        v1.Equals(v2).ShouldBeFalse();
        (v1 == v2).ShouldBeFalse();
    }

    [Fact]
    public void ComparisonOperators_ShouldWork()
    {
        // Arrange
        var v1 = new ApiVersion(1, 0);
        var v2 = new ApiVersion(2, 0);

        // Assert
        (v1 < v2).ShouldBeTrue();
        (v1 <= v2).ShouldBeTrue();
        (v2 > v1).ShouldBeTrue();
        (v2 >= v1).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldBeSame()
    {
        // Arrange
        var v1 = new ApiVersion(1, 0);
        var v2 = new ApiVersion(1, 0);

        // Act & Assert
        v1.GetHashCode().ShouldBe(v2.GetHashCode());
    }
}

public class ApiVersionSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeApiVersioning()
    {
        ApiVersionSettings.SectionName.ShouldBe("ApiVersioning");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new ApiVersionSettings();

        // Assert
        settings.Enabled.ShouldBeTrue();
        settings.DefaultVersion.ShouldBe("1.0");
        settings.AssumeDefaultVersionWhenUnspecified.ShouldBeTrue();
        settings.ReportApiVersions.ShouldBeTrue();
        settings.HeaderName.ShouldBe("X-Api-Version");
        settings.QueryParameterName.ShouldBe("api-version");
    }
}

public class ApiVersionAttributeTests
{
    [Fact]
    public void Constructor_WithString_ShouldSetVersion()
    {
        // Act
        var attr = new ApiVersionAttribute("2.0");

        // Assert
        attr.Version.ShouldBe("2.0");
        attr.Deprecated.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithMajorMinor_ShouldSetVersion()
    {
        // Act
        var attr = new ApiVersionAttribute(2, 1);

        // Assert
        attr.Version.ShouldBe("2.1");
    }

    [Fact]
    public void Constructor_WithMajorOnly_ShouldSetVersion()
    {
        // Act
        var attr = new ApiVersionAttribute(3);

        // Assert
        attr.Version.ShouldBe("3");
    }

    [Fact]
    public void Deprecated_ShouldBeSettable()
    {
        // Act
        var attr = new ApiVersionAttribute("1.0") { Deprecated = true };

        // Assert
        attr.Deprecated.ShouldBeTrue();
    }
}
