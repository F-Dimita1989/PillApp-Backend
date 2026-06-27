using System.ComponentModel.DataAnnotations;

namespace PillApp.Api.Dtos;

public sealed class AdminLoginRequestDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 128 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 256 characters.")]
    public string Password { get; set; } = string.Empty;
}