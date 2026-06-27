using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;
using PillApp.Api.Controllers;
using PillApp.Api.Dtos;

namespace PillApp.Api.Tests;

public class AuthControllerTests
{
    private readonly IConfiguration _configuration;

    public AuthControllerTests()
    {
        var configDict = new Dictionary<string, string?>
        {
            ["Security:AdminUsername"] = "admin",
            ["Security:AdminPassword"] = "password123",
            ["Security:JwtIssuer"] = "test-issuer",
            ["Security:JwtAudience"] = "test-audience",
            ["Security:JwtSigningKey"] = "this-is-a-very-long-secret-key-for-testing-purposes-at-least-32-chars",
            ["Security:AdminRole"] = "admin"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    [Fact]
    public void Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var controller = new AuthController(_configuration);
        var request = new AdminLoginRequestDto { Username = "admin", Password = "password123" };

        // Act
        var result = controller.Login(request);

        // Assert
        Assert.NotNull(result);
        var okResult = result.Result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
        
        var response = okResult.Value as AdminLoginResponseDto;
        Assert.NotNull(response);
        Assert.NotEmpty(response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
    }

    [Fact]
    public void Login_WithInvalidUsername_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new AuthController(_configuration);
        var request = new AdminLoginRequestDto { Username = "invalid", Password = "password123" };

        // Act
        var result = controller.Login(request);

        // Assert
        var unauthorizedResult = result.Result as UnauthorizedResult;
        Assert.NotNull(unauthorizedResult);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public void Login_WithEmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var controller = new AuthController(_configuration);
        var request = new AdminLoginRequestDto { Username = "", Password = "" };

        // Act
        var result = controller.Login(request);

        // Assert
        var badResult = result.Result as BadRequestObjectResult;
        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
    }
}
