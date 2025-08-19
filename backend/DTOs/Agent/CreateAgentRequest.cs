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
    
    // Optional
    public string? NationalId { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    // Deprecated
    public string? WorkId { get; set; }

    // Password is now optional for agent creation
    public string? Password { get; set; }
    
    // Field to match what the frontend sends
    public string? Type { get; set; }
    
    // Optional field
    public string? Sector { get; set; }
    
    // Helper method to convert type string to Agent.AgentType enum
    public AgentType GetAgentType()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            return AgentType.INDIVIDUAL; // Default type
        }
        
        try
        {
            // Try direct enum conversion first
            return Enum.Parse<AgentType>(Type, true);
        }
        catch (ArgumentException)
        {
            // Handle case sensitivity and common variations
            string normalizedType = Type.Trim().ToLower();
            if (normalizedType == "individual")
            {
                return AgentType.INDIVIDUAL;
            }
            else if (normalizedType == "sales")
            {
                return AgentType.SALES;
            }
            else if (normalizedType.Contains("individual"))
            {
                return AgentType.INDIVIDUAL;
            }
            else if (normalizedType.Contains("sale"))
            {
                return AgentType.SALES;
            }
            
            return AgentType.INDIVIDUAL; // Default if not recognized
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