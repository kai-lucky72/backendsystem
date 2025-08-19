using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.DTOs;

public class UserDTO
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Deprecated
    public string? WorkId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public bool Active { get; set; }
    
    // Fields for agent-specific information
    public string? Type { get; set; }
    public string? Sector { get; set; }
    
    // Additional fields required by frontend
    public string? Group { get; set; }
    public bool IsTeamLeader { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ClientsCollected { get; set; }
    public int AttendanceRate { get; set; }
    
    /// <summary>
    /// Utility method to format createdAt date
    /// </summary>
    public static string? FormatDate(DateTime? dateTime)
    {
        if (dateTime == null) return null;
        return dateTime.Value.ToString("yyyy-MM-dd");
    }
}