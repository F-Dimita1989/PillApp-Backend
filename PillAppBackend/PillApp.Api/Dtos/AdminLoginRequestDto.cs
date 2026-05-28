namespace PillApp.Api.Dtos;

public sealed class AdminLoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}