using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Client;

public class ClientDTO
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string InsuranceType { get; set; } = string.Empty;
    public decimal PayingAmount { get; set; }
    public string PayingMethod { get; set; } = string.Empty;
    public int ContractYears { get; set; }
    public string? AgentId { get; set; }
    public string? AgentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
    
    /// <summary>
    /// Formats a date to ISO date format
    /// </summary>
    public static string? FormatDate(DateTime? dateTime)
    {
        if (dateTime == null) return null;
        return dateTime.Value.ToString("yyyy-MM-dd");
    }
}