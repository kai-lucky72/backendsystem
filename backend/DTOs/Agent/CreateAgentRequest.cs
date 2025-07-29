using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.DTOs.Agent;

public class CreateAgentRequest
{
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[0-9+\-\s]+$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "National ID is required")]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Work ID is required")]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Work ID must contain only uppercase letters and numbers")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "Work ID must be between 5 and 10 characters")]
    public string WorkId { get; set; } = string.Empty;

    // Password is now optional for agent creation
    public string? Password { get; set; }
    
    // Field to match what the frontend sends
    public string? Type { get; set; }
    
    // Optional field
    public string? Sector { get; set; }
    
    // Helper method to convert type string to Agent.AgentTypeEnum enum
    public Agent.AgentTypeEnum GetAgentType()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            return Agent.AgentTypeEnum.Individual; // Default type
        }
        
        try
        {
            // Try direct enum conversion first
            return Enum.Parse<Agent.AgentTypeEnum>(Type, true);
        }
        catch (ArgumentException)
        {
            // Handle case sensitivity and common variations
            string normalizedType = Type.Trim().ToLower();
            if (normalizedType == "individual")
            {
                return Agent.AgentTypeEnum.Individual;
            }
            else if (normalizedType == "sales")
            {
                return Agent.AgentTypeEnum.Sales;
            }
            else if (normalizedType.Contains("individual"))
            {
                return Agent.AgentTypeEnum.Individual;
            }
            else if (normalizedType.Contains("sale"))
            {
                return Agent.AgentTypeEnum.Sales;
            }
            
            return Agent.AgentTypeEnum.Individual; // Default if not recognized
        }
    }
    
    // Helper method to get sector with default value if not provided
    public string GetSectorOrDefault()
    {
        if (string.IsNullOrWhiteSpace(Sector))
        {
            return "General";
        }
        return Sector;
    }
}