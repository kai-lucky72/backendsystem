using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public class AuthRequest
{
    [Required(ErrorMessage = "Work ID is required")]
    public string WorkId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;
    
    public string? Role { get; set; }
}