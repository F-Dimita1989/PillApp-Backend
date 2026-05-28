namespace PillApp.Api.Dtos;

public sealed class AdminLoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
}