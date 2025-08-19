using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public class AuthRequest
{
    [Required(ErrorMessage = "Phone number is required")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}