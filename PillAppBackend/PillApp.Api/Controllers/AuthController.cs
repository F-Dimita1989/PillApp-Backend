using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PillApp.Api.Dtos;

namespace PillApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public ActionResult<AdminLoginResponseDto> Login([FromBody] AdminLoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username e password sono obbligatori.");

        var adminUsername = _configuration["Security:AdminUsername"];
        var adminPassword = _configuration["Security:AdminPassword"];
        var jwtIssuer = _configuration["Security:JwtIssuer"];
        var jwtAudience = _configuration["Security:JwtAudience"];
        var jwtSigningKey = _configuration["Security:JwtSigningKey"];
        var adminRole = _configuration["Security:AdminRole"] ?? "admin";

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Configurazione admin mancante.");

        if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience) || string.IsNullOrWhiteSpace(jwtSigningKey))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Configurazione JWT mancante.");

        var userMatches = FixedTimeEquals(adminUsername, request.Username);
        var passwordMatches = FixedTimeEquals(adminPassword, request.Password);

        if (!userMatches || !passwordMatches)
            return Unauthorized();

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminUsername),
            new(JwtRegisteredClaimNames.UniqueName, adminUsername),
            new("role", adminRole)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new AdminLoginResponseDto
        {
            AccessToken = tokenString,
            ExpiresAt = expiresAt
        });
    }

    private static bool FixedTimeEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        return expectedBytes.Length == providedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}