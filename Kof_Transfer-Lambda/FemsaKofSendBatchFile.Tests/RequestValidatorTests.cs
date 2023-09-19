using FemsaKofSendBatchFile.Validators;
using FemsaKofSendBatchFile.Validators.Abstractions;
using Xunit;
using FluentAssertions;

namespace FemsaKofSendBatchFile.Tests;

public class RequestValidatorTests
{
    private readonly IValidEnvironmentVariables _validEnvironmentVariables;

    public RequestValidatorTests()
    {
        _validEnvironmentVariables = new ValidEnvironmentVariables();
    }

    [Theory]
    [InlineData(null, "https://example.com", "apikey", "username", "password")]
    [InlineData("https://example.com", null, "apikey", "username", "password")]
    [InlineData("https://example.com", "https://example.com", null, "username", "password")]
    [InlineData("https://example.com", "https://example.com", "apikey", null, "password")]
    [InlineData("https://example.com", "https://example.com", "apikey", "username", null)]
    public void IsValidEnvironmentVariables_InvalidVariables_ReturnsFalse(
        string gravtyLoginUrl, string gravtyGetSignedUrl, string apiKey, string userName, string password)
    {
        // Act
        bool isValid = _validEnvironmentVariables.IsValidEnvironmentVariables(
            gravtyLoginUrl, gravtyGetSignedUrl, apiKey, userName, password);

        // Assert
        isValid.Should().BeFalse();        
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com", "apikey", "username", "password")] 
    public void IsValidEnvironmentVariables_ValidVariables_ReturnsTrue(
        string gravtyLoginUrl, string gravtyGetSignedUrl, string apiKey, string userName, string password)
    {
        // Act
        bool isValid = _validEnvironmentVariables.IsValidEnvironmentVariables(
            gravtyLoginUrl, gravtyGetSignedUrl, apiKey, userName, password);

        // Assert
        isValid.Should().BeTrue();
    }
}
