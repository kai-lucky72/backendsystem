using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Manager;

public class CreateManagerRequest
{
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email is required")]
    public string Email { get; set; } = string.Empty;
    
    public string? Password { get; set; }
    
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,3}[-\s\.]?[0-9]{4,10}$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string? NationalId { get; set; }

    // Deprecated
    public string? WorkId { get; set; }
}