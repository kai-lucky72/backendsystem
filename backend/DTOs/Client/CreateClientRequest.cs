using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Client;

public class CreateClientRequest
{
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "National ID is required")]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[0-9+\-\s]+$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; } // Optional

    [Required(ErrorMessage = "Location is required")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    public DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Insurance type is required")]
    public string InsuranceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Paying amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Paying amount must be positive")]
    public decimal PayingAmount { get; set; }

    [Required(ErrorMessage = "Paying method is required")]
    public string PayingMethod { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contract years is required")]
    [Range(1, 50, ErrorMessage = "Contract years must be between 1 and 50")]
    public int ContractYears { get; set; }
}