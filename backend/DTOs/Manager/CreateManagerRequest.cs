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
    
    [Required(ErrorMessage = "National ID is required")]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Work ID is required")]
    [RegularExpression(@"^[a-zA-Z0-9-_]+$", ErrorMessage = "Work ID can only contain letters, numbers, hyphens and underscores")]
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Work ID must be between 4 and 20 characters")]
    public string WorkId { get; set; } = string.Empty;
}